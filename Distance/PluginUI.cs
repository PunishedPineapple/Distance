using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;

namespace Distance;

public sealed class PluginUI : IDisposable
{
	public PluginUI( Plugin plugin, DalamudPluginInterface pluginInterface, Configuration configuration )
	{
		mPlugin = plugin;
		mPluginInterface = pluginInterface;
		mConfiguration = configuration;

		mAggroArcsUI = new( plugin, this, configuration );
		mCustomWidgetsUI = new( plugin, this, configuration );
		mCustomArcsUI = new( plugin, this, configuration );
	}

	public unsafe void Dispose()
	{
		//	This is just to make sure that no nodes get left visible after we stop managing them.  We should probably be properly removing and freeing the
		//	nodes, but by checking for a node with the right id before constructing one, we should only ever have a one-time leak per node, which is probably fine.
		for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; i++ )
		{
			HideTextNode( mDistanceNodeIDBase + (uint)i );
		}

		HideTextNode( mAggroDistanceNodeID );

		mAggroArcsUI.Dispose();
		mCustomWidgetsUI.Dispose();
		mCustomArcsUI.Dispose();
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
		mOverlayDrawTimer.Restart();
		DrawOverlay();
		mOverlayDrawTimer.Stop();
		mOverlayDrawTime_uSec = mOverlayDrawTimer.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;

