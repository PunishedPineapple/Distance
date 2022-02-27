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

namespace ReadyCheckHelper
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
			//MemoryHandler.Init( mSigScanner );

			//	Localization and Command Initialization
			OnLanguageChanged( mPluginInterface.UiLanguage );

			//	UI Initialization
			mUI = new PluginUI( this, mPluginInterface, mConfiguration, mDataManager, mGameGui, mSigScanner );
			mPluginInterface.UiBuilder.Draw += DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			mUI.Initialize();

			//	Event Subscription
			mPluginInterface.LanguageChanged += OnLanguageChanged;
			mFramework.Update += OnGameFrameworkUpdate;

			//***** TODO: Add the territory changed event, and filter our database to only the applicable entries when we zone. If we key by the bnpc name id, maybe verify that against the sheet, having the actual english name in the database to check when loading. *****

		}

		//	Cleanup
		public void Dispose()
		{
			mFramework.Update -= OnGameFrameworkUpdate;
			//MemoryHandler.Uninit();
			mUI.Dispose();
			mPluginInterface.UiBuilder.Draw -= DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
			mPluginInterface.LanguageChanged -= OnLanguageChanged;
			mCommandManager.RemoveHandler( mTextCommandName );
		}

		protected void OnLanguageChanged( string langCode )
		{
			//***** TODO *****
			var allowedLang = new List<string>{ /*"de", "ja", "fr", "it", "es"*/ };

			PluginLog.Information( "Trying to set up Loc for culture {0}", langCode );

			if( allowedLang.Contains( langCode ) )
			{
				Loc.Setup( File.ReadAllText( Path.Join( mPluginInterface.AssemblyLocation.FullName, $"loc_{langCode}.json" ) ) );
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
				HelpMessage = String.Format( Loc.Localize( "Plugin Text Command Description", "Use {0} to open the the configuration window." ), "\"/pready config\"" )
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
			else if( subCommand.ToLower() == "data" )
			{
				mUI.MainWindowVisible = !mUI.MainWindowVisible;
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

		protected string ProcessTextCommand_Help( string args )
		{
			if( args.ToLower() == "config" )
			{
				return Loc.Localize( "Config Subcommand Help Message", "Opens the settings window." );
			}
			else
			{
				return String.Format( Loc.Localize( "Basic Help Message", "This plugin works automatically; however, some text commands are supported.  Valid subcommands are {0}, {1}, and {2}.  Use \"{3} <subcommand>\" for more information on each subcommand." ), "\"config\"", "\"results\"", "\"clear\"", "/pready help" );
			}
		}

		public void OnGameFrameworkUpdate( Framework framework )
		{
			var actualTarget = mTargetManager.SoftTarget ?? mTargetManager.Target;
			if( mClientState.LocalPlayer == null || actualTarget == null )
			{
				CurrentDistanceInfo = null;
				CurrentDistanceDrawInfo.ShowAggroDistance = false;
				CurrentDistanceDrawInfo.ShowDistance = false;
			}
			else
			{
				CurrentDistanceInfo ??= new Distance.DistanceInfo();
				CurrentDistanceInfo.Position = mClientState.LocalPlayer.Position;
				
				CurrentDistanceInfo.TargetPosition = actualTarget.Position;
				CurrentDistanceInfo.TargetRadius_Yalms = actualTarget.HitboxRadius;
				CurrentDistanceInfo.AggroRange_Yalms = 14f;

				//***** TODO *****
				/*if( mTargetManager.Target.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
				{
					var id = ( (Dalamud.Game.ClientState.Objects.Types.BattleNpc)mTargetManager.Target ).NameId;
				}*/

				//***** TODO: This is a hack way of doing it and doesn't always work perfectly; need to actually find what writes to the target bar and hook it. *****
				CurrentDistanceDrawInfo.TargetName = actualTarget.Name.ToString();

				if( actualTarget.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc )
				{
					CurrentDistanceDrawInfo.ShowAggroDistance = mConfiguration.ShowAggroDistance && !mCondition[ConditionFlag.InCombat] /*&& we have valid aggro range data for this target*/;  //***** TODO;
					CurrentDistanceDrawInfo.ShowDistance = true;
				}
				else if( actualTarget.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player &&
						 actualTarget.ObjectId != mClientState.LocalPlayer.ObjectId )
				{
					CurrentDistanceDrawInfo.ShowAggroDistance = false;
					CurrentDistanceDrawInfo.ShowDistance = mConfiguration.ShowDistanceOnPlayers;
				}
				else
				{
					CurrentDistanceDrawInfo.ShowAggroDistance = false;
					CurrentDistanceDrawInfo.ShowDistance = false;
				}
			}
		}

		/*protected void PopulateSkeletonData()
		{
			ExcelSheet<ModelSkeleton> contentFinderSheet = mDataManager.GetExcelSheet<ModelSkeleton>();
			foreach( var entry in contentFinderSheet )
			{
			}
		}*/

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

		public Distance.DistanceInfo CurrentDistanceInfo { get; protected set; } = null;
		public readonly Distance.DistanceDrawInfo CurrentDistanceDrawInfo = new Distance.DistanceDrawInfo();

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
	}
}
