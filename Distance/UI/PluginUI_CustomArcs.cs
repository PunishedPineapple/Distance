﻿using System;
using System.Linq;
using System.Numerics;

using CheapLoc;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using ImGuiNET;

namespace Distance;

internal sealed class PluginUI_CustomArcs : IDisposable
{
	internal PluginUI_CustomArcs( Plugin plugin, PluginUI ui, Configuration configuration )
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
		if( ImGui.Button( Loc.Localize( "Button: Add Distance Arc", "Add Arc" ) + $"###AddArcButton." ) )
		{
			mConfiguration.DistanceArcConfigs.Add( new() );
		}

		int arcIndexToDelete = -1;
		for( int i = 0; i < mConfiguration.DistanceArcConfigs.Count; ++i )
		{
			ImGui.PushID( i );
			try
			{
				var config = mConfiguration.DistanceArcConfigs[i];
				string defaultName = config.ApplicableTargetCategory == TargetCategory.Targets ? config.ApplicableTargetType.GetTranslatedName() : config.ApplicableTargetCategory.GetTranslatedName();
				string name = config.ArcName.Length > 0 ? config.ArcName : defaultName;
				if( ImGui.CollapsingHeader( String.Format( Loc.Localize( "Config Section Header: Distance Arc", "Distance Arc ({0})" ), name ) + "###DistanceArcHeader" ) )
				{
					ImGui.Text( Loc.Localize( "Config Option: Arc Name", "Arc Name:" ) );
					ImGui.SameLine();
					string nameHint = config.ApplicableTargetCategory == TargetCategory.Targets ? config.ApplicableTargetType.GetTranslatedName() : config.ApplicableTargetCategory.GetTranslatedName();
					ImGui.InputTextWithHint( "###ArcNameInputBox", nameHint, ref config.ArcName, 50 );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Arc Name", "This is used to give a customized name to this Arc for use with certain text commands.  If you leave it blank, the type of target for this Arc will be used in the header above, but it will not have a name for use in text commands." ) );
					ImGui.Checkbox( Loc.Localize( "Config Option: Arc Enabled", "Enabled" ) + "###Arc Enabled Checkbox", ref config.Enabled );
					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Arc Rules", "Distance Rules" ) + "###DistanceArcRulesHeader" ) )
					{
						ImGui.Text( Loc.Localize( "Config Option: Object Type", "Object Type:" ) );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Applicable Object Type", "The basic category of object(s) for which this Arc will show distance." ) );
						if( ImGui.BeginCombo( "###ArcObjectCategoryDropdown", config.ApplicableTargetCategory.GetTranslatedName() ) )
						{
							foreach( var item in Enum.GetValues<TargetCategory>() )
							{
								if( ImGui.Selectable( item.GetTranslatedName(), config.ApplicableTargetCategory == item ) )
								{
									config.ApplicableTargetCategory = item;
								}
							}
							ImGui.EndCombo();
						}

						if( config.ApplicableTargetCategory == TargetCategory.Targets )
						{
							ImGui.Text( Loc.Localize( "Config Option: Target Type", "Target Type:" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Applicable Target Type", "The type of target for which this Arc will show distance.  \"Soft Target\" generally only matters for controller players and some two-handed keyboard players.  \"Field Mouseover\" is for when you mouseover an object in the world.  \"UI Mouseover\" is for when you mouseover the party list." ) );
							if( ImGui.BeginCombo( "###ArcTargetTypeDropdown", config.ApplicableTargetType.GetTranslatedName() ) )
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
						}
						else if( config.ApplicableTargetCategory == TargetCategory.Self )
						{
							ImGui.Text( Loc.Localize( "Config Option: Arc Self Azimuth", "Arc Centerpoint Azimuth:" ) );
							ImGuiUtils.HelpMarker( Loc.Localize( "Help: Arc Self Azimuth", "Direction relative to your character to display the arc.  0 is in front, 90 is to the right, 180 is behind, 270 is to the left." ) );
							ImGui.DragInt( "###ArcSelfAzimuthSlider", ref config.SelfTargetedArcAzimuth_Deg, 1f, 0, 359, "%d", ImGuiSliderFlags.AlwaysClamp );
							ImGui.Checkbox( Loc.Localize( "Config Option: Camera-relative Self Arc", "Camera Relative" ) + "###ArcSelfCameraRelativeCheckbox", ref config.SelfTargetedArcCameraRelative );
						}
						else if( config.ApplicableTargetCategory == TargetCategory.AllEnemies )
						{
							ImGui.Checkbox( Loc.Localize( "Config Option: All Enemies Aggressive", "Show Aggressive" ) + "###ArcAllEnemiesAggressiveCheckbox", ref config.AllEnemiesShowAggressive );
							ImGui.Checkbox( Loc.Localize( "Config Option: All Enemies Unaggressive", "Show Unaggressive" ) + "###ArcAllEnemiesUnaggressiveCheckbox", ref config.AllEnemiesShowUnaggressive );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Dead Objects", "Show Dead" ) + "###ShowDeadObjectsCheckbox", ref config.ShowDeadObjects );
						}
						else if( config.ApplicableTargetCategory == TargetCategory.AllPlayers )
						{
							ImGui.Checkbox( Loc.Localize( "Config Option: All Players Party", "Show Party Members" ) + "###ArcAllPlayersPartyCheckbox", ref config.AllPlayersShowParty );
							ImGui.Checkbox( Loc.Localize( "Config Option: All Players Alliance", "Show Alliance Members" ) + "###ArcAllPlayersAllianceCheckbox", ref config.AllPlayersShowAlliance );
							ImGui.Checkbox( Loc.Localize( "Config Option: All Players Other", "Show Other Players" ) + "###ArcAllPlayersOtherCheckbox", ref config.AllPlayersShowOthers );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Dead Objects", "Show Dead" ) + "###ShowDeadObjectsCheckbox", ref config.ShowDeadObjects );
						}

						ImGui.Checkbox( Loc.Localize( "Config Option: Distance is to Ring", "Use distance to target ring, not target center." ) + "###ArcDistanceIsToRingCheckbox", ref config.DistanceIsToRing );
						ImGui.Text( Loc.Localize( "Config Option: Arc Radius", "The radius of the arc (y):" ) );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Arc Radius", "This distance is relative to either the object's center, or to its hitring, as configured above." ) );
						ImGui.DragFloat( "###ArcDistanceSlider", ref config.ArcRadius_Yalms, 0.1f, -30f, 30f );
						ImGui.TreePop();
					}

					if( config.ApplicableTargetCategory is not TargetCategory.Self and not TargetCategory.AllEnemies and not TargetCategory.AllPlayers &&
						ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Arc Filters", "Object Type Filters" ) + $"###DistanceArcFiltersHeader" ) )
					{
						config.Filters.DrawObjectKindOptions();
						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Classjobs", "Condition Filters" ) + "###DistanceArcConditionsHeader" ) )
					{
						config.Filters.DrawConditionOptions();
						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Arc Classjobs", "Job Filters" ) + "###DistanceArcClassjobsHeader" ) )
					{
						config.Filters.DrawClassJobOptions();
						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Arc Appearance", "Appearance" ) + "###DistanceArcAppearanceHeader" ) )
					{
						ImGui.Checkbox( Loc.Localize( "Config Option: Show Arc Pip", "Show Pip." ) + "###ArcShowPip", ref config.ShowPip );
						ImGui.Text( Loc.Localize( "Config Option: Arc Length", "Length of the arc:" ) );
						if( ImGui.RadioButton( Loc.Localize( "Unit String: Degrees Short Lower", "deg" ), !config.ArcLengthIsYalms ) ) config.ArcLengthIsYalms = false;
						ImGui.SameLine();
						if( ImGui.RadioButton( Loc.Localize( "Unit String: Yalms Lower", "yalms" ), config.ArcLengthIsYalms ) ) config.ArcLengthIsYalms = true;
						ImGui.SliderFloat( "###ArcLengthSlider", ref config.ArcLength, 0, 30 );

						ImGui.Text( Loc.Localize( "Config Option: Distance Arc Inside Fade Distance", "Distance inside the arc to start fading (y):" ) );
						ImGui.DragFloat( "###ArcDistanceInnerFadeThresholdSlider", ref config.FadeoutThresholdInner_Yalms, 0.5f, 1f, 30f, "%g", ImGuiSliderFlags.AlwaysClamp );
						ImGui.Text( Loc.Localize( "Config Option: Distance Arc Inside Fade Interval", "Fade over (y):" ) );
						ImGui.DragFloat( "###ArcDistanceInnerFadeIntervalSlider", ref config.FadeoutIntervalInner_Yalms, 0.5f, 0f, 30f, "%g", ImGuiSliderFlags.AlwaysClamp );
						ImGui.Text( Loc.Localize( "Config Option: Distance Arc Outside Fade Distance", "Distance outside the arc to start fading (y):" ) );
						ImGui.DragFloat( "###ArcDistanceOuterFadeThresholdSlider", ref config.FadeoutThresholdOuter_Yalms, 0.5f, 1f, 30f, "%g", ImGuiSliderFlags.AlwaysClamp );
						ImGui.Text( Loc.Localize( "Config Option: Distance Arc Outside Fade Interval", "Fade over (y):" ) );
						ImGui.DragFloat( "###ArcDistanceOuterFadeIntervalSlider", ref config.FadeoutIntervalOuter_Yalms, 0.5f, 0f, 30f, "%g", ImGuiSliderFlags.AlwaysClamp );

						ImGui.TreePop();
					}

					if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Arc Colors", "Colors" ) + "###Distance Arc Colors Header" ) )
					{
						if( config.ApplicableTargetCategory != TargetCategory.Self )
						{
							ImGui.Checkbox( Loc.Localize( "Config Option: Distance Arc Use Distance-based Colors", "Use distance-based arc colors." ) + "###DistanceArcUseDistanceBasedColors", ref config.UseDistanceBasedColor );
						}
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Distance Arc Use Distance-based Colors", "Allows you to set different colors for different distance thresholds.  Uses the \"Far\" colors if past that distance, otherwise the \"Near\" colors if past that distance, otherwise uses the base color specified above." ) );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Color", "Distance text color" ) + "###ArcColorPicker", ref config.Color, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Glow Color", "Distance text glow color" ) + "###ArcEdgeColorPicker", ref config.EdgeColor, ImGuiColorEditFlags.NoInputs );
						if( config.UseDistanceBasedColor && config.ApplicableTargetCategory != TargetCategory.Self )
						{
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Color Inside Far", "Arc color (inside near)" ) + "###ArcColorPickerInnerNear", ref config.InnerNearThresholdColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Glow Color Inside Far", "Arc glow color (inside near)" ) + "###ArcEdgeColorPickerInnerNear", ref config.InnerNearThresholdEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Color Inside Far", "Arc color (inside far)" ) + "###ArcColorPickerInnerFar", ref config.InnerFarThresholdColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Glow Color Inside Far", "Arc glow color (inside far)" ) + "###ArcEdgeColorPickerInnerFar", ref config.InnerFarThresholdEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.Text( Loc.Localize( "Config Option: Distance Arc Inside Near Range", "Distance \"inside near\" range (y):" ) );
							ImGui.DragFloat( "###ArcDistanceNearInsideRangeSlider", ref config.InnerNearThresholdDistance_Yalms, 0.5f, 0f, 30f );
							ImGui.Text( Loc.Localize( "Config Option: Distance Arc Inside Far Range", "Distance \"inside far\" range (y):" ) );
							ImGui.DragFloat( "###ArcDistanceFarInsideRangeSlider", ref config.InnerFarThresholdDistance_Yalms, 0.5f, 0f, 30f );

							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Color Outside Far", "Arc color (outside near)" ) + "###ArcColorPickerOuterNear", ref config.OuterNearThresholdColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Glow Color Outside Far", "Arc glow color (outside near)" ) + "###ArcEdgeColorPickerOuterNear", ref config.OuterNearThresholdEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Color Outside Far", "Arc color (outside far)" ) + "###ArcColorPickerOuterFar", ref config.OuterFarThresholdColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Arc Glow Color Outside Far", "Arc glow color (outside far)" ) + "###ArcEdgeColorPickerOuterFar", ref config.OuterFarThresholdEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.Text( Loc.Localize( "Config Option: Distance Arc Outside Near Range", "Distance \"outside near\" range (y):" ) );
							ImGui.DragFloat( "###ArcDistanceNearOutsideRangeSlider", ref config.OuterNearThresholdDistance_Yalms, 0.5f, 0f, 30f );
							ImGui.Text( Loc.Localize( "Config Option: Distance Arc Outside Far Range", "Distance \"outside far\" range (y):" ) );
							ImGui.DragFloat( "###ArcDistanceFarOutsideRangeSlider", ref config.OuterFarThresholdDistance_Yalms, 0.5f, 0f, 30f );
						}
						ImGui.TreePop();
					}

					if( ImGui.Button( Loc.Localize( "Button: Delete Arc", "Delete Arc" ) + "###DeleteArcButton" ) )
					{
						mArcIndexWantToDelete = i;
					}
					if( mArcIndexWantToDelete == i )
					{
						ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );
						ImGui.Text( Loc.Localize( "Settings Window Text: Confirm Delete Label", "Confirm delete: " ) );
						ImGui.SameLine();
						if( ImGui.Button( Loc.Localize( "Button: Yes", "Yes" ) + "###DeleteArcYesButton" ) )
						{
							arcIndexToDelete = mArcIndexWantToDelete;
						}
						ImGui.PopStyleColor();
						ImGui.SameLine();
						if( ImGui.Button( Loc.Localize( "Button: No", "No" ) + "###DeleteArcNoButton" ) )
						{
							mArcIndexWantToDelete = -1;
						}
					}
				}
			}
			finally
			{
				ImGui.PopID();
			}
		}
		if( arcIndexToDelete > -1 && arcIndexToDelete < mConfiguration.DistanceArcConfigs.Count )
		{
			mConfiguration.DistanceArcConfigs.RemoveAt( arcIndexToDelete );
			mArcIndexWantToDelete = -1;
		}
	}

