using System;
using System.Numerics;

using CheapLoc;

using Dalamud.Bindings.ImGui;

namespace Distance;

internal sealed class PluginUI_Nameplates : IDisposable
{
	internal PluginUI_Nameplates( Plugin plugin, PluginUI ui, Configuration configuration )
	{
		mPlugin = plugin;
		mUI = ui;
		mConfiguration = configuration;
	}

	public void Dispose()
	{
	}

	internal void DrawConfigOptions()
	{
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Nameplate Distances", "Show distances on nameplates." ) + "###Show nameplate distances.", ref mConfiguration.NameplateDistancesConfig.ShowNameplateDistances );
		if( mConfiguration.NameplateDistancesConfig.ShowNameplateDistances )
		{
			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Distance Rules", "Distance Rules" ) + $"###Nameplate Distance Rules Header." ) )
			{
				ImGui.Checkbox( Loc.Localize("Config Option: Distance is to Ring for nameplates", "Show distance to target ring, not target center." ) + $"###Distance is to ring (nameplates).", ref mConfiguration.NameplateDistancesConfig.DistanceIsToRing );
				ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Show All", "Show distance on all nameplates." ) + $"###Show distance to all nameplates.", ref mConfiguration.NameplateDistancesConfig.ShowAll );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Show All", "Shows distance on all nameplates for any objects that match the object type filters in the next section.  If this is unchecked, additional options will appear below." ) );
				if( !mConfiguration.NameplateDistancesConfig.ShowAll )
				{
					ImGui.Indent();
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Target", "Show distance on target." ) + $"###Show distance to Target (nameplates).", ref mConfiguration.NameplateDistancesConfig.ShowTarget );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Soft Target", "Show distance on soft target." ) + $"###Show distance to Soft Target (nameplates).", ref mConfiguration.NameplateDistancesConfig.ShowSoftTarget );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Focus Target", "Show distance on focus target." ) + $"###Show distance to Focus Target (nameplates).", ref mConfiguration.NameplateDistancesConfig.ShowFocusTarget );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Mouseover Target", "Show distance on mouseover target." ) + $"###Show distance to mouseover target (nameplates).", ref mConfiguration.NameplateDistancesConfig.ShowMouseoverTarget );
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Show Distance on Aggro", "Show distance on enemies aggressive to you." ) + $"###Show distance to aggro (nameplates).", ref mConfiguration.NameplateDistancesConfig.ShowAggressive );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Show Distance on Aggro", "This only applies to targets shown in the enemy list.  Distances for additional enemies aggressive to you will not be shown." ) );
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Show Distance on Party", "Show distance on party members." ) + $"###Show distance to party (nameplates).", ref mConfiguration.NameplateDistancesConfig.ShowPartyMembers );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Show Distance on Party", "This does not apply to cross-world party members until you have entered an instanced duty." ) );
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Show Distance on Alliance", "Show distance on alliance members." ) + $"###Show distance to alliance (nameplates).", ref mConfiguration.NameplateDistancesConfig.ShowAllianceMembers );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Show Distance on Alliance", "This does not apply to cross-world alliance members until you have entered an instanced duty." ) );
					ImGui.Checkbox( Loc.Localize( "Config Option: Filters are Exclusive", "Filters are exclusive." ) + $"###Filters are exclusive (nameplates).", ref mConfiguration.NameplateDistancesConfig.FiltersAreExclusive );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Filters are Exclusive", "If this is checked, distances will be shown only when an object meets both the criteria above AND the filters below.  If it is unchecked, distances will be shown for objects that meet EITHER criteria." ) );
					ImGui.Unindent();
				}
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Offsets", "Distance Offsets" ) + $"###NameplateOffsetsHeader." ) )
			{
				ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Measurement Offset (Player)", "Amount to offset the distance readout for players ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Readout Offset", "This value is subtracted from the real distance to determine the displayed distance.  This can be used to get the widget to show the distance from being able to hit the boss with a skill, for example." ) );
				ImGui.DragFloat( "###DistanceOffsetSlider_Player", ref mConfiguration.NameplateDistancesConfig.DistanceOffset_Player_Yalms, 0.1f, -30f, 30f );
				ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Measurement Offset (BNpc)", "Amount to offset the distance readout for combatant NPCs ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Readout Offset", "This value is subtracted from the real distance to determine the displayed distance.  This can be used to get the widget to show the distance from being able to hit the boss with a skill, for example." ) );
				ImGui.DragFloat( "###DistanceOffsetSlider_BNpc", ref mConfiguration.NameplateDistancesConfig.DistanceOffset_BNpc_Yalms, 0.1f, -30f, 30f );
				ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Measurement Offset (Other)", "Amount to offset the distance readout for all other entities ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Readout Offset", "This value is subtracted from the real distance to determine the displayed distance.  This can be used to get the widget to show the distance from being able to hit the boss with a skill, for example." ) );
				ImGui.DragFloat( "###DistanceOffsetSlider_Other", ref mConfiguration.NameplateDistancesConfig.DistanceOffset_Other_Yalms, 0.1f, -30f, 30f );
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Filters", "Object Type Filters" ) + $"###NameplateFiltersHeader." ) )
			{
				mConfiguration.NameplateDistancesConfig.Filters.DrawObjectKindOptions();
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize("Config Section Header: Distance Nameplate Condition", "Condition Filters" ) + "###NameplateConditionsHeader" ) )
			{
				mConfiguration.NameplateDistancesConfig.Filters.DrawConditionOptions();
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Classjobs", "Job Filters" ) + "###NameplateClassjobsHeader" ) )
			{
				mConfiguration.NameplateDistancesConfig.Filters.DrawClassJobOptions();
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Appearance", "Appearance" ) + $"###Nameplate Appearance Header." ) )
			{
				Vector2 sliderLimits = new( -300, 300 );
				ImGui.Text( Loc.Localize( "Config Option: Nameplates - Text Offset", "Distance text position offset (X,Y):" ) );
				ImGui.DragFloat2( "###NameplateDistanceTextOffsetSlider", ref mConfiguration.NameplateDistancesConfig.DistanceTextOffset, 1f, sliderLimits.X, sliderLimits.Y, "%g", ImGuiSliderFlags.AlwaysClamp );
				ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Automatic Alignment", "Automatically position distance text." ) + $"###automatic alignment (nameplates).", ref mConfiguration.NameplateDistancesConfig.AutomaticallyAlignText );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplates - Automatic Alignment", "If this is checked, the distance text will automatically right, center, or left align to the nameplate text (subject to the offset above).  If unchecked, the distance text will always be at a fixed location, regardless of name length." ) );
				if( mConfiguration.NameplateDistancesConfig.AutomaticallyAlignText )
				{
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplates - Place Below Name", "Place distance below nameplate." ) + $"###align below name (nameplates).", ref mConfiguration.NameplateDistancesConfig.PlaceTextBelowName );
				}
				ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Use Heavy Font", "Use heavy font for distance text." ) + $"###Distance font heavy  (nameplates).", ref mConfiguration.NameplateDistancesConfig.DistanceFontHeavy );
				//ImGui.RadioButton( Loc.Localize( "Config Option: Nameplate Style - Match Game", "Match Game" ) + "###NameplateStyleMatchGameButton", ref mConfiguration.NameplateDistancesConfig.mNameplateStyle, (int)NameplateStyle.MatchGame );
				//ImGui.SameLine();
				//ImGui.RadioButton( Loc.Localize( "Config Option: Nameplate Style - Old", "Old" ) + "###NameplateStyleOldButton", ref mConfiguration.NameplateDistancesConfig.mNameplateStyle, (int)NameplateStyle.Old );
				//ImGui.SameLine();
				//ImGui.RadioButton( Loc.Localize( "Config Option: Nameplate Style - New", "New" ) + "###NameplateStyleNewButton", ref mConfiguration.NameplateDistancesConfig.mNameplateStyle, (int)NameplateStyle.New );
				ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Size", "Distance text font size:" ) );
				ImGui.SliderInt( $"###DistanceTextFontSizeSlider (nameplates)", ref mConfiguration.NameplateDistancesConfig.DistanceFontSize, 6, 36 );
				ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Alignment", "Text alignment:" ) );
				ImGui.SliderInt( "###DistanceTextFontAlignmentSlider (nameplates)", ref mConfiguration.NameplateDistancesConfig.mDistanceFontAlignment, 6, 8, "", ImGuiSliderFlags.NoInput );
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + $"###Show distance units (nameplates).", ref mConfiguration.NameplateDistancesConfig.ShowUnitsOnDistance );
				ImGui.Checkbox( Loc.Localize( "Config Option: Allow Negative Distances", "Allow negative distances." ) + $"###Allow negative distances (nameplates).", ref mConfiguration.NameplateDistancesConfig.AllowNegativeDistances );
				ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distances:" ) );
				ImGui.SliderInt( $"###DistancePrecisionSlider (nameplates)", ref mConfiguration.NameplateDistancesConfig.DistanceDecimalPrecision, 0, 3 );

				ImGui.TreePop();
			}
			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Colors", "Colors" ) + $"###Nameplate Colors Header." ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Distance-based Colors (Party)", "Use distance-based text colors for party members." ) + $"###Distance Text Use distance-based colors (Nameplates - Party).", ref mConfiguration.NameplateDistancesConfig.UseDistanceBasedColor_Party );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplate Distance Text Use Distance-based Colors", "Allows you to set different colors for different distance thresholds.  Uses the \"Far\" color if beyond that distance, the \"Mid\" color if between far and near thresholds, and the \"Near\" color if within that distance." ) );
				if( mConfiguration.NameplateDistancesConfig.UseDistanceBasedColor_Party )
				{
					ImGui.Indent();
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Near", "Use default nameplate color when near." ) + $"###Distance Text Use nameplate color (near) (Nameplates - Party).", ref mConfiguration.NameplateDistancesConfig.NearRangeTextUseNameplateColor_Party );
					if( !mConfiguration.NameplateDistancesConfig.NearRangeTextUseNameplateColor_Party )
					{
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Near", "Distance text color (near)" ) + $"###DistanceTextColorPicker Near (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.NearRangeTextColor_Party, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Near", "Distance text glow color (near)" ) + $"###DistanceTextEdgeColorPicker Near (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.NearRangeTextEdgeColor_Party, ImGuiColorEditFlags.NoInputs );
					}
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Mid", "Use default nameplate color when mid." ) + $"###Distance Text Use nameplate color (mid) (Nameplates - Party).", ref mConfiguration.NameplateDistancesConfig.MidRangeTextUseNameplateColor_Party );
					if( !mConfiguration.NameplateDistancesConfig.MidRangeTextUseNameplateColor_Party )
					{
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Mid", "Distance text color (mid)" ) + $"###DistanceTextColorPicker Mid (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.MidRangeTextColor_Party, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Mid", "Distance text glow color (mid)" ) + $"###DistanceTextEdgeColorPicker Mid (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.MidRangeTextEdgeColor_Party, ImGuiColorEditFlags.NoInputs );
					}
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Far", "Use default nameplate color when far." ) + $"###Distance Text Use nameplate color (far) (Nameplates - Party).", ref mConfiguration.NameplateDistancesConfig.FarRangeTextUseNameplateColor_Party );
					if( !mConfiguration.NameplateDistancesConfig.FarRangeTextUseNameplateColor_Party )
					{
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Far", "Distancet text color (far)" ) + $"###DistanceTextColorPicker Far (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.FarRangeTextColor_Party, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Far", "Distance text glow color (far)" ) + $"###DistanceTextEdgeColorPicker Far (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.FarRangeTextEdgeColor_Party, ImGuiColorEditFlags.NoInputs );
					}
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Near Range", "Distance \"near\" range ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( $"###DistanceNearRangeSlider (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.NearThresholdDistance_Party_Yalms, 0.5f, -30f, 30f );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Far Range", "Distance \"far\" range ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( $"###DistanceFarRangeSlider (Nameplates - Party)", ref mConfiguration.NameplateDistancesConfig.FarThresholdDistance_Party_Yalms, 0.5f, -30f, 30f );
					ImGui.Unindent();
					ImGui.Spacing();
				}

				ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Distance-based Colors (BNpc)", "Use distance-based text colors for battle NPCs." ) + $"###Distance Text Use distance-based colors (Nameplates - BNpc).", ref mConfiguration.NameplateDistancesConfig.UseDistanceBasedColor_BNpc );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Nameplate Distance Text Use Distance-based Colors", "Allows you to set different colors for different distance thresholds.  Uses the \"Far\" color if beyond that distance, the \"Mid\" color if between far and near thresholds, and the \"Near\" color if within that distance." ) );
				if( mConfiguration.NameplateDistancesConfig.UseDistanceBasedColor_BNpc )
				{
					ImGui.Indent();
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Near", "Use default nameplate color when near." ) + $"###Distance Text Use nameplate color (near) (Nameplates - BNpc).", ref mConfiguration.NameplateDistancesConfig.NearRangeTextUseNameplateColor_BNpc );
					if( !mConfiguration.NameplateDistancesConfig.NearRangeTextUseNameplateColor_BNpc )
					{
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Near", "Distance text color (near)" ) + $"###DistanceTextColorPicker Near (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.NearRangeTextColor_BNpc, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Near", "Distance text glow color (near)" ) + $"###DistanceTextEdgeColorPicker Near (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.NearRangeTextEdgeColor_BNpc, ImGuiColorEditFlags.NoInputs );
					}
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Mid", "Use default nameplate color when mid." ) + $"###Distance Text Use nameplate color (mid) (Nameplates - BNpc).", ref mConfiguration.NameplateDistancesConfig.MidRangeTextUseNameplateColor_BNpc );
					if( !mConfiguration.NameplateDistancesConfig.MidRangeTextUseNameplateColor_BNpc )
					{
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Mid", "Distance text color (mid)" ) + $"###DistanceTextColorPicker Mid (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.MidRangeTextColor_BNpc, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Mid", "Distance text glow color (mid)" ) + $"###DistanceTextEdgeColorPicker Mid (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.MidRangeTextEdgeColor_BNpc, ImGuiColorEditFlags.NoInputs );
					}
					ImGui.Checkbox( Loc.Localize( "Config Option: Nameplate Distance Text Use Nameplate Color Far", "Use default nameplate color when far." ) + $"###Distance Text Use nameplate color (far) (Nameplates - BNpc).", ref mConfiguration.NameplateDistancesConfig.FarRangeTextUseNameplateColor_BNpc );
					if( !mConfiguration.NameplateDistancesConfig.FarRangeTextUseNameplateColor_BNpc )
					{
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Far", "Distancet text color (far)" ) + $"###DistanceTextColorPicker Far (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.FarRangeTextColor_BNpc, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Far", "Distance text glow color (far)" ) + $"###DistanceTextEdgeColorPicker Far (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.FarRangeTextEdgeColor_BNpc, ImGuiColorEditFlags.NoInputs );
					}
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Near Range", "Distance \"near\" range ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( $"###DistanceNearRangeSlider (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.NearThresholdDistance_BNpc_Yalms, 0.5f, -30f, 30f );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Far Range", "Distance \"far\" range ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( $"###DistanceFarRangeSlider (Nameplates - BNpc)", ref mConfiguration.NameplateDistancesConfig.FarThresholdDistance_BNpc_Yalms, 0.5f, -30f, 30f );
					ImGui.Unindent();
				}
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Nameplate Fading", "Fading" ) + "###NameplateFadingHeader" ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Enable Fading (BNpc)", "Enable Distance-based fading for battle NPCs." ), ref mConfiguration.NameplateDistancesConfig.EnableFading_BNpc );
				if( mConfiguration.NameplateDistancesConfig.EnableFading_BNpc )
				{
					ImGui.Indent();
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Inside Fade Distance", "Distance inside of the target to start fading ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Inside Fade Distance", "If you use the distance from center instead of distance from target ring, the inner fade settings will only ever have an effect if you configured a positive distance offset above." ) );
					ImGui.DragFloat( "###DistanceTextInnerFadeThresholdSlider_BNpc", ref mConfiguration.NameplateDistancesConfig.FadeoutThresholdInner_BNpc_Yalms, 0.5f, 1f, 50f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Inside Fade Interval", "Fade over ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( "###DistanceTextInnerFadeIntervalSlider_BNpc", ref mConfiguration.NameplateDistancesConfig.FadeoutIntervalInner_BNpc_Yalms, 0.5f, 0f, 50f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Outside Fade Distance", "Distance away from the target to start fading ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( "###DistanceTextOuterFadeThresholdSlider_BNpc", ref mConfiguration.NameplateDistancesConfig.FadeoutThresholdOuter_BNpc_Yalms, 0.5f, 1f, 100f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Outside Fade Interval", "Fade over ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( "###DistanceTextOuterFadeIntervalSlider_BNpc", ref mConfiguration.NameplateDistancesConfig.FadeoutIntervalOuter_BNpc_Yalms, 0.5f, 0f, 100f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Invert Fading", "Inverted fading." ) + "###InvertedFadingCheckbox_BNpc", ref mConfiguration.NameplateDistancesConfig.InvertFading_BNpc );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Invert Fading", "Instead of showing the distance text within the range defined above, show it only outside of the defined range instead.  The fadeout interval is added to the configured threshold distance when this option is used." ) );
					ImGui.Unindent();
				}

				ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Enable Fading (Party)", "Enable Distance-based fading for party members." ), ref mConfiguration.NameplateDistancesConfig.EnableFading_Party );
				if( mConfiguration.NameplateDistancesConfig.EnableFading_Party )
				{
					ImGui.Indent();
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Inside Fade Distance", "Distance inside of the target to start fading ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Inside Fade Distance", "If you use the distance from center instead of distance from target ring, the inner fade settings will only ever have an effect if you configured a positive distance offset above." ) );
					ImGui.DragFloat( "###DistanceTextInnerFadeThresholdSlider_Party", ref mConfiguration.NameplateDistancesConfig.FadeoutThresholdInner_Party_Yalms, 0.5f, 1f, 50f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Inside Fade Interval", "Fade over ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( "###DistanceTextInnerFadeIntervalSlider_Party", ref mConfiguration.NameplateDistancesConfig.FadeoutIntervalInner_Party_Yalms, 0.5f, 0f, 50f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Outside Fade Distance", "Distance away from the target to start fading ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( "###DistanceTextOuterFadeThresholdSlider_Party", ref mConfiguration.NameplateDistancesConfig.FadeoutThresholdOuter_Party_Yalms, 0.5f, 1f, 100f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Outside Fade Interval", "Fade over ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( "###DistanceTextOuterFadeIntervalSlider_Party", ref mConfiguration.NameplateDistancesConfig.FadeoutIntervalOuter_Party_Yalms, 0.5f, 0f, 100f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Invert Fading", "Inverted fading." ) + "###InvertedFadingCheckbox_Party", ref mConfiguration.NameplateDistancesConfig.InvertFading_Party );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Invert Fading", "Instead of showing the distance text within the range defined above, show it only outside of the defined range instead.  The fadeout interval is added to the configured threshold distance when this option is used." ) );
					ImGui.Unindent();
				}

				ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Enable Fading (Other)", "Enable Distance-based fading for other entities." ), ref mConfiguration.NameplateDistancesConfig.EnableFading_Other );
				if( mConfiguration.NameplateDistancesConfig.EnableFading_Other )
				{
					ImGui.Indent();
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Inside Fade Distance", "Distance inside of the target to start fading ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Inside Fade Distance", "If you use the distance from center instead of distance from target ring, the inner fade settings will only ever have an effect if you configured a positive distance offset above." ) );
					ImGui.DragFloat( "###DistanceTextInnerFadeThresholdSlider_Other", ref mConfiguration.NameplateDistancesConfig.FadeoutThresholdInner_Other_Yalms, 0.5f, 1f, 50f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Inside Fade Interval", "Fade over ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( "###DistanceTextInnerFadeIntervalSlider_Other", ref mConfiguration.NameplateDistancesConfig.FadeoutIntervalInner_Other_Yalms, 0.5f, 0f, 50f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Outside Fade Distance", "Distance away from the target to start fading ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( "###DistanceTextOuterFadeThresholdSlider_Other", ref mConfiguration.NameplateDistancesConfig.FadeoutThresholdOuter_Other_Yalms, 0.5f, 1f, 100f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( String.Format( Loc.Localize( "Config Option: Distance Text Outside Fade Interval", "Fade over ({0}):" ), LocalizationHelpers.DistanceUnitShort ) );
					ImGui.DragFloat( "###DistanceTextOuterFadeIntervalSlider_Other", ref mConfiguration.NameplateDistancesConfig.FadeoutIntervalOuter_Other_Yalms, 0.5f, 0f, 100f, "%g", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Invert Fading", "Inverted fading." ) + "###InvertedFadingCheckbox_Other", ref mConfiguration.NameplateDistancesConfig.InvertFading_Other );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Invert Fading", "Instead of showing the distance text within the range defined above, show it only outside of the defined range instead.  The fadeout interval is added to the configured threshold distance when this option is used." ) );
					ImGui.Unindent();
				}

				ImGui.TreePop();
			}
		}
	}

	private readonly Plugin mPlugin;
	private readonly PluginUI mUI;
	private readonly Configuration mConfiguration;
}