		mWidgetNodeUpdateTimer.Restart();
		DrawOnGameUI();
		mWidgetNodeUpdateTimer.Stop();
		mWidgetNodeUpdateTime_uSec = mWidgetNodeUpdateTimer.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;
	}

	private void DrawSettingsWindow()
	{
		if( !SettingsWindowVisible )
		{
			return;
		}

		//	I'm too stupid to do anything right so we get to have this pile :D
		float minWidth =	ImGui.GetStyle().WindowPadding.X * 2 +
							ImGui.GetStyle().FramePadding.X * 2 + ImGui.GetTextLineHeight() /*cheat to get checkbox width*/ + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.CalcTextSize( Loc.Localize( "Config Option: Show Aggro Distance", "Show the remaining distance from the enemy before they will detect you." ) ).X +
							ImGui.GetStyle().FramePadding.X * 2 + ImGui.GetStyle().ItemSpacing.X + ImGui.CalcTextSize( "(?)" ).X;

		ImGui.SetNextWindowSizeConstraints( new( minWidth, 0f ), new( float.MaxValue ) );
		if( ImGui.Begin( Loc.Localize( "Window Title: Config", "Distance Settings" ) + "###Distance Settings", ref mSettingsWindowVisible,
			ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse /*| ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse*/ ) )
		{
			if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Aggro Widget Settings", "Aggro Widget Settings" ) + "###Aggro Widget Settings Header." ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Aggro Distance", "Show the remaining distance from the enemy before they will detect you." ) + "###Show aggro distance.", ref mConfiguration.mShowAggroDistance );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Show Aggro Distance", "This distance will only be shown when it is known, and only on major bosses.  Additionally, it will only be shown until you enter combat." ) );
				if( mConfiguration.ShowAggroDistance )
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
						Vector2 sliderLimits = new( useScreenText ? 0 : -1000, useScreenText ? Math.Max( ImGuiHelpers.MainViewport.Size.X, ImGuiHelpers.MainViewport.Size.Y ) : 1000 );
						ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Text Position", "Position of the aggro widget (X,Y):" ) );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Position", "This is an offset relative to the UI element if it is attached to one, or is an absolute position on the screen if not." ) );
						ImGui.DragFloat2( "###AggroDistanceTextPositionSlider", ref mConfiguration.mAggroDistanceTextPosition, 1f, sliderLimits.X, sliderLimits.Y, "%g", ImGuiSliderFlags.AlwaysClamp );
						ImGui.Checkbox( Loc.Localize( "Config Option: Aggro Distance Text Use Heavy Font", "Use heavy font for aggro widget." ) + "###Aggro Distance font heavy.", ref mConfiguration.mAggroDistanceFontHeavy );
						ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Text Font Size", "Aggro widget font size:" ) );
						ImGui.SliderInt( "###AggroDistanceTextFontSizeSlider", ref mConfiguration.mAggroDistanceFontSize, 6, 36 );
						ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Text Alignment", "Text alignment:" ) );
						ImGui.SliderInt( "###AggroDistanceTextFontAlignmentSlider", ref mConfiguration.mAggroDistanceFontAlignment, 6, 8, "", ImGuiSliderFlags.NoInput );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + "###Show aggro distance units.", ref mConfiguration.mShowUnitsOnAggroDistance );
						ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distance:" ) );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Aggro Distance Precision", "Aggro ranges are only accurate to within ~0.05 yalms, so please be wary when using more than one decimal point of precision." ) );
						ImGui.SliderInt( "###AggroDistancePrecisionSlider", ref mConfiguration.mAggroDistanceDecimalPrecision, 0, 3 );
						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Aggro Widget Colors", "Colors" ) + $"###Aggro Widget Colors Header." ) )
					{
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
						ImGui.TreePop();
					}
				}
			}

			ImGui.PushID( "AggroArcOptions" );
			try
			{
				mAggroArcsUI.DrawConfigOptions();
			}
			finally
			{
				ImGui.PopID();
			}

			if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Aggro Distance Data", "Aggro Distance Data" ) + "###Aggro Distance Data Header." ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Auto Update Aggro Data", "Try to automatically fetch the most recent aggro distance data on startup." ) + "###Auto Update Aggro Data.", ref mConfiguration.mAutoUpdateAggroData );
				if( ImGui.Button( Loc.Localize( "Button: Download Aggro Distances", "Check for Updated Aggro Distances" ) + "###Download updated aggro distances." ) )
				{
					Task.Run( async () =>
					{
						var downloadedFile = await BNpcAggroInfoDownloader.DownloadUpdatedAggroDataAsync( Path.Join( mPluginInterface.GetPluginConfigDirectory(), "AggroDistances.dat" ) );
						if( downloadedFile != null ) BNpcAggroInfo.Init( Service.DataManager, downloadedFile );
					} );
				}
				if( BNpcAggroInfoDownloader.CurrentDownloadStatus != BNpcAggroInfoDownloader.DownloadStatus.None )
				{
					ImGui.Text( Loc.Localize( "Config Text: Download Status Indicator", $"Status of most recent update attempt:" ) + $"\r\n{BNpcAggroInfoDownloader.GetCurrentDownloadStatusMessage()}" );
				}
			}

			if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Nameplate Settings", "Nameplate Settings" ) + "###Nameplate Settings Header." ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Nameplate Distances", "Show distances on nameplates." ) + "###Show nameplate distances.", ref mConfiguration.NameplateDistancesConfig.mShowNameplateDistances );
				if( mConfiguration.NameplateDistancesConfig.ShowNameplateDistances )
				{
					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Distance Rules", "Distance Rules" ) + $"###Nameplate Distance Rules Header." ) )
					{
						ImGui.Checkbox( Loc.Localize( "Config Option: Distance is to Ring", "Show distance to target ring, not target center." ) + $"###Distance is to ring (nameplates).", ref mConfiguration.NameplateDistancesConfig.mDistanceIsToRing );
						ImGui.Text( Loc.Localize( "Config Option: Hide", "Hide:" ) );
						ImGui.SameLine();
						if( ImGui.RadioButton( Loc.Localize( "Config Option: Hide Out Of Combat", "Out of Combat" ) + "###HideOutOfCombatButton", mConfiguration.NameplateDistancesConfig.HideOutOfCombat ) )
						{
							mConfiguration.NameplateDistancesConfig.HideOutOfCombat = true;
							mConfiguration.NameplateDistancesConfig.HideInCombat = false;
						}
						ImGui.SameLine();
						if( ImGui.RadioButton( Loc.Localize( "Config Option: Hide In Combat", "In Combat" ) + "###HideInCombatButton", mConfiguration.NameplateDistancesConfig.HideInCombat ) )
						{
							mConfiguration.NameplateDistancesConfig.HideOutOfCombat = false;
							mConfiguration.NameplateDistancesConfig.HideInCombat = true;
						}
						ImGui.SameLine();
						if( ImGui.RadioButton( Loc.Localize( "Config Option: Hide Neither", "Neither" ) + "###HideNeitherInCombatButton", !mConfiguration.NameplateDistancesConfig.HideOutOfCombat && !mConfiguration.NameplateDistancesConfig.HideInCombat ) )
						{
							mConfiguration.NameplateDistancesConfig.HideOutOfCombat = false;
							mConfiguration.NameplateDistancesConfig.HideInCombat = false;
						}
						ImGui.Text( Loc.Localize( "Config Option: Hide", "Hide:" ) );
						ImGui.SameLine();
						if( ImGui.RadioButton( Loc.Localize( "Config Option: Hide Out Of Instance", "Out of Instance" ) + "###HideOutOfInstanceButton", mConfiguration.NameplateDistancesConfig.HideOutOfInstance ) )
						{
							mConfiguration.NameplateDistancesConfig.HideOutOfInstance = true;
							mConfiguration.NameplateDistancesConfig.HideInInstance = false;
						}
						ImGui.SameLine();
						if( ImGui.RadioButton( Loc.Localize( "Config Option: Hide In Instance", "In Instance" ) + "###HideInInstanceButton", mConfiguration.NameplateDistancesConfig.HideInInstance ) )
						{
							mConfiguration.NameplateDistancesConfig.HideOutOfInstance = false;
							mConfiguration.NameplateDistancesConfig.HideInInstance = true;
						}
						ImGui.SameLine();
						if( ImGui.RadioButton( Loc.Localize( "Config Option: Hide Neither", "Neither" ) + "###HideNeitherInInstanceButton", !mConfiguration.NameplateDistancesConfig.HideOutOfInstance && !mConfiguration.NameplateDistancesConfig.HideInInstance ) )
						{
							mConfiguration.NameplateDistancesConfig.HideOutOfInstance = false;
							mConfiguration.NameplateDistancesConfig.HideInInstance = false;
						}
						ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Show All", "Show distance on all nameplates." ) + $"###Show distance to all nameplates.", ref mConfiguration.NameplateDistancesConfig.mShowAll );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Show All", "Shows distance on all nameplates for any objects that match the object type filters in the next section.  If this is unchecked, additional options will appear below." ) );
						if( !mConfiguration.NameplateDistancesConfig.ShowAll )
						{
							ImGui.Indent();
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Target", "Show distance on target." ) + $"###Show distance to Target (nameplates).", ref mConfiguration.NameplateDistancesConfig.mShowTarget );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Soft Target", "Show distance on soft target." ) + $"###Show distance to Soft Target (nameplates).", ref mConfiguration.NameplateDistancesConfig.mShowSoftTarget );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Focus Target", "Show distance on focus target." ) + $"###Show distance to Focus Target (nameplates).", ref mConfiguration.NameplateDistancesConfig.mShowFocusTarget );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Mouseover Target", "Show distance on mouseover target." ) + $"###Show distance to mouseover target (nameplates).", ref mConfiguration.NameplateDistancesConfig.mShowMouseoverTarget );
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Show Distance on Aggro", "Show distance on enemies aggressive to you." ) + $"###Show distance to aggro (nameplates).", ref mConfiguration.NameplateDistancesConfig.mShowAggressive );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Show Distance on Aggro", "This only applies to targets shown in the enemy list.  Distances for additional enemies aggressive to you will not be shown." ) );
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Show Distance on Party", "Show distance on party members." ) + $"###Show distance to party (nameplates).", ref mConfiguration.NameplateDistancesConfig.mShowPartyMembers );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Show Distance on Party", "This does not apply to cross-world party members." ) );
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Show Distance on Alliance", "Show distance on alliance members." ) + $"###Show distance to alliance (nameplates).", ref mConfiguration.NameplateDistancesConfig.mShowAllianceMembers );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Show Distance on Alliance", "This does not apply to cross-world alliance members." ) );
							ImGui.Checkbox( Loc.Localize( "Config Option: Filters are Exclusive", "Filters are exclusive." ) + $"###Filters are exclusive (nameplates).", ref mConfiguration.NameplateDistancesConfig.mFiltersAreExclusive );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Filters are Exclusive", "If this is checked, distances will be shown only when an object meets both the criteria above AND the filters below.  If it is unchecked, distances will be shown for objects that meet EITHER criteria." ) );
							ImGui.Unindent();
						}
						ImGui.TreePop();
					}
					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Filters", "Object Type Filters" ) + $"###Nameplate Filters Header." ) )
					{
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Players", "Show the distance to players." ) + $"###Show distance to players (nameplates).", ref mConfiguration.NameplateDistancesConfig.Filters.mShowDistanceOnPlayers );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on BattleNpc", "Show the distance to combatant NPCs." ) + $"###Show distance to BattleNpc (nameplates).", ref mConfiguration.NameplateDistancesConfig.Filters.mShowDistanceOnBattleNpc );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventNpc", "Show the distance to non-combatant NPCs." ) + $"###Show distance to EventNpc (nameplates).", ref mConfiguration.NameplateDistancesConfig.Filters.mShowDistanceOnEventNpc );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Treasure", "Show the distance to treasure chests." ) + $"###Show distance to treasure (nameplates).", ref mConfiguration.NameplateDistancesConfig.Filters.mShowDistanceOnTreasure );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Aetheryte", "Show the distance to aetherytes." ) + $"###Show distance to aetheryte (nameplates).", ref mConfiguration.NameplateDistancesConfig.Filters.mShowDistanceOnAetheryte );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Gathering Node", "Show the distance to gathering nodes." ) + $"###Show distance to gathering node (nameplates).", ref mConfiguration.NameplateDistancesConfig.Filters.mShowDistanceOnGatheringNode );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventObj", "Show the distance to interactable objects." ) + $"###Show distance to EventObj (nameplates).", ref mConfiguration.NameplateDistancesConfig.Filters.mShowDistanceOnEventObj );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Companion", "Show the distance to companions." ) + $"###Show distance to companion (nameplates).", ref mConfiguration.NameplateDistancesConfig.Filters.mShowDistanceOnCompanion );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Housing", "Show the distance to housing items." ) + $"###Show distance to housing (nameplates).", ref mConfiguration.NameplateDistancesConfig.Filters.mShowDistanceOnHousing );
						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Classjobs", "Job Filters" ) + "###NameplateClassjobsHeader" ) )
					{
						float maxJobTextWidth = 0;
						float currentJobTextWidth = 0;
						float checkboxWidth = 0;
						float leftMarginPos = 0;
						var classJobDict = ClassJobUtils.ClassJobDict;
						foreach( var entry in classJobDict )
						{
							if( !entry.Value.Abbreviation.IsNullOrEmpty() )
							{
								maxJobTextWidth = Math.Max( maxJobTextWidth, ImGui.CalcTextSize( entry.Value.Abbreviation ).X );
							}
						}
						foreach( var sortCategory in Enum.GetValues<ClassJobData.ClassJobSortCategory>() )
						{
							int displayedJobsCount = 0;
							int rowLength = sortCategory < ClassJobData.ClassJobSortCategory.Class ? 6 : 4;
							for( uint j = 1; j < mConfiguration.NameplateDistancesConfig.Filters.ApplicableClassJobsArray.Length; ++j )
							{
								if( classJobDict.ContainsKey( j ) && classJobDict[j].SortCategory == sortCategory && !classJobDict[j].Abbreviation.IsNullOrEmpty() )
								{
									int colNum = (int) displayedJobsCount % rowLength;
									currentJobTextWidth = ImGui.CalcTextSize( classJobDict[j].Abbreviation ).X;
									if( displayedJobsCount != 0 && colNum != 0 ) ImGui.SameLine( leftMarginPos + ( checkboxWidth + maxJobTextWidth + ImGui.GetStyle().FramePadding.X + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.GetStyle().ItemSpacing.X ) * colNum );
									ImGui.Checkbox( $"{classJobDict[j].Abbreviation}###NameplateClassjob{j}Checkbox", ref mConfiguration.NameplateDistancesConfig.Filters.ApplicableClassJobsArray[j] );

									//	Big kludges, but I'm stupid and don't know a better way.
									if( displayedJobsCount == 0 )
									{
										checkboxWidth = ImGui.GetItemRectSize().Y;
										leftMarginPos = ImGui.GetItemRectMin().X - ImGui.GetWindowPos().X;
									}

									++displayedJobsCount;
								}
							}
						}
						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Appearance", "Appearance" ) + $"###Nameplate Appearance Header." ) )
					{
						Vector2 sliderLimits = new( -300, 300 );
						ImGui.Text( Loc.Localize( "Config Option: Nameplates - Text Offset", "Distance text position offset (X,Y):" ) );
						ImGui.DragFloat2( "###NameplateDistanceTextOffsetSlider", ref mConfiguration.NameplateDistancesConfig.mDistanceTextOffset, 1f, sliderLimits.X, sliderLimits.Y, "%g", ImGuiSliderFlags.AlwaysClamp );
						ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Automatic Alignment", "Automatically position distance text." ) + $"###automatic alignment (nameplates).", ref mConfiguration.NameplateDistancesConfig.mAutomaticallyAlignText );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Automatic Alignment", "If this is checked, the distance text will automatically right, center, or left align to the nameplate text (subject to the offset above).  If unchecked, the distance text will always be at a fixed location, regardless of name length." ) );
						if( mConfiguration.NameplateDistancesConfig.mAutomaticallyAlignText )
						{
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Place Below Name", "Place distance below nameplate." ) + $"###align below name (nameplates).", ref mConfiguration.NameplateDistancesConfig.mPlaceTextBelowName );
						}
						ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Use Heavy Font", "Use heavy font for distance text." ) + $"###Distance font heavy  (nameplates).", ref mConfiguration.NameplateDistancesConfig.mDistanceFontHeavy );
						//ImGui.RadioButton( Loc.Localize( "Config Option: Nameplate Style - Match Game", "Match Game" ) + "###NameplateStyleMatchGameButton", ref mConfiguration.NameplateDistancesConfig.mNameplateStyle, (int)NameplateStyle.MatchGame );
						//ImGui.SameLine();
						//ImGui.RadioButton( Loc.Localize( "Config Option: Nameplate Style - Old", "Old" ) + "###NameplateStyleOldButton", ref mConfiguration.NameplateDistancesConfig.mNameplateStyle, (int)NameplateStyle.Old );
						//ImGui.SameLine();
						//ImGui.RadioButton( Loc.Localize( "Config Option: Nameplate Style - New", "New" ) + "###NameplateStyleNewButton", ref mConfiguration.NameplateDistancesConfig.mNameplateStyle, (int)NameplateStyle.New );
						ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Size", "Distance text font size:" ) );
						ImGui.SliderInt( $"###DistanceTextFontSizeSlider (nameplates)", ref mConfiguration.NameplateDistancesConfig.mDistanceFontSize, 6, 36 );
						ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Alignment", "Text alignment:" ) );
						ImGui.SliderInt( "###DistanceTextFontAlignmentSlider (nameplates)", ref mConfiguration.NameplateDistancesConfig.mDistanceFontAlignment, 6, 8, "", ImGuiSliderFlags.NoInput );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + $"###Show distance units (nameplates).", ref mConfiguration.NameplateDistancesConfig.mShowUnitsOnDistance );
						ImGui.Checkbox( Loc.Localize( "Config Option: Allow Negative Distances", "Allow negative distances." ) + $"###Allow negative distances (nameplates).", ref mConfiguration.NameplateDistancesConfig.mAllowNegativeDistances );
						ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distances:" ) );
						ImGui.SliderInt( $"###DistancePrecisionSlider (nameplates)", ref mConfiguration.NameplateDistancesConfig.mDistanceDecimalPrecision, 0, 3 );

						ImGui.TreePop();
					}
					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Colors", "Colors" ) + $"###Nameplate Colors Header." ) )
					{
						ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Distance-based Colors (Party)", "Use distance-based text colors for party members." ) + $"###Distance Text Use distance-based colors (Nameplates - Party).", ref mConfiguration.NameplateDistancesConfig.mUseDistanceBasedColor_Party );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplate Distance Text Use Distance-based Colors", "Allows you to set different colors for different distance thresholds.  Uses the \"Far\" color if beyond that distance, the \"Mid\" color if between far and near thresholds, and the \"Near\" color if within that distance." ) );
						if( mConfiguration.NameplateDistancesConfig.UseDistanceBasedColor_Party )
						{
							ImGui.Indent();
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Near", "Use default nameplate color when near." ) + $"###Distance Text Use nameplate color (near) (Nameplates - Party).", ref mConfiguration.NameplateDistancesConfig.mNearRangeTextUseNameplateColor_Party );
							if( !mConfiguration.NameplateDistancesConfig.mNearRangeTextUseNameplateColor_Party )
							{
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Near", "Distance text color (near)" ) + $"###DistanceTextColorPicker Near (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.mNearRangeTextColor_Party, ImGuiColorEditFlags.NoInputs );
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Near", "Distance text glow color (near)" ) + $"###DistanceTextEdgeColorPicker Near (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.mNearRangeTextEdgeColor_Party, ImGuiColorEditFlags.NoInputs );
							}
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Mid", "Use default nameplate color when mid." ) + $"###Distance Text Use nameplate color (mid) (Nameplates - Party).", ref mConfiguration.NameplateDistancesConfig.mMidRangeTextUseNameplateColor_Party );
							if( !mConfiguration.NameplateDistancesConfig.mMidRangeTextUseNameplateColor_Party )
							{
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Mid", "Distance text color (mid)" ) + $"###DistanceTextColorPicker Mid (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.mMidRangeTextColor_Party, ImGuiColorEditFlags.NoInputs );
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Mid", "Distance text glow color (mid)" ) + $"###DistanceTextEdgeColorPicker Mid (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.mMidRangeTextEdgeColor_Party, ImGuiColorEditFlags.NoInputs );
							}
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Far", "Use default nameplate color when far." ) + $"###Distance Text Use nameplate color (far) (Nameplates - Party).", ref mConfiguration.NameplateDistancesConfig.mFarRangeTextUseNameplateColor_Party );
							if( !mConfiguration.NameplateDistancesConfig.mFarRangeTextUseNameplateColor_Party )
							{
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Far", "Distancet text color (far)" ) + $"###DistanceTextColorPicker Far (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.mFarRangeTextColor_Party, ImGuiColorEditFlags.NoInputs );
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Far", "Distance text glow color (far)" ) + $"###DistanceTextEdgeColorPicker Far (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.mFarRangeTextEdgeColor_Party, ImGuiColorEditFlags.NoInputs );
							}
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Near Range", "Distance \"near\" range (y):" ) );
							ImGui.DragFloat( $"###DistanceNearRangeSlider (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.mNearThresholdDistance_Party_Yalms, 0.5f, -30f, 30f );
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Far Range", "Distance \"far\" range (y):" ) );
							ImGui.DragFloat( $"###DistanceFarRangeSlider (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.mFarThresholdDistance_Party_Yalms, 0.5f, -30f, 30f );
							ImGui.Unindent();
							ImGui.Spacing();
						}

						ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Distance-based Colors (BNpc)", "Use distance-based text colors for battle NPCs." ) + $"###Distance Text Use distance-based colors (Nameplates - BNpc).", ref mConfiguration.NameplateDistancesConfig.mUseDistanceBasedColor_BNpc );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplate Distance Text Use Distance-based Colors", "Allows you to set different colors for different distance thresholds.  Uses the \"Far\" color if beyond that distance, the \"Mid\" color if between far and near thresholds, and the \"Near\" color if within that distance." ) );
						if( mConfiguration.NameplateDistancesConfig.UseDistanceBasedColor_BNpc )
						{
							ImGui.Indent();
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Near", "Use default nameplate color when near." ) + $"###Distance Text Use nameplate color (near) (Nameplates - BNpc).", ref mConfiguration.NameplateDistancesConfig.mNearRangeTextUseNameplateColor_BNpc );
							if( !mConfiguration.NameplateDistancesConfig.mNearRangeTextUseNameplateColor_BNpc )
							{
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Near", "Distance text color (near)" ) + $"###DistanceTextColorPicker Near (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.mNearRangeTextColor_BNpc, ImGuiColorEditFlags.NoInputs );
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Near", "Distance text glow color (near)" ) + $"###DistanceTextEdgeColorPicker Near (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.mNearRangeTextEdgeColor_BNpc, ImGuiColorEditFlags.NoInputs );
							}
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Mid", "Use default nameplate color when mid." ) + $"###Distance Text Use nameplate color (mid) (Nameplates - BNpc).", ref mConfiguration.NameplateDistancesConfig.mMidRangeTextUseNameplateColor_BNpc );
							if( !mConfiguration.NameplateDistancesConfig.mMidRangeTextUseNameplateColor_BNpc )
							{
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Mid", "Distance text color (mid)" ) + $"###DistanceTextColorPicker Mid (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.mMidRangeTextColor_BNpc, ImGuiColorEditFlags.NoInputs );
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Mid", "Distance text glow color (mid)" ) + $"###DistanceTextEdgeColorPicker Mid (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.mMidRangeTextEdgeColor_BNpc, ImGuiColorEditFlags.NoInputs );
							}
							ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Far", "Use default nameplate color when far." ) + $"###Distance Text Use nameplate color (far) (Nameplates - BNpc).", ref mConfiguration.NameplateDistancesConfig.mFarRangeTextUseNameplateColor_BNpc );
							if( !mConfiguration.NameplateDistancesConfig.mFarRangeTextUseNameplateColor_BNpc )
							{
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Far", "Distancet text color (far)" ) + $"###DistanceTextColorPicker Far (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.mFarRangeTextColor_BNpc, ImGuiColorEditFlags.NoInputs );
								ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Far", "Distance text glow color (far)" ) + $"###DistanceTextEdgeColorPicker Far (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.mFarRangeTextEdgeColor_BNpc, ImGuiColorEditFlags.NoInputs );
							}
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Near Range", "Distance \"near\" range (y):" ) );
							ImGui.DragFloat( $"###DistanceNearRangeSlider (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.mNearThresholdDistance_BNpc_Yalms, 0.5f, -30f, 30f );
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Far Range", "Distance \"far\" range (y):" ) );
							ImGui.DragFloat( $"###DistanceFarRangeSlider (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.mFarThresholdDistance_BNpc_Yalms, 0.5f, -30f, 30f );
							ImGui.Unindent();
						}
						ImGui.TreePop();
					}
				}
			}

			if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Miscellaneous", "Miscellaneous Options" ) + "###Misc. Options Header." ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Suppress Text Command Responses", "Suppress text command responses." ) + "###Suppress text command responses.", ref mConfiguration.mSuppressCommandLineResponses );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Suppress Text Command Responses", "Selecting this prevents any text commands you use from printing responses to chat.  Responses to the help command will always be printed." ) );
			}

			ImGui.Spacing();

			try
			{
				ImGui.PushStyleColor( ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Button] );
				ImGui.PushFont( UiBuilder.IconFont );
				ImGui.Text( "\uF0C1" );
				ImGui.PopFont();
				ImGui.PopStyleColor();
				ImGui.SameLine();
				ImGuiUtils.URLLink( "https://github.com/PunishedPineapple/Distance/wiki/Distances-in-FFXIV", Loc.Localize( "Config Text: Distance Information Link", "About Distance Measurement in FFXIV" ), false, UiBuilder.IconFont );
			}
			catch( Exception e )
			{
				Service.PluginLog.Warning( $"Unable to open the requested link:\r\n{e}" );
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

			ImGui.PushID( "CustomWidgetOptions" );
			try
			{
				mCustomWidgetsUI.DrawConfigOptions();
			}
			finally
			{
				ImGui.PopID();
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

			ImGui.PushID( "CustomArcOptions" );
			try
			{
				mCustomArcsUI.DrawConfigOptions();
			}
			finally
			{
				ImGui.PopID();
			}

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

	internal static bool IsUsableAggroDistanceFormatString( string str )
	{
		if( str.Where( x => { return x == '{'; } ).Count() != 1 || str.Where( x => { return x == '}'; } ).Count() != 1 ) return false;
		var openBraceIndex = str.IndexOf( '{' );
		var closeBraceIndex = str.IndexOf( '}' );
		if( closeBraceIndex - openBraceIndex != 2 ) return false;
		if( str[closeBraceIndex - 1] != '0' ) return false;
		return true;
	}

	private unsafe void DrawDebugWindow()
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
							
			ImGui.Text( "Nameplate Text Flags:" );
			ImGui.Checkbox( "Auto Adjust Size", ref DEBUG_TextFlags_AutoAdjustNodeSize );
			ImGui.Checkbox( "Bold", ref DEBUG_TextFlags_Bold );
			ImGui.Checkbox( "Italic", ref DEBUG_TextFlags_Italic );
			ImGui.Checkbox( "Edge", ref DEBUG_TextFlags_Edge );
			ImGui.Checkbox( "Glare", ref DEBUG_TextFlags_Glare );
			ImGui.Checkbox( "Emboss", ref DEBUG_TextFlags_Emboss );
			ImGui.Checkbox( "Word Wrap", ref DEBUG_TextFlags_WordWrap );
			ImGui.Checkbox( "Multi-line", ref DEBUG_TextFlags_MultiLine );

			ImGui.Text( "Nameplate Text Flags 2:" );
			ImGui.Checkbox( "Unknown 1", ref DEBUG_TextFlags2_Unknown1 );
			ImGui.Checkbox( "Unknown 2", ref DEBUG_TextFlags2_Unknown2 );
			ImGui.Checkbox( "Ellipsis", ref DEBUG_TextFlags2_Ellipsis );
			ImGui.Checkbox( "Unknown 8", ref DEBUG_TextFlags2_Unknown8 );
			ImGui.Checkbox( "Unknown 16 (New Nameplates)", ref DEBUG_TextFlags2_Unknown16 );
			ImGui.Checkbox( "Unknown 32", ref DEBUG_TextFlags2_Unknown32 );
			ImGui.Checkbox( "Unknown 64 (New Nameplates)", ref DEBUG_TextFlags2_Unknown64 );
			ImGui.Checkbox( "Unknown 128", ref DEBUG_TextFlags2_Unknown128 );

			if( ImGui.Button( "Set Nameplate Text Flags" ) ) NameplateHandler.DEBUG_mSetTextFlags = true;

			NameplateHandler.DEBUG_mNameplateTextFlags = 0;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_AutoAdjustNodeSize ? 1 : 0 ) << 0;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Bold ? 1 : 0 ) << 1;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Italic ? 1 : 0 ) << 2;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Edge ? 1 : 0 ) << 3;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Glare ? 1 : 0 ) << 4;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Emboss ? 1 : 0 ) << 5;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_WordWrap ? 1 : 0 ) << 6;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_MultiLine ? 1 : 0 ) << 7;

			NameplateHandler.DEBUG_mNameplateTextFlags2 = 0;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown1 ? 1 : 0 ) << 0;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown2 ? 1 : 0 ) << 1;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Ellipsis ? 1 : 0 ) << 2;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown8 ? 1 : 0 ) << 3;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown16 ? 1 : 0 ) << 4;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown32 ? 1 : 0 ) << 5;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown64 ? 1 : 0 ) << 6;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown128 ? 1 : 0 ) << 7;

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.Text( "Performance Timers:" );
			ImGui.Text( $"Nameplate Distance Update Time: {NameplateHandler.DistanceUpdateTime_uSec}μs" );
			ImGui.Text( $"Nameplate Node Configuration Time: {NameplateHandler.NodeUpdateTime_uSec}μs" );
			ImGui.Text( $"Widget Node Configuration Time: {mWidgetNodeUpdateTime_uSec}μs" );
			ImGui.Text( $"Overlay Draw Time: {mOverlayDrawTime_uSec}μs" );

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.Text( "Addresses:" );
			ImGui.Text( $"Nameplate Addon: 0x{Service.GameGui.GetAddonByName( "NamePlate", 1 ):X}" );
			ImGui.Text( $"Nameplate Addon (Cached): 0x{NameplateHandler.DEBUG_CachedNameplateAddonPtr:X}" );

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.Text( $"TerritoryType: {Service.ClientState.TerritoryType}" );

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

	private void DrawDebugAggroEntitiesWindow()
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

	private void DrawDebugNameplateInfoWindow()
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
			for( int i = 0; i < NameplateHandler.DEBUG_NameplateDistanceInfo.Length; ++i )
			{
				ImGui.Text( $"{i}:" );
				ImGui.Text( NameplateHandler.DEBUG_NameplateDistanceInfo[i].ToString() );
				ImGui.Text( $"Should draw?: {NameplateHandler.DEBUG_ShouldDrawDistanceInfo[i]}" );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
			}
		}
	}

	private void DrawOverlay()
	{
		if( ShouldHideUIOverlays() ) return;

		ImGuiHelpers.ForceNextWindowMainViewport();
		ImGui.SetNextWindowPos( ImGui.GetMainViewport().Pos );
		ImGui.SetNextWindowSize( ImGui.GetMainViewport().Size );
		if( ImGui.Begin( "##AggroDistanceIndicatorWindow", ImGuiUtils.OverlayWindowFlags ) )
		{
			mAggroArcsUI.DrawOnOverlay();
			mCustomArcsUI.DrawOnOverlay();
		}

		ImGui.End();
	}

	private void DrawOnGameUI()
	{
		//	Draw the aggro widget.
		UpdateAggroDistanceTextNode( mPlugin.GetDistanceInfo( mConfiguration.AggroDistanceApplicableTargetType ), mConfiguration.ShowAggroDistance && mPlugin.ShouldDrawAggroDistanceInfo() );

		//	Draw each configured distance widget.
		for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; ++i )
		{
			UpdateDistanceTextNode( (uint)i,
									mPlugin.GetDistanceInfo( mConfiguration.DistanceWidgetConfigs[i].ApplicableTargetType ),
									mConfiguration.DistanceWidgetConfigs[i],
									mPlugin.ShouldDrawDistanceInfo( mConfiguration.DistanceWidgetConfigs[i] ) );
		}

		//	Note: Nameplate drawing is handled in NameplateHandler.
	}

	private unsafe void UpdateDistanceTextNode( uint distanceWidgetNumber, DistanceInfo distanceInfo, DistanceWidgetConfig config, bool show )
	{
		if( !show ) HideTextNode( mDistanceNodeIDBase + distanceWidgetNumber );

		string str = "";
		byte nodeAlphaToUse = 255;
		Vector4 textColorToUse = config.TextColor;
		Vector4 edgeColorToUse = config.TextEdgeColor;

		if( distanceInfo.IsValid )
		{
			float distance = config.DistanceIsToRing ? distanceInfo.DistanceFromTargetRing_Yalms : distanceInfo.DistanceFromTarget_Yalms;
			distance -= config.DistanceOffset_Yalms;
			float displayDistance = config.AllowNegativeDistances ? distance : Math.Max( 0, distance );
			string unitString = config.ShowUnits ? "y" : "";
			string distanceTypeSymbol = "";
			if( config.ShowDistanceModeMarker ) distanceTypeSymbol = config.DistanceIsToRing ? "◯ " : "· ";
			str = $"{distanceTypeSymbol}{displayDistance.ToString( $"F{config.DecimalPrecision}" )}{unitString}";

			if( config.UseDistanceBasedColor )
			{
				if( distance > config.FarThresholdDistance_Yalms )
				{
					textColorToUse = config.FarThresholdTextColor;
					edgeColorToUse = config.FarThresholdTextEdgeColor;
				}
				else if( distance > config.NearThresholdDistance_Yalms )
				{
					textColorToUse = config.NearThresholdTextColor;
					edgeColorToUse = config.NearThresholdTextEdgeColor;
				}
			}
		}

		if( config.TrackTargetBarTextColor )
		{
			AtkUnitBase* pTargetAddonToUse = null;
			UInt16 targetBarNameNodeIndex = 0;
			var pTargetAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfo", 1 );
			var pTargetAddonSplit = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
			var pFocusTargetAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_FocusTargetInfo", 1 );
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

					nodeAlphaToUse = pTargetNameTextNode->AtkResNode.Color.A;

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
			Alpha = nodeAlphaToUse,
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

	private unsafe void UpdateAggroDistanceTextNode( DistanceInfo distanceInfo, bool show )
	{
		if( !show ) HideTextNode( mAggroDistanceNodeID );

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
			Alpha = 255,	//***** TODO: Should probably grab this off of the focus target addon when this node is attached to focus target bar, since that can fade out a bit.
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

	private unsafe void UpdateTextNode( GameAddonEnum addonToUse, uint nodeID, string str, TextNodeDrawData drawData, bool show = true )
	{
		AtkTextNode* pNode = null;
		AtkUnitBase* pAddon = null;

		var pNormalTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfo", 1 );
		var pSplitTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
		var pFocusTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_FocusTargetInfo", 1 );
		var pScreenTextAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_ScreenText", 1 );
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

					pNode->AtkResNode.Color.A = drawData.Alpha;

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

	internal static unsafe void HideTextNode( uint nodeID )
	{
		//	Get the possible addons we could be using.
		var pNormalTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfo", 1 );
		var pSplitTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
		var pFocusTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_FocusTargetInfo", 1 );
		var pScreenTextAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_ScreenText", 1 );

		//	Hide the node(s) with the specified ID in any of those addons.
		if( pScreenTextAddon != null ) AtkNodeHelpers.HideNode( pScreenTextAddon, nodeID );
		if( pFocusTargetBarAddon != null ) AtkNodeHelpers.HideNode( pFocusTargetBarAddon, nodeID );
		if( pNormalTargetBarAddon != null ) AtkNodeHelpers.HideNode( pNormalTargetBarAddon, nodeID );
		if( pSplitTargetBarAddon != null ) AtkNodeHelpers.HideNode( pSplitTargetBarAddon, nodeID );
	}

	private unsafe bool AddonIsVisible( GameAddonEnum addon )
	{
		switch( addon )
		{
			case GameAddonEnum.TargetBar:
				var pNormalTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfo", 1 );
				var pSplitTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
				return ( pNormalTargetBarAddon != null && pNormalTargetBarAddon->IsVisible ) || ( pSplitTargetBarAddon != null && pSplitTargetBarAddon->IsVisible );
			case GameAddonEnum.FocusTargetBar:
				var pFocusTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_FocusTargetInfo", 1 );
				return pFocusTargetBarAddon != null && pFocusTargetBarAddon->IsVisible;
			default:
				return false;
		}
	}

	private bool ShouldHideUIOverlays()
	{
		return	Service.GameGui.GameUiHidden ||
				Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInCutSceneEvent] ||
				Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene] ||
				Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.CreatingCharacter];
	}

	private readonly Plugin mPlugin;
	private readonly DalamudPluginInterface mPluginInterface;
	private readonly Configuration mConfiguration;

	private readonly PluginUI_AggroArcs mAggroArcsUI;
	private readonly PluginUI_CustomWidgets mCustomWidgetsUI;
	private readonly PluginUI_CustomArcs mCustomArcsUI;

	//	Need a real backing field on the following properties for use with ImGui.
	private bool mSettingsWindowVisible = false;
	internal bool SettingsWindowVisible
	{
		get { return mSettingsWindowVisible; }
		set { mSettingsWindowVisible = value; }
	}

	private bool mDebugWindowVisible = false;
	internal bool DebugWindowVisible
	{
		get { return mDebugWindowVisible; }
		set { mDebugWindowVisible = value; }
	}

	private bool mDebugAggroEntitiesWindowVisible = false;
	internal bool DebugAggroEntitiesWindowVisible
	{
		get { return mDebugAggroEntitiesWindowVisible; }
		set { mDebugAggroEntitiesWindowVisible = value; }
	}

	private bool mDebugNameplateInfoWindowVisible = false;
	internal bool DebugNameplateInfoWindowVisible
	{
		get { return mDebugNameplateInfoWindowVisible; }
		set { mDebugNameplateInfoWindowVisible = value; }
	}

	private bool DEBUG_TextFlags_AutoAdjustNodeSize = false;
	private bool DEBUG_TextFlags_Bold = false;
	private bool DEBUG_TextFlags_Italic = false;
	private bool DEBUG_TextFlags_Edge = true;
	private bool DEBUG_TextFlags_Glare = true;
	private bool DEBUG_TextFlags_Emboss = false;
	private bool DEBUG_TextFlags_WordWrap = false;
	private bool DEBUG_TextFlags_MultiLine = false;

	private bool DEBUG_TextFlags2_Unknown1 = false;
	private bool DEBUG_TextFlags2_Unknown2 = false;
	private bool DEBUG_TextFlags2_Ellipsis = false;
	private bool DEBUG_TextFlags2_Unknown8 = false;
	private bool DEBUG_TextFlags2_Unknown16 = false;
	private bool DEBUG_TextFlags2_Unknown32 = false;
	private bool DEBUG_TextFlags2_Unknown64 = false;
	private bool DEBUG_TextFlags2_Unknown128 = false;

	//	Do this to control the order of dropdown items.
	internal static readonly TargetType[] TargetDropdownMenuItems =
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

	internal static readonly string[] UIAttachDropdownOptions =
	{
		AddonAttachType.Auto.GetTranslatedName(),
		AddonAttachType.ScreenText.GetTranslatedName(),
		AddonAttachType.Target.GetTranslatedName(),
		AddonAttachType.FocusTarget.GetTranslatedName(),
		AddonAttachType.Cursor.GetTranslatedName(),
		//AddonAttachType.Nameplate.GetTranslatedUIAttachTypeEnumString(),
	};

	private const UInt16 FocusTargetBarColorNodeIndex = 10;
	private const UInt16 TargetBarColorNodeIndex = 39;
	private const UInt16 SplitTargetBarColorNodeIndex = 8;
	private const UInt16 TargetOfTargetBarColorNodeIndex = 49;
	private const UInt16 SplitTargetOfTargetBarColorNodeIndex = 12;

	private readonly Stopwatch mWidgetNodeUpdateTimer = new();
	private readonly Stopwatch mOverlayDrawTimer = new();
	private Int64 mWidgetNodeUpdateTime_uSec = 0;
	private Int64 mOverlayDrawTime_uSec = 0;

	//	Note: Node IDs only need to be unique within a given addon.
	internal const uint mDistanceNodeIDBase = 0x6C78B300;    //YOLO hoping for no collisions.
	internal const uint mAggroDistanceNodeID = mDistanceNodeIDBase - 1;
}