	internal void DrawOnOverlay()
	{
		if( Service.ClientState.IsPvP ) return;
		if( Service.ClientState.LocalPlayer == null ) return;

		foreach( var config in mConfiguration.DistanceArcConfigs )
		{
			if( !config.Enabled ) continue;
			if( !config.Filters.ShowDistanceForConditions( Service.Condition[ConditionFlag.InCombat], Service.Condition[ConditionFlag.BoundByDuty] ) ) continue;
			if( !config.Filters.ShowDistanceForClassJob( Service.ClientState.LocalPlayer?.ClassJob.Id ?? 0 ) ) continue;
			//	Note that we cannot evaluate the object type filters here, because they may behave differently by target category.

			if( config.ApplicableTargetCategory == TargetCategory.Targets ) DrawCustomArc_Targets( config );
			else if( config.ApplicableTargetCategory == TargetCategory.Self ) DrawCustomArc_Self( config );
			else if( config.ApplicableTargetCategory == TargetCategory.AllEnemies ) DrawCustomArc_AllEnemies( config );
			else if( config.ApplicableTargetCategory == TargetCategory.AllPlayers ) DrawCustomArc_AllPlayers( config );
		}
	}
	private void DrawCustomArc_Targets( DistanceArcConfig config )
	{
		var distanceInfo = mPlugin.GetDistanceInfo( config.ApplicableTargetType );
		if( !distanceInfo.IsValid ) return;
		if( !config.Filters.ShowDistanceForObjectKind( distanceInfo.TargetKind ) ) return;

		float trueArcRadius_Yalms = config.ArcRadius_Yalms + ( config.DistanceIsToRing ? distanceInfo.TargetRadius_Yalms : 0 );
		float distanceFromArc_Yalms = distanceInfo.DistanceFromTarget_Yalms - trueArcRadius_Yalms;

		if( !config.WithinDisplayRangeOfArc( distanceFromArc_Yalms ) ) return;

		var colors = config.GetColors( distanceFromArc_Yalms );

		ArcUtils.DrawArc_ScreenSpace(
			distanceInfo.TargetPosition,
			distanceInfo.PlayerPosition,
			trueArcRadius_Yalms,
			config.ArcLength,
			config.ArcLengthIsYalms,
			config.ShowPip,
			colors.Item1,
			colors.Item2 );
	}

