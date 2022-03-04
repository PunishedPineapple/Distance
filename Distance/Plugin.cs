using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using Dalamud.Plugin;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game;
using Dalamud.Data;
using Dalamud.Logging;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.FFXIV.Component.GUI;
using CheapLoc;

namespace Distance
{
	public class Plugin : IDalamudPlugin
	{
		//	Initialization
		public Plugin(
			DalamudPluginInterface pluginInterface,
			Framework framework,
			ClientState clientState,
			CommandManager commandManager,
			Dalamud.Game.ClientState.Conditions.Condition condition,
			TargetManager targetManager,
			ChatGui chatGui,
			GameGui gameGui,
			DataManager dataManager,
			SigScanner sigScanner )
		{
			//	API Access
			mPluginInterface	= pluginInterface;
			mFramework			= framework;
			mClientState		= clientState;
			mCommandManager		= commandManager;
			mCondition			= condition;
			mTargetManager		= targetManager;
			mChatGui			= chatGui;
			mGameGui			= gameGui;
			mSigScanner			= sigScanner;
			mDataManager		= dataManager;

			//	Configuration
			mPluginInterface = pluginInterface;
			mConfiguration = mPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			mConfiguration.Initialize( mPluginInterface );

			//	Aggro distance data loading
			Task.Run( async () =>
			{
				//	We can have the aggro distances data that got shipped with the plugin, or one that got downloaded.  Load in both and see which has the higher version to decide which to actually use.
				string aggroDistancesFilePath_Assembly = Path.Join( mPluginInterface.AssemblyLocation.DirectoryName, "AggroDistances.dat" );
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
					var downloadedFile = await BNpcAggroInfoDownloader.DownloadUpdatedAggroDataAsync( Path.Join( mPluginInterface.GetPluginConfigDirectory(), "AggroDistances.dat" ) );
					aggroFile_Config = downloadedFile ?? aggroFile_Config;
				}
				
				var fileToUse = aggroFile_Config.FileVersion > aggroFile_Assembly.FileVersion ? aggroFile_Config : aggroFile_Assembly;
				BNpcAggroInfo.Init( mDataManager, fileToUse );
			} );

			//	Localization and Command Initialization
			OnLanguageChanged( mPluginInterface.UiLanguage );

			//	UI Initialization
			mUI = new PluginUI( this, mPluginInterface, mConfiguration, mDataManager, mGameGui, mSigScanner, mClientState, mCondition );
			mPluginInterface.UiBuilder.Draw += DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			mUI.Initialize();

			//	Event Subscription
			mPluginInterface.LanguageChanged += OnLanguageChanged;
			mFramework.Update += OnGameFrameworkUpdate;
			mClientState.TerritoryChanged += OnTerritoryChanged;
		}

		//	Cleanup
		public void Dispose()
		{
			mFramework.Update -= OnGameFrameworkUpdate;
			mUI.Dispose();
			mClientState.TerritoryChanged -= OnTerritoryChanged;
			mPluginInterface.UiBuilder.Draw -= DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
			mPluginInterface.LanguageChanged -= OnLanguageChanged;
			mCommandManager.RemoveHandler( mTextCommandName );

			BNpcAggroInfoDownloader.CancelAllDownloads();
		}

		protected void OnLanguageChanged( string langCode )
		{
			//***** TODO *****
			var allowedLang = new List<string>{ /*"de", "ja", "fr", "it", "es"*/ };

			PluginLog.Information( "Trying to set up Loc for culture {0}", langCode );

			if( allowedLang.Contains( langCode ) )
			{
				Loc.Setup( File.ReadAllText( Path.Join( mPluginInterface.AssemblyLocation.DirectoryName, $"loc_{langCode}.json" ) ) );
			}
			else
			{
				Loc.SetupWithFallbacks();
			}

			//	Set up the command handler with the current language.
			if( mCommandManager.Commands.ContainsKey( mTextCommandName ) )
			{
				mCommandManager.RemoveHandler( mTextCommandName );
			}
			mCommandManager.AddHandler( mTextCommandName, new CommandInfo( ProcessTextCommand )
			{
				HelpMessage = String.Format( Loc.Localize( "Plugin Text Command Description", "Use {0} for a listing of available text commands." ), "\"/pdistance help\"" )
			} );
		}

