using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace Distance
{
	internal static unsafe class NameplateHandler
	{
		public static void Init( SigScanner sigScanner, ClientState clientState, Condition condition )
		{
			mClientState = clientState;
			mCondition = condition;

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
					mNameplateDrawHook.Enable();
				}
			}
			catch( Exception e )
			{
				throw new Exception( $"Error in \"NameplateHandler.Init()\" while searching for required function signatures; this probably means that the plugin needs to be updated due to changes in Final Fantasy XIV.  Raw exception as follows:\r\n{e}" );
			}
		}

		public static void Uninit()
		{
			mNameplateDrawHook?.Disable();
			mNameplateDrawHook?.Dispose();
			mNameplateDrawHook = null;

			DestroyNameplateDistanceNodes();
			mpNameplateAddon = null;
		}

		unsafe public static void UpdateNameplateEntityDistanceData()
		{
			if( mClientState == null )
			{
				InvalidateAllNameplateDistanceData();
				return;
			}
			var pFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
			if( pFramework == null )
			{
				InvalidateAllNameplateDistanceData();
				return;
			}
			var pUIModule = pFramework->GetUiModule();
			if( pUIModule == null )
			{
				InvalidateAllNameplateDistanceData();
				return;
			}
			var pUI3DModule = pUIModule->GetUI3DModule();
			if( pUI3DModule == null )
			{
				InvalidateAllNameplateDistanceData();
				return;
			}

			int i = 0;
			for( ; i < pUI3DModule->NamePlateObjectInfoCount; ++i )
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
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].PlayerPosition = mClientState.LocalPlayer != null ? mClientState.LocalPlayer.Position : System.Numerics.Vector3.Zero;
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].TargetPosition = new( pObject->Position.X, pObject->Position.Y, pObject->Position.Z );
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].TargetRadius_Yalms = pObject->HitboxRadius;
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].BNpcNameID = (Dalamud.Game.ClientState.Objects.Enums.ObjectKind)pObject->ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc ? pObject->GetNpcID() : 0;
						float? aggroRange = BNpcAggroInfo.GetAggroRange( mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].BNpcNameID, mClientState.TerritoryType );
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].HasAggroRangeData = aggroRange.HasValue;
						mNameplateDistanceInfoArray[pObjectInfo->NamePlateIndex].AggroRange_Yalms = aggroRange ?? 0;
					}
					else
					{
						mNameplateDistanceInfoArray[i].Invalidate();
					}
				}
				else if( i >= 0 && i < mNameplateDistanceInfoArray.Length )
				{
					mNameplateDistanceInfoArray[i].Invalidate();
				}
			}

			//	Invalidate any that we couldn't update.
			for( ; i < mNameplateDistanceInfoArray.Length; ++i )
			{
				mNameplateDistanceInfoArray[i].Invalidate();
			}
		}

		private static void InvalidateAllNameplateDistanceData()
		{
			foreach( var entry in mNameplateDistanceInfoArray )
			{
				entry.Invalidate();
			}
		}

		private static void NameplateDrawDetour( AddonNamePlate* pThis )
		{
			if( mpNameplateAddon != pThis )
			{
				DestroyNameplateDistanceNodes();
				mpNameplateAddon = pThis;
				if( mpNameplateAddon != null )
				{
					CreateNameplateDistanceNodes();
				}
			}

			mNameplateDrawHook.Original( pThis );
		}

		public static TextNodeDrawData? GetNameplateNodeDrawData( int i )
		{
			var pNameplateNode = GetNameplateNode( i );
			if( pNameplateNode == null ) return null;

			int nameplateTextNodeIndex = 9;
			var pTargetNameNode = pNameplateNode->UldManager.NodeListSize > nameplateTextNodeIndex ? pNameplateNode->UldManager.NodeList[nameplateTextNodeIndex] : null;
			if( pTargetNameNode != null && pTargetNameNode->GetAsAtkTextNode() != null )
			{
				var pTargetNameTextNode = pTargetNameNode->GetAsAtkTextNode();
				return new TextNodeDrawData()
				{
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

		private static AtkComponentBase* GetNameplateNode( int i )
		{
			if( i < AddonNamePlate.NumNamePlateObjects &&
				mpNameplateAddon != null &&
				mpNameplateAddon->NamePlateObjectArray[i].RootNode != null )
			{
				return mpNameplateAddon->NamePlateObjectArray[i].RootNode->Component;
			}
			else
			{
				return null;
			}
		}

		private static void CreateNameplateDistanceNodes()
		{
			for( int i = 0; i < AddonNamePlate.NumNamePlateObjects; ++i )
			{
				var pNameplateNode = GetNameplateNode( i );

				//	Make a node.
				var pNewNode = CreateOrphanTextNode( (uint)( mNameplateDistanceNodeIDBase + i ));

				//	Set up the node in the addon.
				if( pNewNode != null )
				{
					var pLastChild = pNameplateNode->UldManager.RootNode;
					while( pLastChild->PrevSiblingNode != null ) pLastChild = pLastChild->PrevSiblingNode;
					pNewNode->AtkResNode.NextSiblingNode = pLastChild;
					pNewNode->AtkResNode.ParentNode = (AtkResNode*)pNameplateNode;
					pLastChild->PrevSiblingNode = (AtkResNode*)pNewNode;
					pNameplateNode->UldManager.UpdateDrawNodeList();
				}

				//	Store it in our array.
				mDistanceTextNodes[i] = pNewNode;
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
					var pNameplateNode = GetNameplateNode( i );

					if( pTextNode->AtkResNode.PrevSiblingNode != null ) pTextNode->AtkResNode.PrevSiblingNode->NextSiblingNode = pTextNode->AtkResNode.NextSiblingNode;
					if( pTextNode->AtkResNode.NextSiblingNode != null ) pTextNode->AtkResNode.NextSiblingNode->PrevSiblingNode = pTextNode->AtkResNode.PrevSiblingNode;
					pTextNode->AtkResNode.Destroy( true );
					pNameplateNode->UldManager.UpdateDrawNodeList();

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

		public static void UpdateNameplateDistanceTextNode( int i, string str, TextNodeDrawData drawData, bool show = true )
		{
			var pNode = mDistanceTextNodes[i];
			if( pNode != null )
			{
				bool visible = show && !mCondition[Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene];
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
		}

		//	Delgates and Hooks
		private delegate void NameplateDrawFuncDelegate( AddonNamePlate* pThis );
		private static NameplateDrawFuncDelegate mdNameplateDraw = new NameplateDrawFuncDelegate( NameplateDrawDetour );
		private static IntPtr mfpOnNameplateDraw = IntPtr.Zero;
		private static Hook<NameplateDrawFuncDelegate> mNameplateDrawHook;

		//	Members
		internal static readonly DistanceInfo[] mNameplateDistanceInfoArray = new DistanceInfo[AddonNamePlate.NumNamePlateObjects];    //***** TODO: Expose the data properly. *****
		private static ClientState mClientState;
		private static Condition mCondition;
		private static AddonNamePlate* mpNameplateAddon = null;
		private static readonly AtkTextNode*[] mDistanceTextNodes = new AtkTextNode*[AddonNamePlate.NumNamePlateObjects];

		private static readonly uint mNameplateDistanceNodeIDBase = 0x6C78C400;    //YOLO hoping for no collisions.
	}
}
