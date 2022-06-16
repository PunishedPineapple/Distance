using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;


namespace Distance
{
	// It is good to have this be disposable in general, in case you ever need it
	// to do any cleanup
	public class PluginUI : IDisposable
	{
		//	Construction
		public PluginUI( Plugin plugin, DalamudPluginInterface pluginInterface, Configuration configuration, DataManager dataManager, GameGui gameGui, ClientState clientState, Dalamud.Game.ClientState.Conditions.Condition condition )
		{
			mPlugin = plugin;
			mPluginInterface = pluginInterface;
			mConfiguration = configuration;
			mDataManager = dataManager;
			mGameGui = gameGui;
			mClientState = clientState;
			mCondition = condition;
		}

		//	Destruction
		unsafe public void Dispose()
		{
			//	This is just to make sure that no nodes get left visible after we stop managing them.  We should probably be properly removing and freeing the
			//	nodes, but by checking for a node with the right id before constructing one, we should only ever have a one-time leak per node, which is probably fine.
			for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; i++ )
			{
				UpdateDistanceTextNode( (uint)i, new(), new(), false );
			}

			UpdateAggroDistanceTextNode( new(), false );
		}

		public void Initialize()
		{
		}

		public void Draw()
		{
			//	Draw the sub-windows.
			DrawSettingsWindow();
			DrawDebugWindow();
			DrawDebugAggroEntitiesWindow();
			DrawDebugNameplateInfoWindow();

			//	Draw other UI stuff.
			DrawOverlay();
			DrawOnGameUI();
		}

