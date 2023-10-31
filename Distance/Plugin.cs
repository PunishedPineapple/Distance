using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Distance;

public sealed class Plugin : IDalamudPlugin
{
	//	Initialization
	public Plugin( DalamudPluginInterface pluginInterface )
	{
		//	API Access
		pluginInterface.Create<Service>();
		mPluginInterface = pluginInterface;

		//	Initialization
		TargetResolver.Init();

		//	Configuration
		mConfiguration = mPluginInterface.GetPluginConfig() as Configuration;
		if( mConfiguration == null )
		{
			mConfiguration = new Configuration();
			mConfiguration.DistanceWidgetConfigs.Add( new() );
		}
		mConfiguration.Initialize( mPluginInterface );

		//	Aggro distance data loading
		Task.Run( async () =>
		{
			//	We can have the aggro distances data that got shipped with the plugin, or one that got downloaded.  Load in both and see which has the higher version to decide which to actually use.
			string aggroDistancesFilePath_Assembly = Path.Join( mPluginInterface.AssemblyLocation.DirectoryName, "Resources\\AggroDistances.dat" );
			string aggroDistancesFilePath_Config = Path.Join( mPluginInterface.GetPluginConfigDirectory(), "AggroDistances.dat" );
			BNpcAggroInfoFile aggroFile_Assembly = new();
			BNpcAggroInfoFile aggroFile_Config = new();
			aggroFile_Assembly.ReadFromFile( aggroDistancesFilePath_Assembly );
			if( File.Exists( aggroDistancesFilePath_Config ) )
			{
				aggroFile_Config.ReadFromFile( aggroDistancesFilePath_Config );
			}

			//	Auto-updating (if desired)
			if( mConfiguration.AutoUpdateAggroData )
			{
				UInt64 highestLocalVersion = UInt64.Max( aggroFile_Assembly.FileVersion, aggroFile_Config.FileVersion );
				var downloadedFile = await BNpcAggroInfoDownloader.DownloadUpdatedAggroDataAsync( AggroDataPath, highestLocalVersion );
				aggroFile_Config = downloadedFile ?? aggroFile_Config;
			}
			
			var fileToUse = aggroFile_Config.FileVersion > aggroFile_Assembly.FileVersion ? aggroFile_Config : aggroFile_Assembly;
			BNpcAggroInfo.Init( Service.DataManager, fileToUse );
		} );

		//	Localization and Command Initialization
		OnLanguageChanged( mPluginInterface.UiLanguage );

		//	UI Initialization
		mUI = new PluginUI( this, mPluginInterface, mConfiguration );
		mPluginInterface.UiBuilder.Draw += DrawUI;
		mPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
		mUI.Initialize();
		NameplateHandler.Init( mConfiguration );

		//	We need to disable automatic hiding, because we actually turn off our game UI nodes in the draw functions as-appropriate, so we can't skip the draw functions.
		mPluginInterface.UiBuilder.DisableAutomaticUiHide = true;

		//	Event Subscription
		mPluginInterface.LanguageChanged += OnLanguageChanged;
		Service.Framework.Update += OnGameFrameworkUpdate;
		Service.ClientState.TerritoryChanged += OnTerritoryChanged;
	}

	//	Cleanup
	public void Dispose()
	{
		Service.Framework.Update -= OnGameFrameworkUpdate;
		Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
		mPluginInterface.UiBuilder.Draw -= DrawUI;
		mPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
		mPluginInterface.LanguageChanged -= OnLanguageChanged;
		Service.CommandManager.RemoveHandler( mTextCommandName );
		mUI.Dispose();

		BNpcAggroInfoDownloader.CancelAllDownloads();

		NameplateHandler.Uninit();
		TargetResolver.Uninit();
	}

