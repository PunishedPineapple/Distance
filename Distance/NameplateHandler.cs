using System;
using System.Diagnostics;
using System.Numerics;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Distance;

internal static unsafe class NameplateHandler
{
	internal static void Init( Configuration configuration )
	{
		//	It's kinda jank to init a static class with an instance's data, but it'll never matter here, and the
		//	plugin service effectively already crosses this bridge this anyway, so it's not worth worrying about.
		mConfiguration = configuration;

		if( mConfiguration.NameplateDistancesConfig.ShowNameplateDistances )
		{
			EnableNameplateDistances();
		}
    }

	internal static void Uninit()
	{
        DisableNameplateDistances();

		DestroyNameplateDistanceNodes();

		mNodeUpdateTimer.Reset();
		mDistanceUpdateTimer.Reset();

		mpNameplateAddon = null;
		mConfiguration = null;
	}

	internal static void EnableNameplateDistances()
	{
		if( !mEnabled )
		{
			try
			{
				Service.AddonLifecycle.RegisterListener( AddonEvent.PostDraw, "NamePlate", NameplateDrawDetour );
				mEnabled = true;
			}
			catch( Exception e )
			{
				Service.PluginLog.Error( $"Unknown error while trying to enable nameplate distances:\r\n{e}" );
				DisableNameplateDistances();
			}
		}
	}

	internal static void DisableNameplateDistances()
	{
		if( mEnabled )
		{
			try
			{
				Service.AddonLifecycle.UnregisterListener( NameplateDrawDetour );
			}
			catch( Exception e )
			{
				Service.PluginLog.Error( $"Unknown error while unregistering nameplate listener:\r\n{e}" );
			}

			try
			{
				mEnabled = false;
				mNodeUpdateTimer.Reset();
				mDistanceUpdateTimer.Reset();
				HideAllNameplateDistanceNodes();
			}
			catch( Exception e )
			{
				Service.PluginLog.Error( $"Unknown error while trying to disable nameplate distances:\r\n{e}" );
			}
		}
	}