		//	Text Commands
		protected void ProcessTextCommand( string command, string args )
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
			else if( subCommand.ToLower() == "distancemode" )
			{
				commandResponse = ProcessTextCommand_DistanceMode( subCommandArgs );
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
				mChatGui.Print( commandResponse );
			}
		}

		protected string ProcessTextCommand_DistanceMode( string args )
		{
				 if( args.Trim().Equals( "center", StringComparison.InvariantCultureIgnoreCase ) )	mConfiguration.DistanceIsToRing = false;
			else if( args.Trim().Equals( "ring", StringComparison.InvariantCultureIgnoreCase ) )	mConfiguration.DistanceIsToRing = true;
			else																					mConfiguration.DistanceIsToRing = !mConfiguration.DistanceIsToRing;

			return String.Format( Loc.Localize( "Text Command Response: Distancemode", "Distance mode set to {0}" ), mConfiguration.DistanceIsToRing ? "\"ring\"" : "\"center\"" );
		}

		protected string ProcessTextCommand_Help( string args )
		{
			if( args.ToLower() == "config" )
			{
				return Loc.Localize( "Help Message: Config Subcommand", "Opens the settings window." );
			}
			if( args.ToLower() == "distancemode" )
			{
				return String.Format( Loc.Localize( "Help Message: Distancemode Subcommand", "Changes the mode of the distance indicator.  Valid arguments are are {0} and {1}.  If no arguments are supplied, this will cycle between modes." ), "\"center\"", "\"ring\"" );
			}
			else
			{
				return String.Format( Loc.Localize( "Help Message: Basic", "Valid subcommands are {0} and {1}.  Use \"{2} <subcommand>\" for more information on each subcommand." ), "\"config\"", "\"distancemode\"", "/pdistance help" );
			}
		}

		public void OnGameFrameworkUpdate( Framework framework )
		{
			UpdateTargetDistanceData();
		}

		protected void OnTerritoryChanged( object sender, UInt16 ID )
		{
			//	Pre-filter when we enter a zone so that we have a lower chance of stutters once we're actually in.
			BNpcAggroInfo.FilterAggroEntities( ID );
		}

		public DistanceInfo GetDistanceInfo( TargetType targetType, bool targetAlsoMeansSoftTarget )
		{
			if( targetType == TargetType.Target && targetAlsoMeansSoftTarget && mCurrentDistanceInfoArray[(int)TargetType.SoftTarget].IsValid )
			{
				targetType = TargetType.SoftTarget;
			}

			return mCurrentDistanceInfoArray[(int)targetType];
		}

		public bool ShouldDrawAggroDistanceInfo()
		{
			return	mConfiguration.ShowAggroDistance &&
					GetDistanceInfo( TargetType.Target, true ).IsValid &&
					GetDistanceInfo( TargetType.Target, true ).TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
					GetDistanceInfo( TargetType.Target, true ).HasAggroRangeData &&
					!mCondition[ConditionFlag.InCombat];
		}

		public bool ShouldDrawDistanceInfo( TargetType targetType, bool targetAlsoMeansSoftTarget = false )
		{
			if( targetType == TargetType.Target && targetAlsoMeansSoftTarget && mCurrentDistanceInfoArray[(int)TargetType.SoftTarget].IsValid )
			{
				targetType = TargetType.SoftTarget;
			}

			if( !mCurrentDistanceInfoArray[(int)targetType].IsValid ) return false;

			bool show = false;

			switch( mCurrentDistanceInfoArray[(int)targetType].TargetKind )
			{
				case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc:
					show = mConfiguration.ShowDistanceOnBattleNpc;
					break;

				case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player:
					show = mConfiguration.ShowDistanceOnPlayers && mCurrentDistanceInfoArray[(int)targetType].ObjectID != mClientState.LocalPlayer.ObjectId;
					break;

				case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc:
					show = mConfiguration.ShowDistanceOnEventNpc;
					break;

				case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure:
					show = mConfiguration.ShowDistanceOnTreasure;
					break;

				case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Aetheryte:
					show = mConfiguration.ShowDistanceOnAetheryte;
					break;

				case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint:
					show = mConfiguration.ShowDistanceOnGatheringNode;
					break;

				case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj:
					show = mConfiguration.ShowDistanceOnEventObj;
					break;

				case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Companion:
					show = mConfiguration.ShowDistanceOnCompanion;
					break;

				case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Housing:
					show = mConfiguration.ShowDistanceOnHousing;
					break;

				default:
					show = false;
					break;
			}

			return show;
		}

		protected void UpdateTargetDistanceData()
		{
			Dalamud.Game.ClientState.Objects.Types.GameObject target = null;
			int i;

			target = mTargetManager.Target;
			i = (int)TargetType.Target;
			if( target != null )
			{
				mCurrentDistanceInfoArray[i].IsValid = true;
				mCurrentDistanceInfoArray[i].TargetKind = target.ObjectKind;
				mCurrentDistanceInfoArray[i].ObjectID = target.ObjectId;
				mCurrentDistanceInfoArray[i].PlayerPosition = mClientState.LocalPlayer.Position;
				mCurrentDistanceInfoArray[i].TargetPosition = target.Position;
				mCurrentDistanceInfoArray[i].TargetRadius_Yalms = target.HitboxRadius;
				mCurrentDistanceInfoArray[i].BNpcNameID = target.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc ? ( (Dalamud.Game.ClientState.Objects.Types.BattleNpc)target ).NameId : 0;
				float? aggroRange = BNpcAggroInfo.GetAggroRange( mCurrentDistanceInfoArray[i].BNpcNameID, mClientState.TerritoryType );
				mCurrentDistanceInfoArray[i].HasAggroRangeData = aggroRange.HasValue;
				mCurrentDistanceInfoArray[i].AggroRange_Yalms = aggroRange ?? 0;
			}
			else
			{
				mCurrentDistanceInfoArray[i].Invalidate();
			}

			target = mTargetManager.SoftTarget;
			i = (int)TargetType.SoftTarget;
			if( target != null )
			{
				mCurrentDistanceInfoArray[i].IsValid = true;
				mCurrentDistanceInfoArray[i].TargetKind = target.ObjectKind;
				mCurrentDistanceInfoArray[i].ObjectID = target.ObjectId;
				mCurrentDistanceInfoArray[i].PlayerPosition = mClientState.LocalPlayer.Position;
				mCurrentDistanceInfoArray[i].TargetPosition = target.Position;
				mCurrentDistanceInfoArray[i].TargetRadius_Yalms = target.HitboxRadius;
				mCurrentDistanceInfoArray[i].BNpcNameID = target.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc ? ( (Dalamud.Game.ClientState.Objects.Types.BattleNpc)target ).NameId : 0;
				float? aggroRange = BNpcAggroInfo.GetAggroRange( mCurrentDistanceInfoArray[i].BNpcNameID, mClientState.TerritoryType );
				mCurrentDistanceInfoArray[i].HasAggroRangeData = aggroRange.HasValue;
				mCurrentDistanceInfoArray[i].AggroRange_Yalms = aggroRange ?? 0;
			}
			else
			{
				mCurrentDistanceInfoArray[i].Invalidate();
			}

			target = mTargetManager.FocusTarget;
			i = (int)TargetType.FocusTarget;
			if( target != null )
			{
				mCurrentDistanceInfoArray[i].IsValid = true;
				mCurrentDistanceInfoArray[i].TargetKind = target.ObjectKind;
				mCurrentDistanceInfoArray[i].ObjectID = target.ObjectId;
				mCurrentDistanceInfoArray[i].PlayerPosition = mClientState.LocalPlayer.Position;
				mCurrentDistanceInfoArray[i].TargetPosition = target.Position;
				mCurrentDistanceInfoArray[i].TargetRadius_Yalms = target.HitboxRadius;
				mCurrentDistanceInfoArray[i].BNpcNameID = target.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc ? ( (Dalamud.Game.ClientState.Objects.Types.BattleNpc)target ).NameId : 0;
				float? aggroRange = BNpcAggroInfo.GetAggroRange( mCurrentDistanceInfoArray[i].BNpcNameID, mClientState.TerritoryType );
				mCurrentDistanceInfoArray[i].HasAggroRangeData = aggroRange.HasValue;
				mCurrentDistanceInfoArray[i].AggroRange_Yalms = aggroRange ?? 0;
			}
			else
			{
				mCurrentDistanceInfoArray[i].Invalidate();
			}

			target = mTargetManager.MouseOverTarget;
			i = (int)TargetType.MouseOverTarget;
			if( target != null )
			{
				mCurrentDistanceInfoArray[i].IsValid = true;
				mCurrentDistanceInfoArray[i].TargetKind = target.ObjectKind;
				mCurrentDistanceInfoArray[i].ObjectID = target.ObjectId;
				mCurrentDistanceInfoArray[i].PlayerPosition = mClientState.LocalPlayer.Position;
				mCurrentDistanceInfoArray[i].TargetPosition = target.Position;
				mCurrentDistanceInfoArray[i].TargetRadius_Yalms = target.HitboxRadius;
				mCurrentDistanceInfoArray[i].BNpcNameID = target.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc ? ( (Dalamud.Game.ClientState.Objects.Types.BattleNpc)target ).NameId : 0;
				float? aggroRange = BNpcAggroInfo.GetAggroRange( mCurrentDistanceInfoArray[i].BNpcNameID, mClientState.TerritoryType );
				mCurrentDistanceInfoArray[i].HasAggroRangeData = aggroRange.HasValue;
				mCurrentDistanceInfoArray[i].AggroRange_Yalms = aggroRange ?? 0;
			}
			else
			{
				mCurrentDistanceInfoArray[i].Invalidate();
			}
		}

		protected void DrawUI()
		{
			mUI.Draw();
		}

		protected void DrawConfigUI()
		{
			mUI.SettingsWindowVisible = true;
		}

		public string Name => "Distance";
		protected const string mTextCommandName = "/pdistance";

		protected readonly DistanceInfo[] mCurrentDistanceInfoArray = new DistanceInfo[Enum.GetNames(typeof(TargetType)).Length];
		protected DalamudPluginInterface mPluginInterface;
		protected Framework mFramework;
		protected ClientState mClientState;
		protected CommandManager mCommandManager;
		protected Dalamud.Game.ClientState.Conditions.Condition mCondition;
		protected TargetManager mTargetManager;
		protected ChatGui mChatGui;
		protected GameGui mGameGui;
		protected SigScanner mSigScanner;
		protected DataManager mDataManager;
		protected Configuration mConfiguration;
		protected PluginUI mUI;

		public enum TargetType : int
		{
			Target,
			SoftTarget,
			FocusTarget,
			MouseOverTarget
		}
	}
}