	private void OnLanguageChanged( string langCode )
	{
		//***** TODO *****
		var allowedLang = new List<string>{ /*"de", "ja", "fr", "it", "es"*/ };

		Service.PluginLog.Information( "Trying to set up Loc for culture {0}", langCode );

		if( allowedLang.Contains( langCode ) )
		{
			Loc.Setup( File.ReadAllText( Path.Join( Path.Join( mPluginInterface.AssemblyLocation.DirectoryName, "Resources\\Localization\\" ), $"loc_{langCode}.json" ) ) );
		}
		else
		{
			Loc.SetupWithFallbacks();
		}

		//	Set up the command handler with the current language.
		if( Service.CommandManager.Commands.ContainsKey( mTextCommandName ) )
		{
			Service.CommandManager.RemoveHandler( mTextCommandName );
		}
		Service.CommandManager.AddHandler( mTextCommandName, new CommandInfo( ProcessTextCommand )
		{
			HelpMessage = String.Format( Loc.Localize( "Plugin Text Command Description", "Use {0} for a listing of available text commands." ), "\"/pdistance help\"" )
		} );
	}

	//	Text Commands
	private void ProcessTextCommand( string command, string args )
	{
		//*****TODO: Don't split, just substring off of the first space so that other stuff is preserved verbatim.
		//	Seperate into sub-command and paramters.
		string subCommand = "";
		string subCommandArgs = "";
		string[] argsArray = args.Split( ' ' );
		if( argsArray.Length > 0 )
		{
			subCommand = argsArray[0];
		}
		if( argsArray.Length > 1 )
		{
			//	Recombine because there might be spaces in JSON or something, that would make splitting it bad.
			for( int i = 1; i < argsArray.Length; ++i )
			{
				subCommandArgs += argsArray[i] + ' ';
			}
			subCommandArgs = subCommandArgs.Trim();
		}

		//	Process the commands.
		bool suppressResponse = mConfiguration.SuppressCommandLineResponses;
		string commandResponse = "";
		if( subCommand.Length == 0 )
		{
			//	For now just have no subcommands act like the config subcommand
			mUI.SettingsWindowVisible = !mUI.SettingsWindowVisible;
		}
		else if( subCommand.ToLower() == "config" )
		{
			mUI.SettingsWindowVisible = !mUI.SettingsWindowVisible;
		}
		else if( subCommand.ToLower() == "enable" )
		{
			commandResponse = ProcessTextCommand_Enable( subCommandArgs );
		}
		else if( subCommand.ToLower() == "disable" )
		{
			commandResponse = ProcessTextCommand_Disable( subCommandArgs );
		}
		else if( subCommand.ToLower() == "toggle" )
		{
			commandResponse = ProcessTextCommand_Toggle( subCommandArgs );
		}
		else if( subCommand.ToLower() == "debug" )
		{
			mUI.DebugWindowVisible = !mUI.DebugWindowVisible;
		}
		else if( subCommand.ToLower() == "help" || subCommand.ToLower() == "?" )
		{
			commandResponse = ProcessTextCommand_Help( subCommandArgs );
			suppressResponse = false;
		}
		else
		{
			commandResponse = ProcessTextCommand_Help( subCommandArgs );
		}

		//	Send any feedback to the user.
		if( commandResponse.Length > 0 && !suppressResponse )
		{
			Service.ChatGui.Print( commandResponse );
		}
	}

	private string ProcessTextCommand_Enable( string args )
	{
		if( args.Trim().Length == 0 ) return Loc.Localize( "Text Command Response: No Widget Name Provided", "No widget name was specified!" );

		var configs = mConfiguration.DistanceWidgetConfigs.FindAll( x => { return x.WidgetName == args.Trim(); });
		foreach( var config in configs )
		{
			config.Enabled = true;
		}

		string retString = "";

		if( configs.Count == 0 )
		{
			retString = String.Format( Loc.Localize( "Text Command Response: Enable - None Found", "No widget(s) named \"{0}\" could be found." ), args.Trim() );
		}
		else if( configs.Count == 1 )
		{
			retString = String.Format( Loc.Localize( "Text Command Response: Enable - One Found", "The widget named \"{0}\" was enabled." ), args.Trim() );
		}
		else
		{
			retString = String.Format( Loc.Localize( "Text Command Response: Enable - Multiple Found", "All {0} widgets named \"{1}\" were enabled." ), configs.Count, args.Trim() );
		}

		return retString;
	}

