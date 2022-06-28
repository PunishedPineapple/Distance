using System;
using System.Diagnostics;

using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Distance
{
	internal static unsafe class NameplateHandler
	{
		internal static void Init( SigScanner sigScanner, ClientState clientState, Condition condition, Configuration configuration )
		{
			mClientState = clientState;
			mCondition = condition;
			mConfiguration = configuration;	//	Ya it's kinda jank to init a static class with an instance's data, but it'll never matter here, and just as much work to make this non-static.

			if( sigScanner == null )
			{
				throw new Exception( "Error in \"MemoryHandler.Init()\": A null SigScanner was passed!" );
			}

			//	Get Function Pointers, etc.
			try
			{
				mfpOnNameplateDraw = sigScanner.ScanText( "0F B7 81 ?? ?? ?? ?? 4C 8B C1 66 C1 E0 06" );
				if( mfpOnNameplateDraw != IntPtr.Zero )
				{
					mNameplateDrawHook = new Hook<NameplateDrawFuncDelegate>( mfpOnNameplateDraw, mdNameplateDraw );
					mNameplateDrawHook?.Enable();
				}
			}
			catch( Exception e )
			{
				mNameplateDrawHook?.Disable();
				mNameplateDrawHook?.Dispose();
				mNameplateDrawHook = null;
				PluginLog.LogError( $"Error in \"NameplateHandler.Init()\" while searching for required function signatures; this probably means that the plugin needs to be updated due to changes in Final Fantasy XIV.\r\n{e}" );
			}
		}

		internal static void Uninit()
		{
			mNameplateDrawHook?.Disable();
			mNameplateDrawHook?.Dispose();
			mNameplateDrawHook = null;

			DestroyNameplateDistanceNodes();
			mpNameplateAddon = null;

			mConfiguration = null;
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

			if( mClientState != null &&
				FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance() != null &&
				FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule() != null &&
				FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetUI3DModule() != null )
			{
				var pUI3DModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetUI3DModule();

				//	Update the available distance data.
				for( int i = 0; i < pUI3DModule->NamePlateObjectInfoCount; ++i )
				{
					var pObjectInfo = ((UI3DModule.ObjectInfo**)pUI3DModule->NamePlateObjectInfoPointerArray)[i];
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
							mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].ObjectID = pObject->ObjectID;
							mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].PlayerPosition = mClientState.LocalPlayer?.Position ?? System.Numerics.Vector3.Zero;
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
			mDistanceUpdateTime_uSec = mDistanceUpdateTimer.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;
		}

		private static bool ShouldDrawDistanceForNameplate( int i )
		{
			if( i < 0 || i >= mNameplateDistanceInfoArray.Length ) return false;
			if( mConfiguration == null ) return false;
			if( !mConfiguration.NameplateDistancesConfig.ShowNameplateDistances ) return false;

			var distanceInfo = mNameplateDistanceInfoArray[i];

			if( distanceInfo.ObjectID == mClientState?.LocalPlayer.ObjectId ) return false;

			bool filtersPermitShowing = false;
			if( distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player && mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceOnPlayers ) filtersPermitShowing = true;
			if( distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceOnBattleNpc ) filtersPermitShowing = true;
			if( distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc && mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceOnEventNpc ) filtersPermitShowing = true;
			if( distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure && mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceOnTreasure ) filtersPermitShowing = true;
			if( distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Aetheryte && mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceOnAetheryte ) filtersPermitShowing = true;
			if( distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint && mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceOnGatheringNode ) filtersPermitShowing = true;
			if( distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj && mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceOnEventObj ) filtersPermitShowing = true;
			if( distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Companion && mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceOnCompanion ) filtersPermitShowing = true;
			if( distanceInfo.TargetKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Housing && mConfiguration.NameplateDistancesConfig.Filters.ShowDistanceOnHousing ) filtersPermitShowing = true;

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

				if( mConfiguration.NameplateDistancesConfig.mShowTarget && TargetResolver.GetTarget( TargetType.Target )?.ObjectId == distanceInfo.ObjectID ) return true;
				if( mConfiguration.NameplateDistancesConfig.mShowSoftTarget && TargetResolver.GetTarget( TargetType.SoftTarget )?.ObjectId == distanceInfo.ObjectID ) return true;
				if( mConfiguration.NameplateDistancesConfig.mShowFocusTarget && TargetResolver.GetTarget( TargetType.FocusTarget )?.ObjectId == distanceInfo.ObjectID ) return true;
				if( mConfiguration.NameplateDistancesConfig.mShowMouseoverTarget && TargetResolver.GetTarget( TargetType.MouseOverTarget )?.ObjectId == distanceInfo.ObjectID ) return true;

				//if( Aggressive ) return true;
				//if( Party Member ) return true;
				//if( Alliance Member ) return true;

				return false;
			}
		}

		private static void NameplateDrawDetour( AddonNamePlate* pThis )
		{
			mDrawHookTimer.Restart();

			if( mpNameplateAddon != pThis )
			{
				PluginLog.LogDebug( $"Nameplate draw detour pointer mismatch: 0x{new IntPtr( mpNameplateAddon ):X} -> 0x{new IntPtr( pThis ):X}" );
				//DestroyNameplateDistanceNodes();	//	Should we be doing this?  Does the game clean up the whole node tree including our stuff automatically if the UI gets reinitialized?
				for( int i = 0; i < mDistanceTextNodes.Length; ++i ) mDistanceTextNodes[i] = null;
				mpNameplateAddon = pThis;
				if( mpNameplateAddon != null )
				{
					CreateNameplateDistanceNodes();
				}
			}

			//***** TODO:	Either use the lighter hide all nodes function when config isn't set to show any nameplate stuff, or preferably
			//				disable the hook.  Is disabling the hook while in it safe?  Disabling the hook means that we could have an outdated
			//				addon pointer when trying to dispose though, which would crash.
			if( mConfiguration.NameplateDistancesConfig.ShowNameplateDistances )
			{
				UpdateNameplateDistanceNodes();
			}
			else
			{
				HideAllNameplateDistanceNodes();
			}
			mDrawHookTimer.Stop();
			mDrawHookTime_uSec = mDrawHookTimer.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;

			mNameplateDrawHook.Original( pThis );
		}

		private static void HideAllNameplateDistanceNodes()
		{
			for( int i = 0; i < mNameplateDistanceInfoArray.Length; ++i )
			{
				HideNameplateDistanceTextNode( i );
			}
		}

		private static void UpdateNameplateDistanceNodes()
		{
			for( int i = 0; i < mNameplateDistanceInfoArray.Length; ++i )
			{
				if( mNameplateDistanceInfoArray[i].IsValid )	//	If it's not valid, nameplate should be hiding itself and thus our node.  No need to duplicate that.
				{
					TextNodeDrawData drawData = GetNameplateNodeDrawData( i ) ?? new TextNodeDrawData()
					{
						PositionX = 0,
						PositionY = 0,
						Width = 288,
						Height = 107,
						UseDepth = true,
						TextColorA = (byte)255,
						TextColorR = (byte)255,
						TextColorG = (byte)255,
						TextColorB = (byte)255,
						EdgeColorA = (byte)255,
						EdgeColorR = (byte)255,
						EdgeColorG = (byte)255,
						EdgeColorB = (byte)255,
						FontSize = (byte)mConfiguration.NameplateDistancesConfig.DistanceFontSize,
						AlignmentFontType = (byte)( mConfiguration.NameplateDistancesConfig.DistanceFontAlignment | ( mConfiguration.NameplateDistancesConfig.DistanceFontHeavy ? 0x10 : 0 ) ),
						LineSpacing = 24,
						CharSpacing = 1
					};

					int textOffsetX = 0;
					int textOffsetY = 0;
					var nameplateObject = GetNameplateObject( i );
					if( nameplateObject != null )
					{
						textOffsetX = mConfiguration.NameplateDistancesConfig.DistanceFontAlignment switch
						{
							6 => drawData.Width / 2 - nameplateObject.Value.TextW / 2,
							7 => drawData.Width / 2 - 100,
							8 => drawData.Width / 2 + nameplateObject.Value.TextW / 2 - 200,
							_ => 0,
						};

						textOffsetY = drawData.Height - nameplateObject.Value.TextH - 14;
					}

					drawData.PositionX = (short)textOffsetX;
					drawData.PositionY = (short)textOffsetY;
					drawData.UseDepth = !ObjectIsNonDepthTarget( mNameplateDistanceInfoArray[i].ObjectID );
					drawData.FontSize = (byte)mConfiguration.NameplateDistancesConfig.DistanceFontSize;
					drawData.AlignmentFontType = (byte)( mConfiguration.NameplateDistancesConfig.DistanceFontAlignment | ( mConfiguration.NameplateDistancesConfig.DistanceFontHeavy ? 0x10 : 0 ) );

					float displayDistance = mConfiguration.NameplateDistancesConfig.DistanceIsToRing ? mNameplateDistanceInfoArray[i].DistanceFromTargetRing_Yalms : mNameplateDistanceInfoArray[i].DistanceFromTarget_Yalms;
					if( !mConfiguration.NameplateDistancesConfig.AllowNegativeDistances ) displayDistance = Math.Max( 0, displayDistance );
					string distanceText = displayDistance.ToString( $"F{mConfiguration.NameplateDistancesConfig.DistanceDecimalPrecision}" );
					if( mConfiguration.NameplateDistancesConfig.ShowUnitsOnDistance ) distanceText += "y";
					UpdateNameplateDistanceTextNode( i, distanceText, drawData, mShouldDrawDistanceInfoArray[i] );
				}
			}
		}

		//	Certain types of targets remove depth from their nameplates.  This is to help determine that.
		private static bool ObjectIsNonDepthTarget( uint objectID )
		{
			uint targetOID = TargetResolver.GetTarget( TargetType.Target )?.ObjectId ?? 0;
			uint softTargetOID = TargetResolver.GetTarget( TargetType.SoftTarget )?.ObjectId ?? 0;
			//uint focusTargetOID = TargetResolver.GetTarget( TargetType.FocusTarget )?.ObjectId ?? 0;

			return objectID != 0 && objectID != 0xE0000000 && ( objectID == targetOID || objectID == softTargetOID /*|| objectID == focusTargetOID*/ );
		}

		private static TextNodeDrawData? GetNameplateNodeDrawData( int i )
		{
			var nameplateObject = GetNameplateObject( i );
			if( nameplateObject == null ) return null;

			var pTargetNameResNode = nameplateObject.Value.ResNode;
			var pTargetNameTextNode = nameplateObject.Value.NameText;
			if( pTargetNameTextNode != null && pTargetNameResNode != null  )
			{
				return new TextNodeDrawData()
				{
					PositionX = (short)pTargetNameResNode->X,
					PositionY = (short)pTargetNameResNode->Y,
					Width = pTargetNameResNode->Width,
					Height = pTargetNameResNode->Height,
					TextColorA = ((AtkTextNode*)pTargetNameTextNode)->TextColor.A,
					TextColorR = ((AtkTextNode*)pTargetNameTextNode)->TextColor.R,
					TextColorG = ((AtkTextNode*)pTargetNameTextNode)->TextColor.G,
					TextColorB = ((AtkTextNode*)pTargetNameTextNode)->TextColor.B,
					EdgeColorA = ((AtkTextNode*)pTargetNameTextNode)->EdgeColor.A,
					EdgeColorR = ((AtkTextNode*)pTargetNameTextNode)->EdgeColor.R,
					EdgeColorG = ((AtkTextNode*)pTargetNameTextNode)->EdgeColor.G,
					EdgeColorB = ((AtkTextNode*)pTargetNameTextNode)->EdgeColor.B,
					FontSize = ((AtkTextNode*)pTargetNameTextNode)->FontSize,
					AlignmentFontType = ((AtkTextNode*)pTargetNameTextNode)->AlignmentFontType,
					LineSpacing = ((AtkTextNode*)pTargetNameTextNode)->LineSpacing,
					CharSpacing = ((AtkTextNode*)pTargetNameTextNode)->CharSpacing
				};
			}
			else
			{
				return null;
			}
		}

		private static AddonNamePlate.NamePlateObject? GetNameplateObject( int i )
		{
			if( i < AddonNamePlate.NumNamePlateObjects &&
				mpNameplateAddon != null &&
				mpNameplateAddon->NamePlateObjectArray[i].RootNode != null )
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
			return nameplateObject != null ? nameplateObject.Value.RootNode : null;
		}

		private static void CreateNameplateDistanceNodes()
		{
			for( int i = 0; i < AddonNamePlate.NumNamePlateObjects; ++i )
			{
				var nameplateObject = GetNameplateObject( i );
				if( nameplateObject == null )
				{
					PluginLog.LogWarning( $"Unable to obtain nameplate object for index {i}" );
					continue;
				}

				//	Make a node.
				var pNewNode = CreateOrphanTextNode( (uint)( mNameplateDistanceNodeIDBase + i ));

				var pNameplateResNode = nameplateObject.Value.ResNode;

				//	Set up the node in the addon.
				if( pNewNode != null )
				{
					var pLastChild = pNameplateResNode->ChildNode;
					while( pLastChild->PrevSiblingNode != null ) pLastChild = pLastChild->PrevSiblingNode;
					pNewNode->AtkResNode.NextSiblingNode = pLastChild;
					pNewNode->AtkResNode.ParentNode = pNameplateResNode;
					pLastChild->PrevSiblingNode = (AtkResNode*)pNewNode;
					nameplateObject.Value.RootNode->Component->UldManager.UpdateDrawNodeList();
					pNewNode->AtkResNode.SetUseDepthBasedPriority( true );

					//	Store it in our array.
					mDistanceTextNodes[i] = pNewNode;
				}
			}
		}

		private static void DestroyNameplateDistanceNodes()
		{
			if( mpNameplateAddon == null ) return;

			for( int i = 0; i < AddonNamePlate.NumNamePlateObjects; ++i )
			{
				var pTextNode = mDistanceTextNodes[i];
				if( pTextNode != null )
				{
					var pNameplateNode = GetNameplateComponentNode( i );

					if( pTextNode->AtkResNode.PrevSiblingNode != null ) pTextNode->AtkResNode.PrevSiblingNode->NextSiblingNode = pTextNode->AtkResNode.NextSiblingNode;
					if( pTextNode->AtkResNode.NextSiblingNode != null ) pTextNode->AtkResNode.NextSiblingNode->PrevSiblingNode = pTextNode->AtkResNode.PrevSiblingNode;
					pNameplateNode->Component->UldManager.UpdateDrawNodeList();
					pTextNode->AtkResNode.Destroy( true );

					mDistanceTextNodes[i] = null;
				}
			}
		}

		private static AtkTextNode* CreateOrphanTextNode( uint ID )
		{
			//	Just use some sane defaults.
			var pNewNode = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTextNode), 8);
			if( pNewNode != null )
			{
				IMemorySpace.Memset( pNewNode, 0, (ulong)sizeof( AtkTextNode ) );
				pNewNode->Ctor();

				pNewNode->AtkResNode.Type = NodeType.Text;
				pNewNode->AtkResNode.Flags = (short)( NodeFlags.AnchorLeft | NodeFlags.AnchorTop );
				pNewNode->AtkResNode.DrawFlags = 0;
				pNewNode->AtkResNode.SetPositionShort( 0, 0 );
				pNewNode->AtkResNode.SetWidth( 200 );
				pNewNode->AtkResNode.SetHeight( 14 );

				pNewNode->LineSpacing = 24;
				pNewNode->CharSpacing = 1;
				pNewNode->AlignmentFontType = (byte)AlignmentType.BottomLeft;
				pNewNode->FontSize = 12;
				pNewNode->TextFlags = (byte)( TextFlags.Edge );
				pNewNode->TextFlags2 = 0;

				pNewNode->AtkResNode.NodeID = ID;

				pNewNode->AtkResNode.Color.A = 0xFF;
				pNewNode->AtkResNode.Color.R = 0xFF;
				pNewNode->AtkResNode.Color.G = 0xFF;
				pNewNode->AtkResNode.Color.B = 0xFF;
			}

			return pNewNode;
		}

		private static void HideNameplateDistanceTextNode( int i )
		{
			var pNode = mDistanceTextNodes[i];
			if( pNode != null )
			{
				( (AtkResNode*)pNode )->ToggleVisibility( false );
			}
		}

		private static void UpdateNameplateDistanceTextNode( int i, string str, TextNodeDrawData drawData, bool show = true )
		{
			var pNode = mDistanceTextNodes[i];
			if( pNode != null )
			{
				( (AtkResNode*)pNode )->ToggleVisibility( show );
				if( show )
				{
					pNode->AtkResNode.SetPositionShort( drawData.PositionX, drawData.PositionY );
					pNode->AtkResNode.SetUseDepthBasedPriority( drawData.UseDepth );

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
		}

		//	Delgates and Hooks
		private delegate void NameplateDrawFuncDelegate( AddonNamePlate* pThis );
		private static readonly NameplateDrawFuncDelegate mdNameplateDraw = new( NameplateDrawDetour );
		private static IntPtr mfpOnNameplateDraw = IntPtr.Zero;
		private static Hook<NameplateDrawFuncDelegate> mNameplateDrawHook;

		//	Members
		internal static readonly DistanceInfo[] mNameplateDistanceInfoArray = new DistanceInfo[AddonNamePlate.NumNamePlateObjects];    //***** TODO: Expose the data properly. *****
		internal static readonly bool[] mShouldDrawDistanceInfoArray = new bool[AddonNamePlate.NumNamePlateObjects];
		private static ClientState mClientState;
		private static Condition mCondition;
		private static Configuration mConfiguration = null;
		private static AddonNamePlate* mpNameplateAddon = null;
		private static readonly AtkTextNode*[] mDistanceTextNodes = new AtkTextNode*[AddonNamePlate.NumNamePlateObjects];

		private static readonly Stopwatch mDrawHookTimer = new();
		private static readonly Stopwatch mDistanceUpdateTimer = new();
		internal static Int64 mDrawHookTime_uSec = 0;
		internal static Int64 mDistanceUpdateTime_uSec = 0;

		private static readonly uint mNameplateDistanceNodeIDBase = 0x6C78C400;    //YOLO hoping for no collisions.
	}
}