	private unsafe void DrawCustomArc_Self( DistanceArcConfig config )
	{
		double playerRelativeArcAngle_Rad = config.SelfTargetedArcAzimuth_Deg * Math.PI / 180.0;
		double absoluteArcAngle_Rad = -Service.ClientState.LocalPlayer.Rotation - Math.PI / 2.0 + playerRelativeArcAngle_Rad;

		bool cameraValid = CameraManager.Instance() != null && CameraManager.Instance()->CurrentCamera != null;
		if( config.SelfTargetedArcCameraRelative && cameraValid )
		{
			Vector3 cameraDirection = CameraManager.Instance()->CurrentCamera->LookAtVector - CameraManager.Instance()->CurrentCamera->Object.Position;
			double cameraAngle_Rad = -Math.Atan2( cameraDirection.Z, cameraDirection.X ) + Math.PI / 2.0;
			double cameraOffset_Rad = Service.ClientState.LocalPlayer.Rotation - cameraAngle_Rad;
			absoluteArcAngle_Rad += cameraOffset_Rad;
		}

		MathUtils.Wrap( ref absoluteArcAngle_Rad, -Math.PI, Math.PI );

		float trueArcRadius_Yalms = config.ArcRadius_Yalms + ( config.DistanceIsToRing ? 0.5f : 0f );
		Vector3 arcMidpoint = new()
		{
			X = (float)Math.Cos( absoluteArcAngle_Rad ) * trueArcRadius_Yalms,
			Y = 0f,
			Z = (float)Math.Sin( absoluteArcAngle_Rad ) * trueArcRadius_Yalms,
		};
		arcMidpoint += Service.ClientState.LocalPlayer.Position;

		ArcUtils.DrawArc_ScreenSpace(
			Service.ClientState.LocalPlayer.Position,
			arcMidpoint,
			trueArcRadius_Yalms,
			config.ArcLength,
			config.ArcLengthIsYalms,
			config.ShowPip,
			config.Color,
			config.EdgeColor );
	}

