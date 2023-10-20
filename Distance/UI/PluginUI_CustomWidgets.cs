using System;
using System.Numerics;

using CheapLoc;

using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Distance;

internal sealed class PluginUI_CustomWidgets : IDisposable
{
	public PluginUI_CustomWidgets( Plugin plugin, PluginUI ui, Configuration configuration )
	{
		mPlugin = plugin;
		mUI = ui;
		mConfiguration = configuration;
	}

	public void Dispose()
	{
	}

	public void DrawConfigOptions()
	{
		if( ImGui.Button( Loc.Localize( "Button: Add Distance Widget", "Add Widget" ) + "###AddWidgetButton" ) )
		{
			mConfiguration.DistanceWidgetConfigs.Add( new() );
		}

		int widgetIndexToDelete = -1;
		for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; ++i )
		{
			ImGui.PushID( i );
			try
			{
				var config = mConfiguration.DistanceWidgetConfigs[i];
				var filters = config.Filters;
				string name = config.mWidgetName.Length > 0 ? config.mWidgetName : config.ApplicableTargetType.GetTranslatedName();
				if( ImGui.CollapsingHeader( String.Format( Loc.Localize( "Config Section Header: Distance Widget", "Distance Widget ({0})" ), name ) + "###DistanceWidgetHeader" ) )
				{
					ImGui.Text( Loc.Localize( "Config Option: Widget Name", "Widget Name:" ) );
					ImGui.SameLine();
					ImGui.InputTextWithHint( "###WidgetNameInputBox", config.ApplicableTargetType.GetTranslatedName(), ref config.mWidgetName, 50 );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Widget Name", "This is used to give a customized name to this widget for use with certain text commands.  If you leave it blank, the type of target for this widget will be used in the header above, but it will not have a name for use in text commands." ) );
					ImGui.Checkbox( Loc.Localize( "Config Option: Widget Enabled", "Enabled" ) + "###WidgetEnabledCheckbox", ref config.mEnabled );
					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Rules", "Distance Rules" ) + "###DistanceWidgetRulesHeader" ) )
					{
						ImGui.Text( Loc.Localize( "Config Option: Target Type", "Target Type:" ) );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Applicable Target Type", "The type of target for which this widget will show distance.  \"Soft Target\" generally only matters for controller players and some two-handed keyboard players.  \"Field Mouseover\" is for when you mouseover an object in the world.  \"UI Mouseover\" is for when you mouseover the party list." ) );
						if( ImGui.BeginCombo( "###DistanceTypeDropdown", config.ApplicableTargetType.GetTranslatedName() ) )
						{
							foreach( var item in PluginUI.TargetDropdownMenuItems )
							{
								if( ImGui.Selectable( item.GetTranslatedName(), config.ApplicableTargetType == item ) )
								{
									config.ApplicableTargetType = item;
								}
							}
							ImGui.EndCombo();
						}
						ImGui.Checkbox( Loc.Localize( "Config Option: Distance is to Ring", "Show distance to target ring, not target center." ) + "###DistanceIsToRing", ref config.mDistanceIsToRing );
						ImGui.Text( Loc.Localize( "Config Option: Distance Measurement Offset", "Amount to offset the distance readout (y):" ) );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Readout Offset", "This value is subtracted from the real distance to determine the displayed distance.  This can be used to get the widget to show the distance from being able to hit the boss with a skill, for example." ) );
						ImGui.DragFloat( "###DistanceOffsetSlider", ref config.mDistanceOffset_Yalms, 0.1f, -30f, 30f );
						ImGui.Checkbox( Loc.Localize( "Config Option: Hide In Combat", "Hide when in combat." ) + "###HideInCombat", ref config.mHideInCombat );
						ImGui.Checkbox( Loc.Localize( "Config Option: Hide Out Of Combat", "Hide when out of combat." ) + "###HideOutOfCombat", ref config.mHideOutOfCombat );
						ImGui.Checkbox( Loc.Localize( "Config Option: Hide In Instance", "Hide when in an instance." ) + "###HideInInstance", ref config.mHideInInstance );
						ImGui.Checkbox( Loc.Localize( "Config Option: Hide Out Of Instance", "Hide when out of an instance." ) + "###HideOutOfInstance", ref config.mHideOutOfInstance );
						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Filters", "Object Type Filters" ) + "###DistanceWidgetFiltersHeader" ) )
					{
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Players", "Show the distance to players." ) + "###Show distance to players", ref filters.mShowDistanceOnPlayers );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on BattleNpc", "Show the distance to combatant NPCs." ) + "###Show distance to BattleNpc", ref filters.mShowDistanceOnBattleNpc );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventNpc", "Show the distance to non-combatant NPCs." ) + "###Show distance to EventNpc", ref filters.mShowDistanceOnEventNpc );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Treasure", "Show the distance to treasure chests." ) + "###Show distance to treasure", ref filters.mShowDistanceOnTreasure );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Aetheryte", "Show the distance to aetherytes." ) + "###Show distance to aetheryte", ref filters.mShowDistanceOnAetheryte );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Gathering Node", "Show the distance to gathering nodes." ) + "###Show distance to gathering node", ref filters.mShowDistanceOnGatheringNode );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventObj", "Show the distance to interactable objects." ) + "###Show distance to EventObj", ref filters.mShowDistanceOnEventObj );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Companion", "Show the distance to companions." ) + "###Show distance to companion", ref filters.mShowDistanceOnCompanion );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Housing", "Show the distance to housing items." ) + "###Show distance to housing", ref filters.mShowDistanceOnHousing );
						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Classjobs", "Job Filters" ) + "###DistanceWidgetClassjobsHeader" ) )
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
							for( uint j = 1; j < config.Filters.ApplicableClassJobsArray.Length; ++j )
							{
								if( classJobDict.ContainsKey( j ) && classJobDict[j].SortCategory == sortCategory && !classJobDict[j].Abbreviation.IsNullOrEmpty() )
								{
									int colNum = (int) displayedJobsCount % rowLength;
									currentJobTextWidth = ImGui.CalcTextSize( classJobDict[j].Abbreviation ).X;
									if( displayedJobsCount != 0 && colNum != 0 ) ImGui.SameLine( leftMarginPos + ( checkboxWidth + maxJobTextWidth + ImGui.GetStyle().FramePadding.X + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.GetStyle().ItemSpacing.X ) * colNum );
									ImGui.Checkbox( $"{classJobDict[j].Abbreviation}###WidgetClassjob{j}Checkbox", ref config.Filters.ApplicableClassJobsArray[j] );

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

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Appearance", "Appearance" ) + "###DistanceWidgetAppearanceHeader" ) )
					{
						ImGui.Text( Loc.Localize( "Config Option: UI Attach Point", "UI Binding:" ) );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: UI Attach Point", "This is the UI element to which you wish to attach this widget.  \"Automatic\" tries to infer the best choice based on the target type you've selected for this widget.  \"Screen Space\" does not attach to a specific UI element, but floats above everything.  The others should be self-explanatory.  Note: Attaching to the mouse cursor can look odd if you use the hardware cursor; switch to the software cursor in the game options if necessary." ) );
						ImGui.Combo( "###UIAttachTypeDropdown", ref config.mUIAttachType, PluginUI.UIAttachDropdownOptions, PluginUI.UIAttachDropdownOptions.Length );
						bool useScreenText = config.UIAttachType.GetGameAddonToUse( config.ApplicableTargetType ) == GameAddonEnum.ScreenText;
						Vector2 sliderLimits = new( useScreenText ? 0 : -1000, useScreenText ? Math.Max( ImGuiHelpers.MainViewport.Size.X, ImGuiHelpers.MainViewport.Size.Y ) : 1000 );
						ImGui.Text( Loc.Localize( "Config Option: Distance Text Position", "Position of the distance readout (X,Y):" ) );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Position", "This is an offset relative to the UI element if it is attached to one, or is an absolute position on the screen if not." ) );
						ImGui.DragFloat2( "###DistanceTextPositionSlider", ref config.mTextPosition, 1f, sliderLimits.X, sliderLimits.Y, "%g", ImGuiSliderFlags.AlwaysClamp );
						ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Use Heavy Font", "Use heavy font for distance text." ) + "###DistanceFontHeavy", ref config.mFontHeavy );
						ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Size", "Distance text font size:" ) );
						ImGui.SliderInt( "###DistanceTextFontSizeSlider", ref config.mFontSize, 6, 36 );
						ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Alignment", "Text alignment:" ) );
						ImGui.SliderInt( "###DistanceTextFontAlignmentSlider", ref config.mFontAlignment, 6, 8, "", ImGuiSliderFlags.NoInput );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + "###ShowDistanceUnits", ref config.mShowUnits );
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Mode Indicator", "Show the distance mode indicator." ) + "###ShowDistanceTypeMarker", ref config.mShowDistanceModeMarker );
						ImGui.Checkbox( Loc.Localize( "Config Option: Allow Negative Distances", "Allow negative distances." ) + "###AllowNegativeDistances", ref config.mAllowNegativeDistances );
						ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distances:" ) );
						ImGui.SliderInt( "###DistancePrecisionSlider", ref config.mDecimalPrecision, 0, 3 );
						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Colors", "Colors" ) + "###DistanceWidgetColorsHeader" ) )
					{
						ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Track Target Bar Color", "Attempt to use target bar text color." ) + "###DistanceTextUseTargetBarColor", ref config.mTrackTargetBarTextColor );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Track Target Bar Color", "If the color of the target bar text (or focus target) can be determined, it will take precedence; otherwise the colors set below will be used." ) );
						ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Use Distance-based Colors", "Use distance-based text colors." ) + "###DistanceTextUseDistanceBasedColors", ref config.mUseDistanceBasedColor );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Use Distance-based Colors", "Allows you to set different colors for different distance thresholds.  Uses the \"Far\" color if beyond that distance, otherwise the \"Near\" color if beyond that distance, otherwise uses the base color specified above.  This setting is ignored if the checkbox to track the target bar color is ticked." ) );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color", "Distance text color" ) + "###DistanceTextColorPicker", ref config.mTextColor, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color", "Distance text glow color" ) + "###DistanceTextEdgeColorPicker", ref config.mTextEdgeColor, ImGuiColorEditFlags.NoInputs );
						if( config.UseDistanceBasedColor )
						{
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Far", "Distance text color (near)" ) + "###DistanceTextColorPickerNear", ref config.mNearThresholdTextColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Far", "Distance text glow color (near)" ) + "###DistanceTextEdgeColorPickerNear", ref config.mNearThresholdTextEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Far", "Distancet text color (far)" ) + "###DistanceTextColorPickerFar", ref config.mFarThresholdTextColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Far", "Distance text glow color (far)" ) + "###DistanceTextEdgeColorPickerFar", ref config.mFarThresholdTextEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Near Range", "Distance \"near\" range (y):" ) );
							ImGui.DragFloat( "###DistanceNearRangeSlider", ref config.mNearThresholdDistance_Yalms, 0.5f, -30f, 30f );
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Far Range", "Distance \"far\" range (y):" ) );
							ImGui.DragFloat( "###DistanceFarRangeSlider", ref config.mFarThresholdDistance_Yalms, 0.5f, -30f, 30f );
						}
						ImGui.TreePop();
					}

					if( ImGui.Button( Loc.Localize( "Button: Delete Distance Widget", "Delete Widget" ) + "###DeleteWidgetButton" ) )
					{
						mWidgetIndexWantToDelete = i;
					}
					if( mWidgetIndexWantToDelete == i )
					{
						ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );
						ImGui.Text( Loc.Localize( "Settings Window Text: Confirm Delete Label", "Confirm delete: " ) );
						ImGui.SameLine();
						if( ImGui.Button( Loc.Localize( "Button: Yes", "Yes" ) + "###DeleteWidgetYesButton" ) )
						{
							widgetIndexToDelete = mWidgetIndexWantToDelete;
						}
						ImGui.PopStyleColor();
						ImGui.SameLine();
						if( ImGui.Button( Loc.Localize( "Button: No", "No" ) + "###DeleteWidgetNoButton" ) )
						{
							mWidgetIndexWantToDelete = -1;
						}
					}
				}
			}
			finally
			{
				ImGui.PopID();
			}
		}
		if( widgetIndexToDelete > -1 && widgetIndexToDelete < mConfiguration.DistanceWidgetConfigs.Count )
		{
			// Hide the last node since we've reduced count by one, making the highest node effectively
			// dormant.  The visibilities for all lower nodes will be updated on the next frame.
			PluginUI.HideTextNode( PluginUI.mDistanceNodeIDBase + (uint)mConfiguration.DistanceWidgetConfigs.Count - 1 );
			mConfiguration.DistanceWidgetConfigs.RemoveAt( widgetIndexToDelete );
			mWidgetIndexWantToDelete = -1;
		}
	}

	private readonly Plugin mPlugin;
	private readonly PluginUI mUI;
	private readonly Configuration mConfiguration;
	private int mWidgetIndexWantToDelete = -1;
}