	internal unsafe static void UpdateNameplateEntityDistanceData()
	{
		mDistanceUpdateTimer.Restart();

		//	Start with a clean slate.
		for( int i = 0; i < mNameplateDistanceInfoArray.Length; ++i )
		{
			mNameplateDistanceInfoArray[i].Invalidate();
			mShouldDrawDistanceInfoArray[i] = false;
		}

		if( FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance() != null &&
			FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule() != null &&
			FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetUI3DModule() != null )
		{
			var pUI3DModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetUI3DModule();

			//	Update the available distance data.
			for( int i = 0; i < pUI3DModule->NamePlateObjectInfoCount; ++i )
			{
				var pObjectInfo = pUI3DModule->NamePlateObjectInfoPointers[i].Value;
				if( pObjectInfo != null &&
					pObjectInfo->GameObject != null &&
					pObjectInfo->NamePlateIndex >= 0 &&
					pObjectInfo->NamePlateIndex < mNameplateDistanceInfoArray.Length )
				{
					var pObject = pObjectInfo->GameObject;
					if( pObject != null )
					{
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].IsValid = true;
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].TargetKind = (Dalamud.Game.ClientState.Objects.Enums.ObjectKind)pObject->ObjectKind;
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].ObjectID = pObject->EntityId;
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].ObjectAddress = new IntPtr( pObject );
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].PlayerPosition = Service.ClientState.LocalPlayer?.Position ?? Vector3.Zero;
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].TargetPosition = new( pObject->Position.X, pObject->Position.Y, pObject->Position.Z );
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].TargetRadius_Yalms = pObject->HitboxRadius;

						//***** TODO: Update the following to be right at some point maybe.  Think about whether we care about these parts for nameplates.
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].BNpcID = 0;
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].HasAggroRangeData = false;
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].AggroRange_Yalms = 0;

						//	Whether we actually want to draw the distance on the nameplate.
						mShouldDrawDistanceInfoArray[pObjectInfo->NamePlateIndex] = ShouldDrawDistanceForNameplate( pObjectInfo->NamePlateIndex );
					}
				}
			}
		}

		mDistanceUpdateTimer.Stop();
	}

	private static bool ShouldDrawDistanceForNameplate( int i )
	{
		if( Service.ClientState.IsPvP ) return false;

		if( i < 0 || i >= mNameplateDistanceInfoArray.Length ) return false;
		if( mConfiguration == null ) return false;
		if( !mConfiguration.NameplateDistancesConfig.ShowNameplateDistances ) return false;
		if( !mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceForConditions( Service.Condition[ConditionFlag.InCombat], Service.Condition[ConditionFlag.BoundByDuty] ) ) return false;

		var distanceInfo = mNameplateDistanceInfoArray[i];

		if( distanceInfo.ObjectID == Service.ClientState.LocalPlayer?.EntityId ) return false;

		bool filtersPermitShowing = mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceForObjectKind( distanceInfo.TargetKind ) &&
									mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceForClassJob( Service.ClientState.LocalPlayer?.ClassJob.RowId ?? 0 );

		if( mConfiguration.NameplateDistancesConfig.ShowAll )
		{
			return filtersPermitShowing;
		}
		else
		{
			if( mConfiguration.NameplateDistancesConfig.FiltersAreExclusive )
			{
				if( !filtersPermitShowing ) return false;
			}
			else
			{
				if( filtersPermitShowing ) return true;
			}

			if( mConfiguration.NameplateDistancesConfig.ShowTarget &&
				TargetResolver.GetTarget( TargetType.Target ).IsSameObject( distanceInfo.ObjectID, distanceInfo.ObjectAddress ) ) return true;
			if( mConfiguration.NameplateDistancesConfig.ShowSoftTarget &&
				TargetResolver.GetTarget( TargetType.SoftTarget ).IsSameObject( distanceInfo.ObjectID, distanceInfo.ObjectAddress ) ) return true;
			if( mConfiguration.NameplateDistancesConfig.ShowFocusTarget &&
				TargetResolver.GetTarget( TargetType.FocusTarget ).IsSameObject( distanceInfo.ObjectID, distanceInfo.ObjectAddress ) ) return true;
			if( mConfiguration.NameplateDistancesConfig.ShowMouseoverTarget &&
				TargetResolver.GetTarget( TargetType.MouseOverTarget ).IsSameObject( distanceInfo.ObjectID, distanceInfo.ObjectAddress ) ) return true;

			if( mConfiguration.NameplateDistancesConfig.ShowAggressive &&
				distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
				GameObjectUtils.ObjectIsAggressive( distanceInfo.ObjectID ) ) return true;
			if( mConfiguration.NameplateDistancesConfig.ShowPartyMembers &&
				distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player &&
				PartyUtils.ObjectIsPartyMember( distanceInfo.ObjectID ) ) return true;
			if( mConfiguration.NameplateDistancesConfig.ShowAllianceMembers &&
				distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player &&
				PartyUtils.ObjectIsAllianceMember( distanceInfo.ObjectID ) ) return true;	//	Make sure this comes after party check, because alliance check is exclusive of party members.

			return false;
		}
	}

	private static void NameplateDrawDetour( AddonEvent type, AddonArgs args )
	{
		var pNameplateAddon = (AddonNamePlate*)args.Addon;

		try
		{
			if( mpNameplateAddon != pNameplateAddon )
			{
				Service.PluginLog.Debug( $"Nameplate draw detour pointer mismatch: 0x{(IntPtr)mpNameplateAddon:X} -> 0x{(IntPtr)pNameplateAddon:X}" );
				//	I don't know how to safely clean up our own nodes when the addon reloads.  The addon might take care of our inserted nodes when it cleans itself up anyway.
				for( int i = 0; i < mDistanceTextNodes.Length; ++i ) mDistanceTextNodes[i] = null;
				mpNameplateAddon = pNameplateAddon;
				if( mpNameplateAddon != null ) CreateNameplateDistanceNodes();
			}

			UpdateNameplateEntityDistanceData();
			UpdateNameplateDistanceNodes();
		}
		catch( Exception e )
		{
			Service.PluginLog.Error( $"Unknown error in nameplate draw hook.  Disabling nameplate distances.\r\n{e}" );
			mConfiguration.NameplateDistancesConfig.ShowNameplateDistances = false;
			DisableNameplateDistances();
		}
	}

	private static void HideAllNameplateDistanceNodes()
	{
		for( int i = 0; i < mNameplateDistanceInfoArray.Length; ++i )
		{
			HideNameplateDistanceTextNode( i );
		}
	}

	//	Note: The way nameplate text in the base game currently works is that it is set to use a high font size, and then
	//	scaled by half.  In order to match as well as we can, especially with "new"-style nameplates, we need to do the
	//	same, but it also won't make sense to the user to have font sizes for nameplates not match font sizes elsewhere,
	//	so we also multiply the configured font size by the inverse of the node scale to get the real font size.  This
	//	scaling also affects alignment calculations, so beware.

	//***** TODO: This probably actually depends on whether the game is set to use high res UI (under graphics settings).  Test this and account for it if necessary.

	private static void UpdateNameplateDistanceNodes()
	{
		mNodeUpdateTimer.Restart();

		for( int i = 0; i < mNameplateDistanceInfoArray.Length; ++i )
		{
			if( !mNameplateDistanceInfoArray[i].IsValid ) continue;

			TextNodeDrawData drawData = GetNameplateNodeDrawData( i );

			int textPositionX = 0;
			int textPositionY = 0;
			AlignmentType textAlignment = mConfiguration.NameplateDistancesConfig.DistanceFontAlignment;

			var nameplateObject = GetNameplateObject( i );
			if( nameplateObject != null && mConfiguration.NameplateDistancesConfig.AutomaticallyAlignText )
			{
				textPositionX = mConfiguration.NameplateDistancesConfig.DistanceFontAlignment switch
				{
					AlignmentType.BottomLeft => drawData.Width / 2 - nameplateObject.Value.TextW / 2,
					AlignmentType.Bottom => drawData.Width / 2 - (int)( AtkNodeHelpers.DefaultTextNodeWidth * drawData.ScaleX ) / 2,
					AlignmentType.BottomRight => drawData.Width / 2 + nameplateObject.Value.TextW / 2 - (int)( AtkNodeHelpers.DefaultTextNodeWidth * drawData.ScaleX ),
					_ => 0,
				};

				if( mConfiguration.NameplateDistancesConfig.PlaceTextBelowName )
				{
					textPositionY = drawData.Height - (int)( AtkNodeHelpers.DefaultTextNodeHeight * drawData.ScaleY ) / 2;
				}
				else
				{
					textPositionY = drawData.Height - nameplateObject.Value.TextH - (int)( AtkNodeHelpers.DefaultTextNodeHeight * drawData.ScaleY );
				}

				//	Change the node to be top aligned (instead of bottom) if placing below name.
				if( mConfiguration.NameplateDistancesConfig.PlaceTextBelowName ) textAlignment -= 6;
				textAlignment = (AlignmentType)Math.Max( 0, Math.Min( (int)textAlignment, 8 ) );
			}

			drawData.PositionX = (short)( textPositionX + mConfiguration.NameplateDistancesConfig.DistanceTextOffset.X );
			drawData.PositionY = (short)( textPositionY + mConfiguration.NameplateDistancesConfig.DistanceTextOffset.Y );
			drawData.UseDepth = !ObjectIsNonDepthTarget( mNameplateDistanceInfoArray[i].ObjectID, mNameplateDistanceInfoArray[i].ObjectAddress ); //Ideally we would just read this from the nameplate text node, but ClientStructs doesn't seem to have a way to do that.
			drawData.FontSize = (byte)( mConfiguration.NameplateDistancesConfig.DistanceFontSize * 1f / drawData.ScaleY );
			drawData.Alignment = textAlignment;
			drawData.Font = mConfiguration.NameplateDistancesConfig.DistanceFontHeavy ? FontType.MiedingerMed : FontType.Axis;

			float distance_Yalms = mConfiguration.NameplateDistancesConfig.DistanceIsToRing ? mNameplateDistanceInfoArray[i].DistanceFromTargetRing_Yalms : mNameplateDistanceInfoArray[i].DistanceFromTarget_Yalms;
			distance_Yalms -= GetDistanceOffset_Yalms( mNameplateDistanceInfoArray[i].TargetKind );
			SetDistanceBasedColor( ref drawData, distance_Yalms, mNameplateDistanceInfoArray[i].ObjectID, mNameplateDistanceInfoArray[i].TargetKind );
			SetDistanceBasedAlpha( ref drawData, distance_Yalms, mNameplateDistanceInfoArray[i].ObjectID, mNameplateDistanceInfoArray[i].TargetKind );

			float displayDistance_Yalms = mConfiguration.NameplateDistancesConfig.AllowNegativeDistances ? distance_Yalms : Math.Max( 0, distance_Yalms );
			string distanceText = displayDistance_Yalms.ToString( $"F{mConfiguration.NameplateDistancesConfig.DistanceDecimalPrecision}" );
			if( mConfiguration.NameplateDistancesConfig.ShowUnitsOnDistance ) distanceText += LocalizationHelpers.DistanceUnitShort;

			UpdateNameplateDistanceTextNode( i, distanceText, drawData, mShouldDrawDistanceInfoArray[i] );
		}

		if( DEBUG_mSetTextFlags ) DEBUG_mSetTextFlags = false;
		mNodeUpdateTimer.Stop();
	}

	private static float GetDistanceOffset_Yalms( Dalamud.Game.ClientState.Objects.Enums.ObjectKind objectKind )
	{
		return objectKind switch
		{
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player => mConfiguration.NameplateDistancesConfig.DistanceOffset_Player_Yalms,
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc => mConfiguration.NameplateDistancesConfig.DistanceOffset_BNpc_Yalms,
			_ => mConfiguration.NameplateDistancesConfig.DistanceOffset_Other_Yalms,
		};
	}

	private static void SetDistanceBasedColor( ref TextNodeDrawData rDrawData, float distance_Yalms, UInt32 objectID, Dalamud.Game.ClientState.Objects.Enums.ObjectKind objectKind )
	{
		bool setColor = false;
		Vector4 textColorToUse = new();
		Vector4 edgeColorToUse = new();

		if( mConfiguration.NameplateDistancesConfig.UseDistanceBasedColor_Party &&
			objectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player &&
			PartyUtils.ObjectIsPartyMember( objectID ) )
		{
			if( distance_Yalms > mConfiguration.NameplateDistancesConfig.FarThresholdDistance_Party_Yalms )
			{
				setColor = !mConfiguration.NameplateDistancesConfig.FarRangeTextUseNameplateColor_Party;
				textColorToUse = mConfiguration.NameplateDistancesConfig.FarRangeTextColor_Party;
				edgeColorToUse = mConfiguration.NameplateDistancesConfig.FarRangeTextEdgeColor_Party;
			}
			else if( distance_Yalms > mConfiguration.NameplateDistancesConfig.NearThresholdDistance_Party_Yalms )
			{
				setColor = !mConfiguration.NameplateDistancesConfig.MidRangeTextUseNameplateColor_Party;
				textColorToUse = mConfiguration.NameplateDistancesConfig.MidRangeTextColor_Party;
				edgeColorToUse = mConfiguration.NameplateDistancesConfig.MidRangeTextEdgeColor_Party;
			}
			else
			{
				setColor = !mConfiguration.NameplateDistancesConfig.NearRangeTextUseNameplateColor_Party;
				textColorToUse = mConfiguration.NameplateDistancesConfig.NearRangeTextColor_Party;
				edgeColorToUse = mConfiguration.NameplateDistancesConfig.NearRangeTextEdgeColor_Party;
			}
		}
		else if(	mConfiguration.NameplateDistancesConfig.UseDistanceBasedColor_BNpc &&
					objectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc )
		{
			if( distance_Yalms > mConfiguration.NameplateDistancesConfig.FarThresholdDistance_BNpc_Yalms )
			{
				setColor = !mConfiguration.NameplateDistancesConfig.FarRangeTextUseNameplateColor_BNpc;
				textColorToUse = mConfiguration.NameplateDistancesConfig.FarRangeTextColor_BNpc;
				edgeColorToUse = mConfiguration.NameplateDistancesConfig.FarRangeTextEdgeColor_BNpc;
			}
			else if( distance_Yalms > mConfiguration.NameplateDistancesConfig.NearThresholdDistance_BNpc_Yalms )
			{
				setColor = !mConfiguration.NameplateDistancesConfig.MidRangeTextUseNameplateColor_BNpc;
				textColorToUse = mConfiguration.NameplateDistancesConfig.MidRangeTextColor_BNpc;
				edgeColorToUse = mConfiguration.NameplateDistancesConfig.MidRangeTextEdgeColor_BNpc;
			}
			else
			{
				setColor = !mConfiguration.NameplateDistancesConfig.NearRangeTextUseNameplateColor_BNpc;
				textColorToUse = mConfiguration.NameplateDistancesConfig.NearRangeTextColor_BNpc;
				edgeColorToUse = mConfiguration.NameplateDistancesConfig.NearRangeTextEdgeColor_BNpc;
			}
		}

		if( setColor )
		{
			rDrawData.TextColorR = (byte)( textColorToUse.X * 255f );
			rDrawData.TextColorG = (byte)( textColorToUse.Y * 255f );
			rDrawData.TextColorB = (byte)( textColorToUse.Z * 255f );
			rDrawData.TextColorA = (byte)( textColorToUse.W * 255f );
			rDrawData.EdgeColorR = (byte)( edgeColorToUse.X * 255f );
			rDrawData.EdgeColorG = (byte)( edgeColorToUse.Y * 255f );
			rDrawData.EdgeColorB = (byte)( edgeColorToUse.Z * 255f );
			rDrawData.EdgeColorA = (byte)( edgeColorToUse.W * 255f );
		}
	}

	private static void SetDistanceBasedAlpha( ref TextNodeDrawData rDrawData, float distance_Yalms, UInt32 objectID, Dalamud.Game.ClientState.Objects.Enums.ObjectKind objectKind )
	{
		float fadeAlphaGain = 1f;

		if( mConfiguration.NameplateDistancesConfig.EnableFading_Party &&
			objectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player &&
			PartyUtils.ObjectIsPartyMember( objectID ) )
		{
			if( distance_Yalms < 0 )
			{
				fadeAlphaGain = MathUtils.LinearInterpolation( -mConfiguration.NameplateDistancesConfig.FadeoutThresholdInner_Party_Yalms - mConfiguration.NameplateDistancesConfig.FadeoutIntervalInner_Party_Yalms, 0f, -mConfiguration.NameplateDistancesConfig.FadeoutThresholdInner_Party_Yalms, 1f, distance_Yalms );
			}
			else
			{
				fadeAlphaGain = MathUtils.LinearInterpolation( mConfiguration.NameplateDistancesConfig.FadeoutThresholdOuter_Party_Yalms, 1f, mConfiguration.NameplateDistancesConfig.FadeoutThresholdOuter_Party_Yalms + mConfiguration.NameplateDistancesConfig.FadeoutIntervalOuter_Party_Yalms, 0f, distance_Yalms );
			}

			if( mConfiguration.NameplateDistancesConfig.InvertFading_Party )
			{
				fadeAlphaGain = 1f - fadeAlphaGain;
			}
		}
		else if( mConfiguration.NameplateDistancesConfig.EnableFading_BNpc &&
				objectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc )
		{
			if( distance_Yalms < 0 )
			{
				fadeAlphaGain = MathUtils.LinearInterpolation( -mConfiguration.NameplateDistancesConfig.FadeoutThresholdInner_BNpc_Yalms - mConfiguration.NameplateDistancesConfig.FadeoutIntervalInner_BNpc_Yalms, 0f, -mConfiguration.NameplateDistancesConfig.FadeoutThresholdInner_BNpc_Yalms, 1f, distance_Yalms );
			}
			else
			{
				fadeAlphaGain = MathUtils.LinearInterpolation( mConfiguration.NameplateDistancesConfig.FadeoutThresholdOuter_BNpc_Yalms, 1f, mConfiguration.NameplateDistancesConfig.FadeoutThresholdOuter_BNpc_Yalms + mConfiguration.NameplateDistancesConfig.FadeoutIntervalOuter_BNpc_Yalms, 0f, distance_Yalms );
			}

			if( mConfiguration.NameplateDistancesConfig.InvertFading_BNpc )
			{
				fadeAlphaGain = 1f - fadeAlphaGain;
			}
		}
		else if( mConfiguration.NameplateDistancesConfig.EnableFading_Other &&
				objectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
				!( objectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player &&
				PartyUtils.ObjectIsPartyMember( objectID ) ) )
		{
			if( distance_Yalms < 0 )
			{
				fadeAlphaGain = MathUtils.LinearInterpolation( -mConfiguration.NameplateDistancesConfig.FadeoutThresholdInner_Other_Yalms - mConfiguration.NameplateDistancesConfig.FadeoutIntervalInner_Other_Yalms, 0f, -mConfiguration.NameplateDistancesConfig.FadeoutThresholdInner_Other_Yalms, 1f, distance_Yalms );
			}
			else
			{
				fadeAlphaGain = MathUtils.LinearInterpolation( mConfiguration.NameplateDistancesConfig.FadeoutThresholdOuter_Other_Yalms, 1f, mConfiguration.NameplateDistancesConfig.FadeoutThresholdOuter_Other_Yalms + mConfiguration.NameplateDistancesConfig.FadeoutIntervalOuter_Other_Yalms, 0f, distance_Yalms );
			}

			if( mConfiguration.NameplateDistancesConfig.InvertFading_Other )
			{
				fadeAlphaGain = 1f - fadeAlphaGain;
			}
		}

		rDrawData.Alpha = (byte)( (float)rDrawData.Alpha * fadeAlphaGain );
	}

	//***** TODO: It would be strongly preferable to pull the depth flag off of the nameplate node itself if we can find it, rather than maintaining this logic.
	private static bool ObjectIsNonDepthTarget( uint objectID, IntPtr pObject )
	{
		if( objectID == 0 )
		{
			return false;
		}
		else if( objectID == 0xE0000000 )
		{
			if( pObject == IntPtr.Zero ) return false;
			var target = TargetResolver.GetTarget( TargetType.Target );
			var softTarget = TargetResolver.GetTarget( TargetType.SoftTarget );
			return pObject == target?.Address || pObject == softTarget?.Address;
		}
		else
		{
			uint targetOID = TargetResolver.GetTarget( TargetType.Target )?.EntityId ?? 0;
			uint softTargetOID = TargetResolver.GetTarget( TargetType.SoftTarget )?.EntityId ?? 0;
			return objectID == targetOID || objectID == softTargetOID;
		}
	}

	private static TextNodeDrawData GetNameplateNodeDrawData( int i )
	{
		var nameplateObject = GetNameplateObject( i );
		if( nameplateObject == null ) return TextNodeDrawData.Default;

		var pNameplateIconNode = nameplateObject.Value.MarkerIcon;	//	Need to check this for people that are using player icon plugins with names hidden.
		var pNameplateResNode = nameplateObject.Value.NameContainer;
		var pNameplateTextNode = nameplateObject.Value.NameText;
		if( pNameplateTextNode != null && pNameplateResNode != null && pNameplateIconNode != null )
		{
			return new TextNodeDrawData()
			{
				Show = pNameplateIconNode->AtkResNode.IsVisible() || ( pNameplateResNode->IsVisible() && pNameplateTextNode->AtkResNode.IsVisible() ),
				PositionX = (short)pNameplateResNode->X,
				PositionY = (short)pNameplateResNode->Y,
				Width = pNameplateResNode->Width,
				Height = pNameplateResNode->Height,
				ScaleX = pNameplateTextNode->AtkResNode.ScaleX,
				ScaleY = pNameplateTextNode->AtkResNode.ScaleY,
				Alpha = pNameplateTextNode->AtkResNode.Color.A,
				TextColorA = pNameplateTextNode->TextColor.A,
				TextColorR = pNameplateTextNode->TextColor.R,
				TextColorG = pNameplateTextNode->TextColor.G,
				TextColorB = pNameplateTextNode->TextColor.B,
				EdgeColorA = pNameplateTextNode->EdgeColor.A,
				EdgeColorR = pNameplateTextNode->EdgeColor.R,
				EdgeColorG = pNameplateTextNode->EdgeColor.G,
				EdgeColorB = pNameplateTextNode->EdgeColor.B,
				FontSize = pNameplateTextNode->FontSize,
				Alignment = pNameplateTextNode->AlignmentType,
				Font = pNameplateTextNode-> FontType,
				LineSpacing = pNameplateTextNode->LineSpacing,
				CharSpacing = pNameplateTextNode->CharSpacing,
				//***** TODO
				/*UseNewNameplateStyle = mConfiguration.NameplateDistancesConfig.NameplateStyle == NameplateStyle.MatchGame &&
										( pNameplateTextNode->TextFlags2 & 0x80 ) != 0 ||
										mConfiguration.NameplateDistancesConfig.NameplateStyle == NameplateStyle.New,*/
			};
		}
		else
		{
			return TextNodeDrawData.Default;
		}
	}

	private static AddonNamePlate.NamePlateObject? GetNameplateObject( int i )
	{
		if( i < AddonNamePlate.NumNamePlateObjects &&
			mpNameplateAddon != null &&
			mpNameplateAddon->NamePlateObjectArray[i].RootComponentNode != null )
		{
			return mpNameplateAddon->NamePlateObjectArray[i];
		}
		else
		{
			return null;
		}
	}

	private static AtkComponentNode* GetNameplateComponentNode( int i )
	{
		var nameplateObject = GetNameplateObject( i );
		return nameplateObject != null ? nameplateObject.Value.RootComponentNode : null;
	}

	private static void CreateNameplateDistanceNodes()
	{
		for( int i = 0; i < AddonNamePlate.NumNamePlateObjects; ++i )
		{
			var nameplateObject = GetNameplateObject( i );
			if( nameplateObject == null )
			{
				Service.PluginLog.Warning( $"Unable to obtain nameplate object for index {i}" );
				continue;
			}
			var pNameplateResNode = nameplateObject.Value.NameContainer;

			//	Make a node.
			var pNewNode = AtkNodeHelpers.CreateOrphanTextNode( mNameplateDistanceNodeIDBase + (uint)i, TextFlags.Edge | TextFlags.Glare );

			//	Set up the node in the addon.
			if( pNewNode != null )
			{
				var pLastChild = pNameplateResNode->ChildNode;
				while( pLastChild->PrevSiblingNode != null ) pLastChild = pLastChild->PrevSiblingNode;
				pNewNode->AtkResNode.NextSiblingNode = pLastChild;
				pNewNode->AtkResNode.ParentNode = pNameplateResNode;
				pLastChild->PrevSiblingNode = (AtkResNode*)pNewNode;
				nameplateObject.Value.RootComponentNode->Component->UldManager.UpdateDrawNodeList();
				pNewNode->AtkResNode.SetUseDepthBasedPriority( true );

				//	Store it in our array.
				mDistanceTextNodes[i] = pNewNode;

				Service.PluginLog.Verbose( $"Attached new text node for nameplate {i} (0x{(IntPtr)pNewNode:X})." );
			}
			else
			{
				Service.PluginLog.Warning( $"Unable to create new text node for nameplate {i}." );
			}
		}
	}

	private static void DestroyNameplateDistanceNodes()
	{
		//	If the addon has moved since disabling the hook, it's impossible to know whether our
		//	node pointers are valid anymore, so we have to just let them leak in that case.
		var pCurrentNameplateAddon = (AddonNamePlate*)Service.GameGui.GetAddonByName( "NamePlate", 1 );
		if( mpNameplateAddon == null || mpNameplateAddon != pCurrentNameplateAddon )
		{
			Service.PluginLog.Warning( $"Unable to cleanup nameplate nodes due to addon address mismatch during unload (Cached: 0x{(IntPtr)mpNameplateAddon:X}, Current: 0x{(IntPtr)pCurrentNameplateAddon})." );
			return;
		}

		for( int i = 0; i < AddonNamePlate.NumNamePlateObjects; ++i )
		{
			var pTextNode = mDistanceTextNodes[i];
			var pNameplateNode = GetNameplateComponentNode( i );
			Service.PluginLog.Verbose( $"Attempting to remove text node 0x{(IntPtr)pTextNode:X} for nameplate {i} on component node 0x{(IntPtr)pNameplateNode:X}." );
			if( pTextNode != null && pNameplateNode != null )
			{
				try
				{
					if( pTextNode->AtkResNode.PrevSiblingNode != null ) pTextNode->AtkResNode.PrevSiblingNode->NextSiblingNode = pTextNode->AtkResNode.NextSiblingNode;
					if( pTextNode->AtkResNode.NextSiblingNode != null ) pTextNode->AtkResNode.NextSiblingNode->PrevSiblingNode = pTextNode->AtkResNode.PrevSiblingNode;
					pNameplateNode->Component->UldManager.UpdateDrawNodeList();
					pTextNode->AtkResNode.Destroy( true );
					mDistanceTextNodes[i] = null;
					Service.PluginLog.Verbose( $"Cleanup of nameplate {i} complete." );
				}
				catch( Exception e )
				{
					Service.PluginLog.Error( $"Unknown error while removing text node 0x{(IntPtr)pTextNode:X} for nameplate {i} on component node 0x{(IntPtr)pNameplateNode:X}:\r\n{e}" );
				}
			}
		}
	}

	private static void HideNameplateDistanceTextNode( int i )
	{
		var pNode = mDistanceTextNodes[i];
		if( pNode != null )
		{
			pNode->AtkResNode.ToggleVisibility( false );
		}
	}

	private static void UpdateNameplateDistanceTextNode( int i, string str, TextNodeDrawData drawData, bool show = true )
	{
		var pNode = mDistanceTextNodes[i];
		if( pNode != null )
		{
			bool visible = show && drawData.Show && drawData.Alpha > 0;
			pNode->AtkResNode.ToggleVisibility( visible );
			if( visible )
			{
				pNode->AtkResNode.SetPositionShort( drawData.PositionX, drawData.PositionY );
				pNode->AtkResNode.SetUseDepthBasedPriority( drawData.UseDepth );
				pNode->AtkResNode.SetScale( drawData.ScaleX, drawData.ScaleY );

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
				pNode->AlignmentType = drawData.Alignment;
				pNode->FontType = drawData.Font;
				pNode->LineSpacing = drawData.LineSpacing;
				pNode->CharSpacing = drawData.CharSpacing;

				if( DEBUG_mSetTextFlags )
				{
					pNode->TextFlags = (byte)DEBUG_mNameplateTextFlags;
					pNode->TextFlags2 = (byte)DEBUG_mNameplateTextFlags2;
				}

				pNode->SetText( str );
			}
		}
	}

	internal static Int64 NodeUpdateTime_uSec => mNodeUpdateTimer.ElapsedMicroseconds();
	internal static Int64 DistanceUpdateTime_uSec => mDistanceUpdateTimer.ElapsedMicroseconds();

	internal static IntPtr DEBUG_CachedNameplateAddonPtr => new( mpNameplateAddon );
	internal static ReadOnlySpan<DistanceInfo> DEBUG_NameplateDistanceInfo => new( mNameplateDistanceInfoArray );
	internal static ReadOnlySpan<bool> DEBUG_ShouldDrawDistanceInfo => new( mShouldDrawDistanceInfoArray );
	internal static bool DEBUG_mSetTextFlags = false;
	internal static int DEBUG_mNameplateTextFlags = (int)( TextFlags.Edge | TextFlags.Glare );
	internal static int DEBUG_mNameplateTextFlags2 = 0;

	//	Delgates and Hooks
	private delegate void NameplateDrawFuncDelegate( AddonEvent type, AddonArgs args );
	private static readonly NameplateDrawFuncDelegate mdNameplateDraw = new( NameplateDrawDetour );

	//	Members
	private static bool mEnabled = false;
	private static readonly DistanceInfo[] mNameplateDistanceInfoArray = new DistanceInfo[AddonNamePlate.NumNamePlateObjects];
	private static readonly bool[] mShouldDrawDistanceInfoArray = new bool[AddonNamePlate.NumNamePlateObjects];
	private static Configuration mConfiguration = null;
	private static AddonNamePlate* mpNameplateAddon = null;
	private static readonly AtkTextNode*[] mDistanceTextNodes = new AtkTextNode*[AddonNamePlate.NumNamePlateObjects];
	private static readonly Stopwatch mNodeUpdateTimer = new();
	private static readonly Stopwatch mDistanceUpdateTimer = new();

	//	Note: Node IDs only need to be unique within a given addon.
	internal const uint mNameplateDistanceNodeIDBase = 0x6C78C400;    //YOLO hoping for no collisions.
}