	private void DrawCustomArc_AllEnemies( DistanceArcConfig config )
	{
		var relevantBNpcs = Service.ObjectTable.Where( x =>
										x != null &&
										x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
										x.SubKind == (byte)BattleNpcSubKind.Enemy &&
										x.IsTargetable &&
										( !x.IsDead || config.ShowDeadObjects ) &&
										x.Position.DistanceTo_XZ( Service.ClientState.LocalPlayer.Position ) < 50f &&
										( x.IsAggressive() && config.AllEnemiesShowAggressive || !x.IsAggressive() && config.AllEnemiesShowUnaggressive )
										);

		foreach( var bnpc in relevantBNpcs )
		{
			float bnpcDistance_Yalms = bnpc.Position.DistanceTo_XZ( Service.ClientState.LocalPlayer.Position );
			float trueArcRadius_Yalms = config.ArcRadius_Yalms + ( config.DistanceIsToRing ? bnpc.HitboxRadius : 0 );
			float distanceFromArc_Yalms = bnpcDistance_Yalms - trueArcRadius_Yalms;

			if( !config.WithinDisplayRangeOfArc( distanceFromArc_Yalms ) ) continue;

			var colors = config.GetColors( distanceFromArc_Yalms );

			ArcUtils.DrawArc_ScreenSpace(
				bnpc.Position,
				Service.ClientState.LocalPlayer.Position,
				trueArcRadius_Yalms,
				config.ArcLength,
				config.ArcLengthIsYalms,
				config.ShowPip,
				colors.Item1,
				colors.Item2 );
		}
	}