		protected void DrawSettingsWindow()
		{
			if( !SettingsWindowVisible )
			{
				return;
			}

			string[] UIAttachDropdownOptions =
			{
				AddonAttachType.Auto.GetTranslatedName(),
				AddonAttachType.ScreenText.GetTranslatedName(),
				AddonAttachType.Target.GetTranslatedName(),
				AddonAttachType.FocusTarget.GetTranslatedName(),
				AddonAttachType.Cursor.GetTranslatedName(),
				//AddonAttachType.Nameplate.GetTranslatedUIAttachTypeEnumString(),
			};

			if( ImGui.Begin( Loc.Localize( "Window Title: Config", "Distance Settings" ) + "###Distance Settings", 
				ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse /*| ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse*/ ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Aggro Distance", "Show the remaining distance from the enemy before they will detect you." ) + "###Show aggro distance.", ref mConfiguration.mShowAggroDistance );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Show Aggro Distance", "This distance will only be shown when it is known, and only on major bosses.  Additionally, it will only be shown until you enter combat." ) );

				if( mConfiguration.ShowAggroDistance )
				{
					if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Aggro Widget Settings", "Aggro Widget Settings" ) + "###Aggro Widget Settings Header." ) )
					{
						if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Aggro Widget Distance Rules", "Distance Rules" ) + $"###Aggro Widget Distance Rules Header." ) )
						{
							ImGui.Text( Loc.Localize( "Config Option: Target Type", "Target Type:" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Applicable Target Type", "The type of target for which this widget will show distance.  \"Soft Target\" generally only matters for controller players and some two-handed keyboard players.  \"Field Mouseover\" is for when you mouseover an object in the world.  \"UI Mouseover\" is for when you mouseover the party list." ) );
							if( ImGui.BeginCombo( $"###AggroDistanceTypeDropdown", mConfiguration.AggroDistanceApplicableTargetType.GetTranslatedName() ) )
							{
								foreach( var item in TargetDropdownMenuItems )
								{
									if( ImGui.Selectable( item.GetTranslatedName(), mConfiguration.AggroDistanceApplicableTargetType == item ) )
									{
										mConfiguration.AggroDistanceApplicableTargetType = item;
									}
								}
								ImGui.EndCombo();
							}
							
							ImGui.TreePop();
						}
						if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Aggro Widget Appearance", "Appearance" ) + $"###Aggro Widget Appearance Header." ) )
						{
							ImGui.Text( Loc.Localize( "Config Option: UI Attach Point", "UI Binding:" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: UI Attach Point", "This is the UI element to which you wish to attach this widget.  \"Automatic\" tries to infer the best choice based on the target type you've selected for this widget.  \"Screen Space\" does not attach to a specific UI element, but floats above everything.  The others should be self-explanatory.  Note: Attaching to the mouse cursor can look odd if you use the hardware cursor; switch to the software cursor in the game options if necessary." ) );
							ImGui.Combo( $"###AggroDistanceUIAttachTypeDropdown", ref mConfiguration.mAggroDistanceUIAttachType, UIAttachDropdownOptions, UIAttachDropdownOptions.Length );
							bool useScreenText = mConfiguration.AggroDistanceUIAttachType.GetGameAddonToUse( mConfiguration.AggroDistanceApplicableTargetType ) == GameAddonEnum.ScreenText;
							Vector2 sliderLimits = new( useScreenText ? 0 : -600, useScreenText ? Math.Max( ImGuiHelpers.MainViewport.Size.X, ImGuiHelpers.MainViewport.Size.Y ) : 600 );
							ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Text Position", "Position of the aggro widget (X,Y):" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Position", "This is an offset relative to the UI element if it is attached to one, or is an absolute position on the screen if not." ) );
							ImGui.DragFloat2( "###AggroDistanceTextPositionSlider", ref mConfiguration.mAggroDistanceTextPosition, 1f, sliderLimits.X, sliderLimits.Y, "%g", ImGuiSliderFlags.AlwaysClamp );
							ImGui.Checkbox( Loc.Localize( "Config Option: Aggro Distance Text Use Heavy Font", "Use heavy font for aggro widget." ) + "###Aggro Distance font heavy.", ref mConfiguration.mAggroDistanceFontHeavy );
							ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Text Font Size", "Aggro widget font size:" ) );
							ImGui.SliderInt( "###AggroDistanceTextFontSizeSlider", ref mConfiguration.mAggroDistanceFontSize, 6, 36 );
							ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Text Alignment", "Text alignment:" ) );
							ImGui.SliderInt( "###AggroDistanceTextFontAlignmentSlider", ref mConfiguration.mAggroDistanceFontAlignment, 6, 8, "", ImGuiSliderFlags.NoInput );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Color", "Aggro widget text color" ) + "###AggroDistanceTextColorPicker", ref mConfiguration.mAggroDistanceTextColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Glow Color", "Aggro widget text glow color" ) + "###AggroDistanceTextEdgeColorPicker", ref mConfiguration.mAggroDistanceTextEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Color Caution", "Aggro widget text color (caution range)" ) + "###AggroDistanceCautionTextColorPicker", ref mConfiguration.mAggroDistanceCautionTextColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Glow Color Caution", "Aggro widget text glow color (caution range)" ) + "###AggroDistanceCautionTextEdgeColorPicker", ref mConfiguration.mAggroDistanceCautionTextEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Color Warning", "Aggro widget text color (warning range)" ) + "###AggroDistanceWarningTextColorPicker", ref mConfiguration.mAggroDistanceWarningTextColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Glow Color Warning", "Aggro widget text glow color (warning range)" ) + "###AggroDistanceWarningTextEdgeColorPicker", ref mConfiguration.mAggroDistanceWarningTextEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Caution Range", "Aggro distance \"caution\" range (y):" ) );
							ImGui.SliderInt( "###AggroDistanceCautionRangeSlider", ref mConfiguration.mAggroCautionDistance_Yalms, 0, 30 );
							ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Warning Range", "Aggro distance \"warning\" range (y):" ) );
							ImGui.SliderInt( "###AggroDistanceWarningRangeSlider", ref mConfiguration.mAggroWarningDistance_Yalms, 0, 30 );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + "###Show aggro distance units.", ref mConfiguration.mShowUnitsOnAggroDistance );
							ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distance:" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Aggro Distance Precision", "Aggro ranges are only accurate to within ~0.05 yalms, so please be wary when using more than one decimal point of precision." ) );
							ImGui.SliderInt( "###AggroDistancePrecisionSlider", ref mConfiguration.mAggroDistanceDecimalPrecision, 0, 3 );
							ImGui.TreePop();
						}
						if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Aggro Widget Arc", "Aggro Arc" ) + $"###Aggro Widget Arc Header." ) )
						{
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Aggro Arc", "Show an arc indicating aggro range." ) + "###Show aggro arc.", ref mConfiguration.mDrawAggroArc );
							if( mConfiguration.DrawAggroArc )
							{
								ImGui.Text( Loc.Localize( "Config Option: Aggro Arc Length", "Length of the aggro arc (deg):" ) );
								ImGui.SliderInt( "###AggroArcLengthSlider", ref mConfiguration.mAggroArcLength_Deg, 0, 15 );
							}
							ImGui.TreePop();
						}
					}

					if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Aggro Distance Data", "Aggro Distance Data" ) + "###Aggro Distance Data Header." ) )
					{
						ImGui.Checkbox( Loc.Localize( "Config Option: Auto Update Aggro Data", "Try to automatically fetch the most recent aggro distance data on startup." ) + "###Auto Update Aggro Data.", ref mConfiguration.mAutoUpdateAggroData );
						if( ImGui.Button( Loc.Localize( "Button: Download Aggro Distances", "Check for Updated Aggro Distances" ) + "###Download updated aggro distances." ) )
						{
							Task.Run( async () =>
							{
								var downloadedFile = await BNpcAggroInfoDownloader.DownloadUpdatedAggroDataAsync( Path.Join( mPluginInterface.GetPluginConfigDirectory(), "AggroDistances.dat" ) );
								if( downloadedFile != null ) BNpcAggroInfo.Init( mDataManager, downloadedFile );
							} );
						}
						if( BNpcAggroInfoDownloader.CurrentDownloadStatus != BNpcAggroInfoDownloader.DownloadStatus.None )
						{
							ImGui.Text( Loc.Localize( "Config Text: Download Status Indicator", $"Status of most recent update attempt:" ) + $"\r\n{BNpcAggroInfoDownloader.GetCurrentDownloadStatusMessage()}" );
						}
					}
				}

				if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Miscellaneous", "Miscellaneous Options" ) + "###Misc. Options Header." ) )
				{

					ImGui.Checkbox( Loc.Localize( "Config Option: Suppress Text Command Responses", "Suppress text command responses." ) + "###Suppress text command responses.", ref mConfiguration.mSuppressCommandLineResponses );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Suppress Text Command Responses", "Selecting this prevents any text commands you use from printing responses to chat.  Responses to the help command will always be printed." ) );
				}

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Separator();

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				if( ImGui.Button( Loc.Localize( "Button: Add Distance Widget", "Add Widget" ) + $"###Add Widget Button." ) )
				{
					mConfiguration.DistanceWidgetConfigs.Add( new() );
				}

				int widgetIndexToDelete = -1;
				for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; ++i )
				{
					var config = mConfiguration.DistanceWidgetConfigs[i];
					var filters = config.Filters;
					string name = config.mWidgetName.Length > 0 ? config.mWidgetName : config.ApplicableTargetType.GetTranslatedName();
					if( ImGui.CollapsingHeader( String.Format( Loc.Localize( "Config Section Header: Distance Widget", "Distance Widget ({0})" ), name ) + $"###Distance Widget Header {i}." ) )
					{
						ImGui.Text( Loc.Localize( "Config Option: Widget Name", "Widget Name:" ) );
						ImGui.SameLine();
						ImGui.InputTextWithHint( $"###WidgetNameInputBox {i}", config.ApplicableTargetType.GetTranslatedName(), ref config.mWidgetName, 50 );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Widget Name", "This is used to give a customized name to this widget for use with certain text commands.  If you leave it blank, the type of target for this widget will be used in the header above, but it will not have a name for use in text commands." )  );
						ImGui.Checkbox( Loc.Localize( "Config Option: Widget Enabled", "Enabled" ) + $"###Widget Enabled Checkbox {i}.", ref config.mEnabled );
						if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Rules", "Distance Rules" ) + $"###Distance Widget Rules Header {i}." ) )
						{
							ImGui.Text( Loc.Localize( "Config Option: Target Type", "Target Type:" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Applicable Target Type", "The type of target for which this widget will show distance.  \"Soft Target\" generally only matters for controller players and some two-handed keyboard players.  \"Field Mouseover\" is for when you mouseover an object in the world.  \"UI Mouseover\" is for when you mouseover the party list." ) );
							if( ImGui.BeginCombo( $"###DistanceTypeDropdown {i}", config.ApplicableTargetType.GetTranslatedName() ) )
							{
								foreach( var item in TargetDropdownMenuItems )
								{
									if( ImGui.Selectable( item.GetTranslatedName(), config.ApplicableTargetType == item ) )
									{
										config.ApplicableTargetType = item;
									}
								}
								ImGui.EndCombo();
							}
							ImGui.Checkbox( Loc.Localize( "Config Option: Distance is to Ring", "Show distance to target ring, not target center." ) + $"###Distance is to ring {i}.", ref config.mDistanceIsToRing );
							ImGui.Text( Loc.Localize( "Config Option: Distance Measurement Offset", "Amount to offset the distance readout (y):" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Readout Offset", "This value is subtracted from the real distance to determine the displayed distance.  This can be used to get the widget to show the distance from being able to hit the boss with a skill, for example." ) );
							ImGui.DragFloat( $"###DistanceOffsetSlider {i}", ref config.mDistanceOffset_Yalms, 0.01f, 0f, 30f );
							ImGui.Checkbox( Loc.Localize( "Config Option: Hide In Combat", "Hide when in combat." ) + $"###Hide In Combat {i}.", ref config.mHideInCombat);
							ImGui.Checkbox( Loc.Localize( "Config Option: Hide Out Of Combat", "Hide when out of combat." ) + $"###Hide Out Of Combat {i}.", ref config.mHideOutOfCombat );
							ImGui.TreePop();
						}

						if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Filters", "Target Filters" ) + $"###Distance Widget Filters Header {i}." ) )
						{
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Players", "Show the distance to players." ) + $"###Show distance to players {i}.", ref filters.mShowDistanceOnPlayers );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on BattleNpc", "Show the distance to combatant NPCs." ) + $"###Show distance to BattleNpc {i}.", ref filters.mShowDistanceOnBattleNpc );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventNpc", "Show the distance to non-combatant NPCs." ) + $"###Show distance to EventNpc {i}.", ref filters.mShowDistanceOnEventNpc );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Treasure", "Show the distance to treasure chests." ) + $"###Show distance to treasure {i}.", ref filters.mShowDistanceOnTreasure );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Aetheryte", "Show the distance to aetherytes." ) + $"###Show distance to aetheryte {i}.", ref filters.mShowDistanceOnAetheryte );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Gathering Node", "Show the distance to gathering nodes." ) + $"###Show distance to gathering node {i}.", ref filters.mShowDistanceOnGatheringNode );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventObj", "Show the distance to interactable objects." ) + $"###Show distance to EventObj {i}.", ref filters.mShowDistanceOnEventObj );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Companion", "Show the distance to companions." ) + $"###Show distance to companion {i}.", ref filters.mShowDistanceOnCompanion );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Housing", "Show the distance to housing items." ) + $"###Show distance to housing {i}.", ref filters.mShowDistanceOnHousing );
							ImGui.TreePop();
						}

						if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Appearance", "Appearance" ) + $"###Distance Widget Appearance Header {i}." ) )
						{
							ImGui.Text( Loc.Localize( "Config Option: UI Attach Point", "UI Binding:" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: UI Attach Point", "This is the UI element to which you wish to attach this widget.  \"Automatic\" tries to infer the best choice based on the target type you've selected for this widget.  \"Screen Space\" does not attach to a specific UI element, but floats above everything.  The others should be self-explanatory.  Note: Attaching to the mouse cursor can look odd if you use the hardware cursor; switch to the software cursor in the game options if necessary." ) );
							ImGui.Combo( $"###UIAttachTypeDropdown {i}", ref config.mUIAttachType, UIAttachDropdownOptions, UIAttachDropdownOptions.Length );
							bool useScreenText = config.UIAttachType.GetGameAddonToUse( config.ApplicableTargetType ) == GameAddonEnum.ScreenText;
							Vector2 sliderLimits = new( useScreenText ? 0 : -600, useScreenText ? Math.Max( ImGuiHelpers.MainViewport.Size.X, ImGuiHelpers.MainViewport.Size.Y ) : 600 );
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Position", "Position of the distance readout (X,Y):" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Position", "This is an offset relative to the UI element if it is attached to one, or is an absolute position on the screen if not." ) );
							ImGui.DragFloat2( $"###DistanceTextPositionSlider {i}", ref config.mTextPosition, 1f, sliderLimits.X, sliderLimits.Y, "%g", ImGuiSliderFlags.AlwaysClamp );
							ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Use Heavy Font", "Use heavy font for distance text." ) + $"###Distance font heavy {i}.", ref config.mFontHeavy );
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Size", "Distance text font size:" ) );
							ImGui.SliderInt( $"###DistanceTextFontSizeSlider {i}", ref config.mFontSize, 6, 36 );
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Alignment", "Text alignment:" ) );
							ImGui.SliderInt( "###DistanceTextFontAlignmentSlider", ref config.mFontAlignment, 6, 8, "", ImGuiSliderFlags.NoInput );
							ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Track Target Bar Color", "Attempt to use target bar text color." ) + $"###Distance Text Use Target Bar Color {i}.", ref config.mTrackTargetBarTextColor );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Track Target Bar Color", "If the color of the target bar text (or focus target) can be determined, it will take precedence; otherwise the colors set below will be used." ) );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color", "Distance text color" ) + $"###DistanceTextColorPicker {i}", ref config.mTextColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color", "Distance text glow color" ) + $"###DistanceTextEdgeColorPicker {i}", ref config.mTextEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + $"###Show distance units {i}.", ref config.mShowUnits );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Mode Indicator", "Show the distance mode indicator." ) + $"###Show distance type marker {i}.", ref config.mShowDistanceModeMarker );
							ImGui.Checkbox( Loc.Localize( "Config Option: Allow Negative Distances", "Allow negative distances." ) + $"###Allow negative distances {i}.", ref config.mAllowNegativeDistances );
							ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distances:" ) );
							ImGui.SliderInt( $"###DistancePrecisionSlider {i}", ref config.mDecimalPrecision, 0, 3 );
							ImGui.TreePop();
						}

						if( ImGui.Button( Loc.Localize( "Button: Delete Distance Widget", "Delete Widget" ) + $"###Delete Widget Button {i}." ) )
						{
							mWidgetIndexWantToDelete = i;
						}
						if( mWidgetIndexWantToDelete == i )
						{
							ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );
							ImGui.Text( Loc.Localize( "Settings Window Text: Confirm Delete Label", "Confirm delete: " ) );
							ImGui.SameLine();
							if( ImGui.Button( Loc.Localize( "Button: Yes", "Yes" ) + $"###Delete Widget Yes Button {i}" ) )
							{
								widgetIndexToDelete = mWidgetIndexWantToDelete;
							}
							ImGui.PopStyleColor();
							ImGui.SameLine();
							if( ImGui.Button( Loc.Localize( "Button: No", "No" ) + $"###Delete Widget No Button {i}" ) )
							{
								mWidgetIndexWantToDelete = -1;
							}
						}
					}
				}
				if( widgetIndexToDelete > -1 && widgetIndexToDelete < mConfiguration.DistanceWidgetConfigs.Count )
				{
					UpdateDistanceTextNode( (uint)widgetIndexToDelete, new(), new(), false );
					mConfiguration.DistanceWidgetConfigs.RemoveAt( widgetIndexToDelete );
					mWidgetIndexWantToDelete = -1;
				}

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				if( ImGui.Button( Loc.Localize( "Button: Save", "Save" ) + "###Save Button" ) )
				{
					mConfiguration.Save();
				}
				ImGui.SameLine();
				if( ImGui.Button( Loc.Localize( "Button: Save and Close", "Save and Close" ) + "###Save and Close Button" ) )
				{
					mConfiguration.Save();
					SettingsWindowVisible = false;
				}
			}

			ImGui.End();
		}

		public static bool IsUsableAggroDistanceFormatString( string str )
		{
			if( str.Where( x => { return x == '{'; } ).Count() != 1 || str.Where( x => { return x == '}'; } ).Count() != 1 ) return false;
			var openBraceIndex = str.IndexOf( '{' );
			var closeBraceIndex = str.IndexOf( '}' );
			if( closeBraceIndex - openBraceIndex != 2 ) return false;
			if( str[closeBraceIndex - 1] != '0' ) return false;
			return true;
		}

		protected void DrawDebugWindow()
		{
			if( !DebugWindowVisible )
			{
				return;
			}

			//	Draw the window.
			ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
			if( ImGui.Begin( Loc.Localize( "Window Title: Debug Data", "Debug Data" ) + "###Debug Data", ref mDebugWindowVisible ) )
			{
				if( ImGui.Button( "Export Localizable Strings" ) )
				{
					string pwd = Directory.GetCurrentDirectory();
					Directory.SetCurrentDirectory( mPluginInterface.AssemblyLocation.DirectoryName );
					Loc.ExportLocalizable();
					Directory.SetCurrentDirectory( pwd );
				}
				ImGui.Checkbox( "Show nameplate data", ref mDebugNameplateInfoWindowVisible );
				ImGui.Checkbox( "Show known aggro range data", ref mDebugAggroEntitiesWindowVisible );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( $"TerritoryType: {mClientState.TerritoryType}" );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				foreach( TargetType entry in Enum.GetValues( typeof( TargetType ) ) )
				{
					ImGui.Text( $"{entry.GetTranslatedName()} Distance Data:" );
					ImGui.Indent();
					ImGui.Text( mPlugin.GetDistanceInfo( entry ).ToString() );
					ImGui.Unindent();
				}
			}

			//	We're done.
			ImGui.End();
		}

		protected void DrawDebugAggroEntitiesWindow()
		{
			if( !DebugAggroEntitiesWindowVisible )
			{
				return;
			}

			//	Draw the window.
			ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
			if( ImGui.Begin( Loc.Localize( "Window Title: Debug Aggro Entities", "Debug: Known Aggro Distances" ) + "###Debug: Known Aggro Distances", ref mDebugAggroEntitiesWindowVisible ) )
			{
				ImGui.Text( $"Current aggro data version: {BNpcAggroInfo.GetCurrentFileVersionAsString()}" );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				var entries = BNpcAggroInfo.GetAllAggroEntities();
				if( entries.Count > 0 )
				{
					if( ImGui.BeginTable( "###KnownAggroEntitiesTable", 4 ) )
					{
						ImGui.TableHeadersRow();
						ImGui.TableSetColumnIndex( 0 );
						ImGui.Text( "TerritoryType" );
						ImGui.TableSetColumnIndex( 1 );
						ImGui.Text( "BNpc ID" );
						ImGui.TableSetColumnIndex( 2 );
						ImGui.Text( "Aggro Distance (y)" );
						ImGui.TableSetColumnIndex( 3 );
						ImGui.Text( "BNpc Name" );

						foreach( var entry in entries )
						{
							ImGui.TableNextRow();
							ImGui.TableSetColumnIndex( 0 );
							ImGui.Text( $"{entry.TerritoryType}" );
							ImGui.TableSetColumnIndex( 1 );
							ImGui.Text( $"{entry.BNpcID}" );
							ImGui.TableSetColumnIndex( 2 );
							ImGui.Text( $"{entry.AggroDistance_Yalms}" );
							ImGui.TableSetColumnIndex( 3 );
							ImGui.Text( $"{entry.EnglishName}" );
						}
						ImGui.EndTable();
					}
				}
				else
				{
					ImGui.Text( "Aggro entities list is empty." );
				}
			}
		}

		protected void DrawDebugNameplateInfoWindow()
		{
			if( !DebugNameplateInfoWindowVisible )
			{
				return;
			}

			//	Draw the window.
			ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
			if( ImGui.Begin( Loc.Localize( "Window Title: Nameplate Info", "Debug: Nameplate Distance Info" ) + "###Debug: Nameplate Info Window", ref mDebugNameplateInfoWindowVisible ) )
			{
				for( int i = 0; i < NameplateHandler.mNameplateDistanceInfoArray.Length; ++i )
				{
					ImGui.Text( $"{i}:" );
					ImGui.Text( NameplateHandler.mNameplateDistanceInfoArray[i].ToString() );

					ImGui.Spacing();
					ImGui.Spacing();
					ImGui.Spacing();
					ImGui.Spacing();
					ImGui.Spacing();
					ImGui.Spacing();
				}
			}
		}

		protected static Vector3[] GetArcPoints( Vector3 center, Vector3 tangentPoint, double arcLength_Deg, float y, int numPoints = -1 )
		{
			//	Compute some points that are on an arc intersecting that point.
			Vector3 translatedTangentPoint = tangentPoint - center;
			float distance = new Vector2( translatedTangentPoint.X, translatedTangentPoint.Z ).Length();
			double arcLength_Rad = arcLength_Deg * Math.PI / 180.0;
			double angle_Rad = Math.Atan2( translatedTangentPoint.Z, translatedTangentPoint.X );

			if( numPoints < 2 ) numPoints = Math.Max( (int)arcLength_Deg, 2 );
			Vector3[] arcPoints = new Vector3[numPoints];
			double angleStep_Rad = arcLength_Rad / ( numPoints - 1 );

			double angleOffset_Rad = -arcLength_Rad / 2.0;
			for( int i = 0; i < arcPoints.Length; ++i )
			{
				arcPoints[i].X = (float)Math.Cos( angle_Rad + angleOffset_Rad ) * distance + center.X;
				arcPoints[i].Z = (float)Math.Sin( angle_Rad + angleOffset_Rad ) * distance + center.Z;
				arcPoints[i].Y = y;
				angleOffset_Rad += angleStep_Rad;
			}

			return arcPoints;
		}

		protected void DrawAggroDistanceArc()
		{
			var distanceInfo = mPlugin.GetDistanceInfo( mConfiguration.AggroDistanceApplicableTargetType );
			float lineLength = distanceInfo.DistanceFromTarget_Yalms;
			float distance_Norm = ( distanceInfo.AggroRange_Yalms + distanceInfo.TargetRadius_Yalms ) / lineLength;
			if( distance_Norm > 0 )
			{
				//	Get the point at the aggro distance on the line between the player and the target.
				Vector3 worldCoords = new()
				{
					X = distance_Norm * ( distanceInfo.PlayerPosition.X - distanceInfo.TargetPosition.X ) + distanceInfo.TargetPosition.X,
					Y = distance_Norm * ( distanceInfo.PlayerPosition.Y - distanceInfo.TargetPosition.Y ) + distanceInfo.TargetPosition.Y,
					Z = distance_Norm * ( distanceInfo.PlayerPosition.Z - distanceInfo.TargetPosition.Z ) + distanceInfo.TargetPosition.Z
				};

				var arcPoints = GetArcPoints( distanceInfo.TargetPosition, worldCoords, mConfiguration.AggroArcLength_Deg, worldCoords.Y );
				var arcScreenPoints = new Vector2[arcPoints.Length];

				bool isScreenPosValid = true;
				isScreenPosValid &= mGameGui.WorldToScreen( worldCoords, out Vector2 screenPos );
				for( int i = 0; i < arcPoints.Length; ++i )
				{
					isScreenPosValid &= mGameGui.WorldToScreen( arcPoints[i], out arcScreenPoints[i] );
				}

				UInt32 color = ImGuiUtils.ColorVecToUInt( mConfiguration.AggroDistanceTextColor );
				UInt32 edgeColor = ImGuiUtils.ColorVecToUInt( mConfiguration.AggroDistanceTextEdgeColor );
				if( distanceInfo.DistanceFromTargetAggro_Yalms < mConfiguration.AggroWarningDistance_Yalms )
				{
					color = ImGuiUtils.ColorVecToUInt( mConfiguration.AggroDistanceWarningTextColor );
					edgeColor = ImGuiUtils.ColorVecToUInt( mConfiguration.AggroDistanceWarningTextEdgeColor );
				}
				else if( distanceInfo.DistanceFromTargetAggro_Yalms < mConfiguration.AggroCautionDistance_Yalms )
				{
					color = ImGuiUtils.ColorVecToUInt( mConfiguration.AggroDistanceCautionTextColor );
					edgeColor = ImGuiUtils.ColorVecToUInt( mConfiguration.AggroDistanceCautionTextEdgeColor );
				}

				ImGui.GetWindowDrawList().AddCircle( screenPos, 5.0f, edgeColor, 36, 5 );
				for( int i = 1; i < arcScreenPoints.Length; ++i )
				{
					ImGui.GetWindowDrawList().AddLine( arcScreenPoints[i-1], arcScreenPoints[i], edgeColor, 5 );
				}

				ImGui.GetWindowDrawList().AddCircle( screenPos, 5.0f, color, 36, 3 );
				for( int i = 1; i < arcScreenPoints.Length; ++i )
				{
					ImGui.GetWindowDrawList().AddLine( arcScreenPoints[i-1], arcScreenPoints[i], color, 3 );
				}
			}
		}

		protected void DrawOverlay()
		{
			if( ShouldHideUIOverlays() ) return;

			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGui.SetNextWindowPos( ImGui.GetMainViewport().Pos );
			ImGui.SetNextWindowSize( ImGui.GetMainViewport().Size );
			if( ImGui.Begin( "##AggroDistanceIndicatorWindow", ImGuiUtils.OverlayWindowFlags ) )
			{
				if( mConfiguration.DrawAggroArc &&
					mPlugin.ShouldDrawAggroDistanceInfo() /*&&
					AddonIsVisible( mConfiguration.AggroDistanceUIAttachType.GetGameAddonToUse( mConfiguration.AggroDistanceApplicableTargetType ) )*/ )	//***** TODO: This is kind of a heavyweight solution, and doesn't even work well.  We probably need director information to get it right.
				{
					DrawAggroDistanceArc();
				}
			}

			ImGui.End();
		}

		protected void DrawOnGameUI()
		{
			//	Draw the aggro widget.
			UpdateAggroDistanceTextNode( mPlugin.GetDistanceInfo( mConfiguration.AggroDistanceApplicableTargetType ), mPlugin.ShouldDrawAggroDistanceInfo() );

			//	Draw each configured distance widget.
			for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; ++i )
			{
				UpdateDistanceTextNode( (uint)i,
										mPlugin.GetDistanceInfo( mConfiguration.DistanceWidgetConfigs[i].ApplicableTargetType ),
										mConfiguration.DistanceWidgetConfigs[i],
										mPlugin.ShouldDrawDistanceInfo( mConfiguration.DistanceWidgetConfigs[i] ) );
			}

			//UpdateNameplateDistanceTextNodes();
		}

		protected void UpdateNameplateDistanceTextNodes()
		{
			for( int i = 0; i < NameplateHandler.mNameplateDistanceInfoArray.Length; ++i )
			{
				TextNodeDrawData drawData = NameplateHandler.GetNameplateNodeDrawData( i ) ?? new TextNodeDrawData()
				{
					PositionX = (short)35,
					PositionY = (short)76,
					TextColorA = (byte)( 1 * 255f ),
					TextColorR = (byte)( 1 * 255f ),
					TextColorG = (byte)( 1 * 255f ),
					TextColorB = (byte)( 1 * 255f ),
					EdgeColorA = (byte)( 1 * 255f ),
					EdgeColorR = (byte)( 1 * 255f ),
					EdgeColorG = (byte)( 1 * 255f ),
					EdgeColorB = (byte)( 1 * 255f ),
					FontSize = (byte)12,
					AlignmentFontType = (byte)( 7 | 0 ),
					LineSpacing = 24,
					CharSpacing = 1
				};

				drawData.PositionX = (short)35;
				drawData.PositionY = (short)76;
				drawData.FontSize = (byte)12;
				drawData.AlignmentFontType = (byte)( 7 | 0 );
				drawData.LineSpacing = 24;
				drawData.CharSpacing = 1;

				NameplateHandler.UpdateNameplateDistanceTextNode( i, $"{NameplateHandler.mNameplateDistanceInfoArray[i].DistanceFromTargetRing_Yalms:F2}", drawData );
			}
		}

		unsafe protected void UpdateDistanceTextNode( uint distanceWidgetNumber, DistanceInfo distanceInfo, DistanceWidgetConfig config, bool show )
		{
			string str = "";
			Vector4 textColorToUse = config.TextColor;
			Vector4 edgeColorToUse = config.TextEdgeColor;

			if( distanceInfo.IsValid )
			{
				float distance = config.DistanceIsToRing ? distanceInfo.DistanceFromTargetRing_Yalms : distanceInfo.DistanceFromTarget_Yalms;
				distance -= config.DistanceOffset_Yalms;
				distance = config.AllowNegativeDistances ? distance : Math.Max( 0, distance );
				string unitString = config.ShowUnits ? "y" : "";
				string distanceTypeSymbol = "";
				if( config.ShowDistanceModeMarker ) distanceTypeSymbol = config.DistanceIsToRing ? "◯ " : "· ";
				str = $"{distanceTypeSymbol}{distance.ToString( $"F{config.DecimalPrecision}" )}{unitString}";
			}

			if( config.TrackTargetBarTextColor )
			{
				AtkUnitBase* pTargetAddonToUse = null;
				UInt16 targetBarNameNodeIndex = 0;
				var pTargetAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_TargetInfo", 1 );
				var pTargetAddonSplit = (AtkUnitBase*)mGameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
				var pFocusTargetAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_FocusTargetInfo", 1 );
				if( config.ApplicableTargetType == TargetType.FocusTarget && pFocusTargetAddon != null && pFocusTargetAddon->IsVisible )
				{
					pTargetAddonToUse = pFocusTargetAddon;
					targetBarNameNodeIndex = FocusTargetBarColorNodeIndex;
				}
				else if( config.ApplicableTargetType == TargetType.TargetOfTarget && pTargetAddon != null && pTargetAddon->IsVisible )
				{
					pTargetAddonToUse = pTargetAddon;
					targetBarNameNodeIndex = TargetOfTargetBarColorNodeIndex;
				}
				else if( config.ApplicableTargetType == TargetType.TargetOfTarget && pTargetAddonSplit != null && pTargetAddonSplit->IsVisible )
				{
					pTargetAddonToUse = pTargetAddonSplit;
					targetBarNameNodeIndex = SplitTargetOfTargetBarColorNodeIndex;
				}
				else if( pTargetAddon != null && pTargetAddon->IsVisible )
				{
					pTargetAddonToUse = pTargetAddon;
					targetBarNameNodeIndex = TargetBarColorNodeIndex;
				}
				else if( pTargetAddonSplit != null && pTargetAddonSplit->IsVisible )
				{
					pTargetAddonToUse = pTargetAddonSplit;
					targetBarNameNodeIndex = SplitTargetBarColorNodeIndex;
				}

				if( pTargetAddonToUse != null )
				{
					var pTargetNameNode = pTargetAddonToUse->UldManager.NodeListSize > targetBarNameNodeIndex ? pTargetAddonToUse->UldManager.NodeList[targetBarNameNodeIndex] : null;
					if( pTargetNameNode != null && pTargetNameNode->GetAsAtkTextNode() != null )
					{
						var pTargetNameTextNode = pTargetNameNode->GetAsAtkTextNode();

						textColorToUse.W = (float)pTargetNameTextNode->TextColor.A / 255f;
						textColorToUse.X = (float)pTargetNameTextNode->TextColor.R / 255f;
						textColorToUse.Y = (float)pTargetNameTextNode->TextColor.G / 255f;
						textColorToUse.Z = (float)pTargetNameTextNode->TextColor.B / 255f;

						edgeColorToUse.W = (float)pTargetNameTextNode->EdgeColor.A / 255f;
						edgeColorToUse.X = (float)pTargetNameTextNode->EdgeColor.R / 255f;
						edgeColorToUse.Y = (float)pTargetNameTextNode->EdgeColor.G / 255f;
						edgeColorToUse.Z = (float)pTargetNameTextNode->EdgeColor.B / 255f;
					}
				}
			}

			GameAddonEnum addonToUse = config.UIAttachType.GetGameAddonToUse( config.ApplicableTargetType );

			Vector2 mouseoverOffset = Vector2.Zero;
			if( addonToUse == GameAddonEnum.ScreenText && (
				config.UIAttachType == AddonAttachType.Cursor ||
				( config.UIAttachType == AddonAttachType.Auto && ( config.ApplicableTargetType is TargetType.MouseOverTarget or TargetType.UIMouseOverTarget or TargetType.MouseOver_And_UIMouseOver_Target ) ) ) )
			{
				mouseoverOffset = ImGui.GetMousePos();
			};

			//	Testing distance value on actor; doesn't work very well.  Better off just getting nameplate distances working and make it an option to only put on mouseover target.
			/*Vector2 screenPos = new();
			bool posIsValid = mGameGui.WorldToScreen( distanceInfo.TargetPosition, out screenPos );
			if( posIsValid )
			{
				mouseoverOffset = screenPos;
			};*/

			TextNodeDrawData drawData = new()
			{
				PositionX = (short)( config.TextPosition.X + mouseoverOffset.X ),
				PositionY = (short)( config.TextPosition.Y + mouseoverOffset.Y ),
				TextColorA = (byte)( textColorToUse.W * 255f ),
				TextColorR = (byte)( textColorToUse.X * 255f ),
				TextColorG = (byte)( textColorToUse.Y * 255f ),
				TextColorB = (byte)( textColorToUse.Z * 255f ),
				EdgeColorA = (byte)( edgeColorToUse.W * 255f ),
				EdgeColorR = (byte)( edgeColorToUse.X * 255f ),
				EdgeColorG = (byte)( edgeColorToUse.Y * 255f ),
				EdgeColorB = (byte)( edgeColorToUse.Z * 255f ),
				FontSize = (byte)config.FontSize,
				AlignmentFontType = (byte)( config.FontAlignment | ( config.FontHeavy ? 0x10 : 0 ) ),
				LineSpacing = 24,
				CharSpacing = 1
			};

			UpdateTextNode( addonToUse, mDistanceNodeIDBase + distanceWidgetNumber, str, drawData, show );
		}

		unsafe protected void UpdateAggroDistanceTextNode( DistanceInfo distanceInfo, bool show )
		{
			string str = "";
			Vector4 color = mConfiguration.mAggroDistanceTextColor;
			Vector4 edgeColor = mConfiguration.mAggroDistanceTextEdgeColor;
			if( distanceInfo.IsValid )
			{
				float distance = Math.Max( 0, distanceInfo.DistanceFromTargetAggro_Yalms );
				string unitString = mConfiguration.ShowUnitsOnAggroDistance ? "y" : "";
				str = $"Aggro in {distance.ToString( $"F{mConfiguration.AggroDistanceDecimalPrecision}" )}{unitString}";

				if( distance < mConfiguration.AggroWarningDistance_Yalms)
				{
					color = mConfiguration.AggroDistanceWarningTextColor;
					edgeColor = mConfiguration.AggroDistanceWarningTextEdgeColor;
				}
				else if( distance < mConfiguration.AggroCautionDistance_Yalms )
				{
					color = mConfiguration.AggroDistanceCautionTextColor;
					edgeColor = mConfiguration.AggroDistanceCautionTextEdgeColor;
				}
			}

			GameAddonEnum addonToUse = mConfiguration.AggroDistanceUIAttachType.GetGameAddonToUse( mConfiguration.AggroDistanceApplicableTargetType );

			Vector2 mouseoverOffset = Vector2.Zero;
			if( addonToUse == GameAddonEnum.ScreenText && (
				mConfiguration.AggroDistanceUIAttachType == AddonAttachType.Cursor ||
				( mConfiguration.AggroDistanceUIAttachType == AddonAttachType.Auto && ( mConfiguration.AggroDistanceApplicableTargetType is TargetType.MouseOverTarget or TargetType.UIMouseOverTarget or TargetType.MouseOver_And_UIMouseOver_Target ) ) ) )
			{
				mouseoverOffset = ImGui.GetMousePos();
			};

			TextNodeDrawData drawData = new()
			{
				PositionX = (short)( mConfiguration.AggroDistanceTextPosition.X + mouseoverOffset.X ),
				PositionY = (short)( mConfiguration.AggroDistanceTextPosition.Y + mouseoverOffset.Y ),
				TextColorA = (byte)( color.W * 255f ),
				TextColorR = (byte)( color.X * 255f ),
				TextColorG = (byte)( color.Y * 255f ),
				TextColorB = (byte)( color.Z * 255f ),
				EdgeColorA = (byte)( edgeColor.W * 255f ),
				EdgeColorR = (byte)( edgeColor.X * 255f ),
				EdgeColorG = (byte)( edgeColor.Y * 255f ),
				EdgeColorB = (byte)( edgeColor.Z * 255f ),
				FontSize = (byte)mConfiguration.AggroDistanceFontSize,
				AlignmentFontType = (byte)( mConfiguration.AggroDistanceFontAlignment | ( mConfiguration.AggroDistanceFontHeavy ? 0x10 : 0 ) ),
				LineSpacing = 24,
				CharSpacing = 1
			};

			UpdateTextNode( addonToUse, mAggroDistanceNodeID, str, drawData, show );
		}

		protected unsafe void UpdateTextNode( GameAddonEnum addonToUse, uint nodeID, string str, TextNodeDrawData drawData, bool show = true )
		{
			AtkTextNode* pNode = null;
			AtkUnitBase* pAddon = null;

			var pNormalTargetBarAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_TargetInfo", 1 );
			var pSplitTargetBarAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
			var pFocusTargetBarAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_FocusTargetInfo", 1 );
			var pScreenTextAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_ScreenText", 1 );
			if( addonToUse == GameAddonEnum.TargetBar )
			{
				pAddon = pSplitTargetBarAddon == null || !pSplitTargetBarAddon->IsVisible ? pNormalTargetBarAddon : pSplitTargetBarAddon;
			}
			else if( addonToUse == GameAddonEnum.FocusTargetBar )
			{
				pAddon = pFocusTargetBarAddon;
			}
			else
			{
				pAddon = pScreenTextAddon;
			}

			if( pAddon != null )
			{
				//	Find our node by ID.  Doing this allows us to not have to deal with freeing the node resources and removing connections to sibling nodes (we'll still leak, but only once).
				pNode = AtkNodeHelpers.GetTextNodeByID( pAddon, nodeID );

				//	If we have our node, set the colors, size, and text from settings.
				if( pNode != null )
				{
					bool visible = show && !ShouldHideUIOverlays();
					( (AtkResNode*)pNode )->ToggleVisibility( visible );
					if( visible )
					{
						pNode->AtkResNode.SetPositionShort( drawData.PositionX, drawData.PositionY );

						pNode->TextColor.A = drawData.TextColorA;
						pNode->TextColor.R = drawData.TextColorR;
						pNode->TextColor.G = drawData.TextColorG;
						pNode->TextColor.B = drawData.TextColorB;

						pNode->EdgeColor.A = drawData.EdgeColorA;
						pNode->EdgeColor.R = drawData.EdgeColorR;
						pNode->EdgeColor.G = drawData.EdgeColorG;
						pNode->EdgeColor.B = drawData.EdgeColorB;

						pNode->FontSize = drawData.FontSize;
						pNode->AlignmentFontType = drawData.AlignmentFontType;
						pNode->LineSpacing = drawData.LineSpacing;
						pNode->CharSpacing = drawData.CharSpacing;

						pNode->SetText( str );
					}
				}
				//	Set up the node if it hasn't been.
				else if( pAddon->RootNode != null )
				{
					pNode = AtkNodeHelpers.CreateNewTextNode( pAddon, nodeID );
				}
			}

			//	Hide the node(s) with the same ID on any of the other addons (in case we switched addon for the node recently).
			if( pAddon != pScreenTextAddon ) AtkNodeHelpers.HideNode( pScreenTextAddon, nodeID );
			if( pAddon != pFocusTargetBarAddon ) AtkNodeHelpers.HideNode( pFocusTargetBarAddon, nodeID );
			if( pAddon != pNormalTargetBarAddon ) AtkNodeHelpers.HideNode( pNormalTargetBarAddon, nodeID );
			if( pAddon != pSplitTargetBarAddon ) AtkNodeHelpers.HideNode( pSplitTargetBarAddon, nodeID );
		}

		protected unsafe bool AddonIsVisible( GameAddonEnum addon )
		{
			switch( addon )
			{
				case GameAddonEnum.TargetBar:
					var pNormalTargetBarAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_TargetInfo", 1 );
					var pSplitTargetBarAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
					return ( pNormalTargetBarAddon != null && pNormalTargetBarAddon->IsVisible ) || ( pSplitTargetBarAddon != null && pSplitTargetBarAddon->IsVisible );
				case GameAddonEnum.FocusTargetBar:
					var pFocusTargetBarAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_FocusTargetInfo", 1 );
					return pFocusTargetBarAddon != null && pFocusTargetBarAddon->IsVisible;
				default:
					return false;
			}
		}

		protected bool ShouldHideUIOverlays()
		{
			return	mGameGui.GameUiHidden ||
					mCondition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInCutSceneEvent] ||
					mCondition[Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene] ||
					mCondition[Dalamud.Game.ClientState.Conditions.ConditionFlag.CreatingCharacter];
		}

		protected Plugin mPlugin;
		protected DalamudPluginInterface mPluginInterface;
		protected Configuration mConfiguration;
		protected DataManager mDataManager;
		protected GameGui mGameGui;
		protected Dalamud.Game.ClientState.Conditions.Condition mCondition;
		protected ClientState mClientState;

		//	Need a real backing field on the following properties for use with ImGui.
		protected bool mSettingsWindowVisible = false;
		public bool SettingsWindowVisible
		{
			get { return mSettingsWindowVisible; }
			set { mSettingsWindowVisible = value; }
		}

		protected bool mDebugWindowVisible = false;
		public bool DebugWindowVisible
		{
			get { return mDebugWindowVisible; }
			set { mDebugWindowVisible = value; }
		}

		protected bool mDebugAggroEntitiesWindowVisible = false;
		public bool DebugAggroEntitiesWindowVisible
		{
			get { return mDebugAggroEntitiesWindowVisible; }
			set { mDebugAggroEntitiesWindowVisible = value; }
		}

		protected bool mDebugNameplateInfoWindowVisible = false;
		public bool DebugNameplateInfoWindowVisible
		{
			get { return mDebugNameplateInfoWindowVisible; }
			set { mDebugNameplateInfoWindowVisible = value; }
		}

		protected int mWidgetIndexWantToDelete = -1;

		//	Do this to control the order of dropdown items.
		protected static readonly TargetType[] TargetDropdownMenuItems =
		{
			TargetType.Target_And_Soft_Target,
			TargetType.FocusTarget,
			TargetType.MouseOver_And_UIMouseOver_Target,
			TargetType.Target,
			TargetType.SoftTarget,
			TargetType.MouseOverTarget,
			TargetType.UIMouseOverTarget,
			TargetType.TargetOfTarget,
		};

		protected const UInt16 FocusTargetBarColorNodeIndex = 10;
		protected const UInt16 TargetBarColorNodeIndex = 39;
		protected const UInt16 SplitTargetBarColorNodeIndex = 8;
		protected const UInt16 TargetOfTargetBarColorNodeIndex = 49;
		protected const UInt16 SplitTargetOfTargetBarColorNodeIndex = 12;

		//	Note: Node IDs only need to be unique within a given addon.
		protected const uint mDistanceNodeIDBase = 0x6C78B300;    //YOLO hoping for no collisions.
		protected const uint mAggroDistanceNodeID = mDistanceNodeIDBase - 1;
	}
}