	private string ProcessTextCommand_Disable( string args )
	{
		if( args.Trim().Length == 0 ) return Loc.Localize( "Text Command Response: No Widget Name Provided", "No widget name was specified!" );

		var configs = mConfiguration.DistanceWidgetConfigs.FindAll( x => { return x.WidgetName == args.Trim(); });
		foreach( var config in configs )
		{
			config.Enabled = false;
		}

		string retString = "";

		if( configs.Count == 0 )
		{
			retString = String.Format( Loc.Localize( "Text Command Response: Disable - None Found", "No widget(s) named \"{0}\" could be found." ), args.Trim() );
		}
		else if( configs.Count == 1 )
		{
			retString = String.Format( Loc.Localize( "Text Command Response: Disable - One Found", "The widget named \"{0}\" was disabled." ), args.Trim() );
		}
		else
		{
			retString = String.Format( Loc.Localize( "Text Command Response: Disable - Multiple Found", "All {0} widgets named \"{1}\" were disabled." ), configs.Count, args.Trim() );
		}

		return retString;
	}

	private string ProcessTextCommand_Toggle( string args )
	{
		if( args.Trim().Length == 0 ) return Loc.Localize( "Text Command Response: No Widget Name Provided", "No widget name was specified!" );

		var configs = mConfiguration.DistanceWidgetConfigs.FindAll( x => { return x.WidgetName == args.Trim(); });
		foreach( var config in configs )
		{
			config.Enabled = !config.Enabled;
		}

		string retString = "";

		if( configs.Count == 0 )
		{
			retString = String.Format( Loc.Localize( "Text Command Response: Toggle - None Found", "No widget(s) named \"{0}\" could be found." ), args.Trim() );
		}
		else if( configs.Count == 1 )
		{
			if( configs[0].Enabled )
			{
				retString = String.Format( Loc.Localize( "Text Command Response: Toggle - One Found (enabled)", "The widget named \"{0}\" was enabled." ), args.Trim() );
			}
			else
			{
				retString = String.Format( Loc.Localize( "Text Command Response: Toggle - One Found (disabled)", "The widget named \"{0}\" was disabled." ), args.Trim() );
			}
		}
		else
		{
			retString = String.Format( Loc.Localize( "Text Command Response: Toggle - Multiple Found", "All {0} widgets named \"{1}\" were toggled." ), configs.Count, args.Trim() );
		}

		return retString;
	}

	private string ProcessTextCommand_Help( string args )
	{
		if( args.ToLower() == "config" )
		{
			return Loc.Localize( "Help Message: Config Subcommand", "Opens the settings window." );
		}
		if( args.ToLower() == "enable" )
		{
			return String.Format( Loc.Localize( "Help Message: Enable Subcommand", "Enables the specified distance widget.  Usage: \"{0} <widget name>\"" ), "/pdistance enable" );
		}
		if( args.ToLower() == "disable" )
		{
			return String.Format( Loc.Localize( "Help Message: Disable Subcommand", "Disables the specified distance widget.  Usage: \"{0} <widget name>\"" ), "/pdistance disable" );
		}
		if( args.ToLower() == "toggle" )
		{
			return String.Format( Loc.Localize( "Help Message: Toggle Subcommand", "Toggles the specified distance widget on or off.  Usage: \"{0} <widget name>\"" ), "/pdistance toggle" );
		}
		else
		{
			return String.Format( Loc.Localize( "Help Message: Basic", "Valid subcommands are {0}, {1}, {2}, and {3}.  Use \"{4} <subcommand>\" for more information on each subcommand." ), "\"config\"", "\"enable\"", "\"disable\"", "\"toggle\"", "/pdistance help" );
		}
	}

	private void OnGameFrameworkUpdate( IFramework framework )
	{
		UpdateTargetDistanceData();

		if( mConfiguration.NameplateDistancesConfig.ShowNameplateDistances ) NameplateHandler.EnableNameplateDistances();
		else NameplateHandler.DisableNameplateDistances();
	}

	private void OnTerritoryChanged( UInt16 ID )
	{
		//	Pre-filter when we enter a zone so that we have a lower chance of stutters once we're actually in.
		BNpcAggroInfo.FilterAggroEntities( ID );
	}

	internal DistanceInfo GetDistanceInfo( TargetType targetType )
	{
		return mCurrentDistanceInfoArray[(int)targetType];
	}

