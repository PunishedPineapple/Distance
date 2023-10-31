using System;
using System.Numerics;

using CheapLoc;

using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Distance;

internal sealed class PluginUI_CustomWidgets : IDisposable
{
	internal PluginUI_CustomWidgets( Plugin plugin, PluginUI ui, Configuration configuration )
	{
		mPlugin = plugin;
		mUI = ui;
		mConfiguration = configuration;
	}

	public void Dispose()
	{
	}

	internal void DrawSettingsTab()
	{
		float buttonHeight = ImGui.GetFrameHeightWithSpacing();
		Vector2 contentRegionSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
		float maxCategoryWidth = ImGui.CalcTextSize( Loc.Localize( "Config Text: Custom Widget Placeholder", "No Custom Widgets (Click + To Add)" ) ).X;
		foreach( var config in mConfiguration.DistanceWidgetConfigs )
		{
			maxCategoryWidth = Math.Max( maxCategoryWidth, ImGui.CalcTextSize( config.DisplayedWidgetName ).X );
		}
		maxCategoryWidth += ImGui.GetStyle().WindowPadding.X;
		float leftPaneWidth = maxCategoryWidth + ImGui.GetStyle().WindowPadding.X * 2 - ImGui.GetStyle().ItemSpacing.X / 2;
		float rightPaneWidth = contentRegionSize.X - leftPaneWidth - ImGui.GetStyle().ItemSpacing.X;
		if( ImGui.BeginChild( "###DistanceSettingsCustomWidgetsChildWindow_Select_Outer", new( leftPaneWidth, ImGui.GetContentRegionAvail().Y - buttonHeight ), false ) )
		{
			if( ImGui.BeginChild( "###DistanceSettingsCustomWidgetsChildWindow_Select", new( leftPaneWidth, ImGui.GetContentRegionAvail().Y - buttonHeight ), true ) )
			{
				if( mConfiguration.DistanceWidgetConfigs.Count > 0 )
				{
					for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; ++i )
					{
						if( ImGui.Selectable( mConfiguration.DistanceWidgetConfigs[i].DisplayedWidgetName + $"###DistanceWidget{i}Selectable", i == mSelectedCustomWidget ) )
						{
							mSelectedCustomWidget = i;
							mWidgetIndexWantToDelete = -1;
						}
					}
				}
				else
				{
					ImGui.Text( Loc.Localize( "Config Text: Custom Widget Placeholder", "No Custom Widgets (Click + To Add)" ) );
					mSelectedCustomWidget = -1;
				}
			}
			ImGui.EndChild();
			ImGui.PushFont( UiBuilder.IconFont );
			try
			{
				if( ImGui.SmallButton( "\uF067" + "###AddWidgetButton" ) )
				{
					mConfiguration.DistanceWidgetConfigs.Add( new() );
					mSelectedCustomWidget = mConfiguration.DistanceWidgetConfigs.Count - 1;
				}
				ImGui.SameLine();
				if( ImGui.SmallButton( "\uF068" + "###RemoveWidgetButton" ) && mSelectedCustomWidget >= 0 )
				{
					mWidgetIndexWantToDelete = mSelectedCustomWidget;
				}
			}
			finally
			{
				ImGui.PopFont();
			}

			if( mWidgetIndexWantToDelete >= 0 && mWidgetIndexWantToDelete == mSelectedCustomWidget )
			{
				ImGui.SameLine();
				ImGui.Text( "/" );
				ImGui.SameLine();
				ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );
				if( ImGui.SmallButton( Loc.Localize( "Button: Yes", "Yes" ) + "###DeleteWidgetYesButton" ) )
				{
					DeleteWidget( mWidgetIndexWantToDelete );
					mSelectedCustomWidget = -1;
					mWidgetIndexWantToDelete = -1;
				}
				ImGui.PopStyleColor();
				ImGui.SameLine();
				if( ImGui.SmallButton( Loc.Localize( "Button: No", "No" ) + "###DeleteWidgetNoButton" ) )
				{
					mWidgetIndexWantToDelete = -1;
				}
			}
		}
		ImGui.EndChild();
		ImGui.SameLine();
		if( ImGui.BeginChild( "###DistanceSettingsCustomWidgetsChildWindow_Settings", new( rightPaneWidth, ImGui.GetContentRegionAvail().Y - buttonHeight ), true ) )
		{
			ImGui.PushID( "CustomWidgetOptions" );
			try
			{
				DrawConfigOptions( mSelectedCustomWidget );
			}
			finally
			{
				ImGui.PopID();
			}
		}
		ImGui.EndChild();
	}

	private void DrawConfigOptions( int i )
	{
		if( i < 0 || i >= mConfiguration.DistanceWidgetConfigs.Count )
		{
			ImGui.Text( Loc.Localize( "Config Text: No Widget Selected", "No Widget Selected" ) );
			return;
		}

		ImGui.PushID( i );
		try
		{
			var config = mConfiguration.DistanceWidgetConfigs[i];
			ImGui.Text( Loc.Localize( "Config Option: Widget Name", "Widget Name:" ) );
			ImGui.SameLine();
			ImGui.InputTextWithHint( "###WidgetNameInputBox", config.ApplicableTargetType.GetTranslatedName(), ref config.WidgetName, 50 );
			ImGuiUtils.HelpMarker( Loc.Localize( "Help: Widget Name", "This is used to give a customized name to this widget for use with certain text commands.  If you leave it blank, the type of target for this widget will be used in the header above, but it will not have a name for use in text commands." ) );
			ImGui.Checkbox( Loc.Localize( "Config Option: Widget Enabled", "Enabled" ) + "###WidgetEnabledCheckbox", ref config.Enabled );
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
				ImGui.Checkbox( Loc.Localize( "Config Option: Distance is to Ring", "Show distance to target ring, not target center." ) + "###DistanceIsToRing", ref config.DistanceIsToRing );
				ImGui.Text( Loc.Localize( "Config Option: Distance Measurement Offset", "Amount to offset the distance readout (y):" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Readout Offset", "This value is subtracted from the real distance to determine the displayed distance.  This can be used to get the widget to show the distance from being able to hit the boss with a skill, for example." ) );
				ImGui.DragFloat( "###DistanceOffsetSlider", ref config.DistanceOffset_Yalms, 0.1f, -30f, 30f );
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Filters", "Object Type Filters" ) + "###DistanceWidgetFiltersHeader" ) )
			{
				config.Filters.DrawObjectKindOptions();
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Classjobs", "Condition Filters" ) + "###DistanceWidgetConditionsHeader" ) )
			{
				config.Filters.DrawConditionOptions();
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Classjobs", "Job Filters" ) + "###DistanceWidgetClassjobsHeader" ) )
			{
				config.Filters.DrawClassJobOptions();
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
				ImGui.DragFloat2( "###DistanceTextPositionSlider", ref config.TextPosition, 1f, sliderLimits.X, sliderLimits.Y, "%g", ImGuiSliderFlags.AlwaysClamp );
				ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Use Heavy Font", "Use heavy font for distance text." ) + "###DistanceFontHeavy", ref config.FontHeavy );
				ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Size", "Distance text font size:" ) );
				ImGui.SliderInt( "###DistanceTextFontSizeSlider", ref config.FontSize, 6, 36 );
				ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Alignment", "Text alignment:" ) );
				ImGui.SliderInt( "###DistanceTextFontAlignmentSlider", ref config.mFontAlignment, 6, 8, "", ImGuiSliderFlags.NoInput );
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + "###ShowDistanceUnits", ref config.ShowUnits );
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Mode Indicator", "Show the distance mode indicator." ) + "###ShowDistanceTypeMarker", ref config.ShowDistanceModeMarker );
				ImGui.Checkbox( Loc.Localize( "Config Option: Allow Negative Distances", "Allow negative distances." ) + "###AllowNegativeDistances", ref config.AllowNegativeDistances );
				ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distances:" ) );
				ImGui.SliderInt( "###DistancePrecisionSlider", ref config.DecimalPrecision, 0, 3 );
				ImGui.TreePop();
			}

			if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Colors", "Colors" ) + "###DistanceWidgetColorsHeader" ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Track Target Bar Color", "Attempt to use target bar text color." ) + "###DistanceTextUseTargetBarColor", ref config.TrackTargetBarTextColor );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Track Target Bar Color", "If the color of the target bar text (or focus target) can be determined, it will take precedence; otherwise the colors set below will be used." ) );
				ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Use Distance-based Colors", "Use distance-based text colors." ) + "###DistanceTextUseDistanceBasedColors", ref config.UseDistanceBasedColor );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Text Use Distance-based Colors", "Allows you to set different colors for different distance thresholds.  Uses the \"Far\" color if beyond that distance, otherwise the \"Near\" color if beyond that distance, otherwise uses the base color specified above.  This setting is ignored if the checkbox to track the target bar color is ticked." ) );
				ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color", "Distance text color" ) + "###DistanceTextColorPicker", ref config.TextColor, ImGuiColorEditFlags.NoInputs );
				ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color", "Distance text glow color" ) + "###DistanceTextEdgeColorPicker", ref config.TextEdgeColor, ImGuiColorEditFlags.NoInputs );
				if( config.UseDistanceBasedColor )
				{
					ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Far", "Distance text color (near)" ) + "###DistanceTextColorPickerNear", ref config.NearThresholdTextColor, ImGuiColorEditFlags.NoInputs );
					ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Far", "Distance text glow color (near)" ) + "###DistanceTextEdgeColorPickerNear", ref config.NearThresholdTextEdgeColor, ImGuiColorEditFlags.NoInputs );
					ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color Far", "Distancet text color (far)" ) + "###DistanceTextColorPickerFar", ref config.FarThresholdTextColor, ImGuiColorEditFlags.NoInputs );
					ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color Far", "Distance text glow color (far)" ) + "###DistanceTextEdgeColorPickerFar", ref config.FarThresholdTextEdgeColor, ImGuiColorEditFlags.NoInputs );
					ImGui.Text( Loc.Localize( "Config Option: Distance Text Near Range", "Distance \"near\" range (y):" ) );
					ImGui.DragFloat( "###DistanceNearRangeSlider", ref config.NearThresholdDistance_Yalms, 0.5f, -30f, 30f );
					ImGui.Text( Loc.Localize( "Config Option: Distance Text Far Range", "Distance \"far\" range (y):" ) );
					ImGui.DragFloat( "###DistanceFarRangeSlider", ref config.FarThresholdDistance_Yalms, 0.5f, -30f, 30f );
				}
				ImGui.TreePop();
			}
		}
		finally
		{
			ImGui.PopID();
		}
	}

	internal void DeleteWidget( int index )
	{
		if( index >= 0 && index < mConfiguration.DistanceWidgetConfigs.Count )
		{
			// Hide the last node since we've reduced count by one, making the highest node effectively
			// dormant.  The visibilities for all lower nodes will be updated on the next frame.
			PluginUI.HideTextNode( PluginUI.mDistanceNodeIDBase + (uint)mConfiguration.DistanceWidgetConfigs.Count - 1 );
			mConfiguration.DistanceWidgetConfigs.RemoveAt( index );
		}
	}

	private int mSelectedCustomWidget = -1;
	private int mWidgetIndexWantToDelete = -1;

	private readonly Plugin mPlugin;
	private readonly PluginUI mUI;
	private readonly Configuration mConfiguration;
}