	private void DrawCustomArc_AllPlayers( DistanceArcConfig config )
	{
		var relevantBNpcs = Service.ObjectTable.Where( x =>
										x != null &&
										x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player && (
											x.IsPartyMember() && config.AllPlayersShowParty ||
											x.IsAllianceMember() && config.AllPlayersShowAlliance ||
											!x.IsPartyMember() && !x.IsAllianceMember() && config.AllPlayersShowOthers ) &&
										x.IsTargetable &&
										( !x.IsDead || config.ShowDeadObjects ) &&
										x.Position.DistanceTo_XZ( Service.ClientState.LocalPlayer.Position ) < 50f
										);

		foreach( var bnpc in relevantBNpcs )
		{
			float bnpcDistance_Yalms = bnpc.Position.DistanceTo_XZ( Service.ClientState.LocalPlayer.Position );
			float trueArcRadius_Yalms = config.ArcRadius_Yalms + ( config.DistanceIsToRing ? bnpc.HitboxRadius : 0 );
			float distanceFromArc_Yalms = bnpcDistance_Yalms - trueArcRadius_Yalms;

			if( !config.WithinDisplayRangeOfArc( distanceFromArc_Yalms ) ) continue;

			var colors = config.GetColors( distanceFromArc_Yalms );

			ArcUtils.DrawArc_ScreenSpace(
				bnpc.Position,
				Service.ClientState.LocalPlayer.Position,
				trueArcRadius_Yalms,
				config.ArcLength,
				config.ArcLengthIsYalms,
				config.ShowPip,
				colors.Item1,
				colors.Item2 );
		}
	}

	private readonly Plugin mPlugin;
	private readonly PluginUI mUI;
	private readonly Configuration mConfiguration;
	private int mArcIndexWantToDelete = -1;
}