	internal bool ShouldDrawAggroDistanceInfo()
	{
		if( Service.ClientState.IsPvP ) return false;

		//***** TODO: We probably need some director info to make it not show as curtain is coming up.  Condition and addon visibility are incomplete solutions.
		return	!Service.GameGui.GameUiHidden &&
				GetDistanceInfo( mConfiguration.AggroDistanceApplicableTargetType ).IsValid &&
				GetDistanceInfo( mConfiguration.AggroDistanceApplicableTargetType ).TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
				GetDistanceInfo( mConfiguration.AggroDistanceApplicableTargetType ).HasAggroRangeData &&
				TargetResolver.GetTarget( mConfiguration.AggroDistanceApplicableTargetType ) is BattleChara { IsDead: false } &&
				!Service.Condition[ConditionFlag.Unconscious] &&
				!Service.Condition[ConditionFlag.InCombat];
	}

	internal bool ShouldDrawDistanceInfo( DistanceWidgetConfig config )
	{
		if( Service.ClientState.IsPvP ) return false;
		if( !config.Enabled ) return false;
		if( !mCurrentDistanceInfoArray[(int)config.ApplicableTargetType].IsValid ) return false;
		if( mCurrentDistanceInfoArray[(int)config.ApplicableTargetType].ObjectID == Service.ClientState.LocalPlayer?.ObjectId ) return false;

		return	config.Filters.ShowDistanceForObjectKind( mCurrentDistanceInfoArray[(int)config.ApplicableTargetType].TargetKind ) &&
				config.Filters.ShowDistanceForClassJob( Service.ClientState.LocalPlayer?.ClassJob.Id ?? 0 ) &&
				config.Filters.ShowDistanceForConditions( Service.Condition[ConditionFlag.InCombat], Service.Condition[ConditionFlag.BoundByDuty] );
	}

	private void UpdateTargetDistanceData()
	{
		if( Service.ClientState.LocalPlayer == null)
		{
			foreach( var info in mCurrentDistanceInfoArray )
			{
				info.Invalidate();
			}

			return;
		}

		for( int i = 0; i < mCurrentDistanceInfoArray.Length; ++i )
		{
			var target = TargetResolver.GetTarget( (TargetType)i );
			if( target != null )
			{
				mCurrentDistanceInfoArray[i].IsValid = true;
				mCurrentDistanceInfoArray[i].TargetKind = target.ObjectKind;
				mCurrentDistanceInfoArray[i].ObjectID = target.ObjectId;
				mCurrentDistanceInfoArray[i].PlayerPosition = Service.ClientState.LocalPlayer.Position;
				mCurrentDistanceInfoArray[i].TargetPosition = target.Position;
				mCurrentDistanceInfoArray[i].TargetRadius_Yalms = target.HitboxRadius;
				mCurrentDistanceInfoArray[i].BNpcID = ( target as Dalamud.Game.ClientState.Objects.Types.BattleNpc )?.NameId ?? 0;
				float? aggroRange = BNpcAggroInfo.GetAggroRange( mCurrentDistanceInfoArray[i].BNpcID, Service.ClientState.TerritoryType );
				mCurrentDistanceInfoArray[i].HasAggroRangeData = aggroRange.HasValue;
				mCurrentDistanceInfoArray[i].AggroRange_Yalms = aggroRange ?? 0;
			}
			else
			{
				mCurrentDistanceInfoArray[i].Invalidate();
			}
		}
	}

	private void DrawUI()
	{
		mUI.Draw();
	}

	private void DrawConfigUI()
	{
		mUI.SettingsWindowVisible = true;
	}

	public static string Name => "Distance";
	private const string mTextCommandName = "/pdistance";

	internal string AggroDataPath => Path.Join( mPluginInterface.GetPluginConfigDirectory(), "AggroDistances.dat" );

	private readonly DistanceInfo[] mCurrentDistanceInfoArray = new DistanceInfo[Enum.GetNames(typeof(TargetType)).Length];
	private readonly DalamudPluginInterface mPluginInterface;
	private readonly Configuration mConfiguration;
	private readonly PluginUI mUI;
}
