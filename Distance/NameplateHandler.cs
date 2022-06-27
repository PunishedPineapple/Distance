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
							mShouldDrawDistanceInfoArray[pObjectInfo->NamePlateIndex] = pObject->ObjectID != mClientState?.LocalPlayer.ObjectId;
						}
					}
				}
			}

			mDistanceUpdateTimer.Stop();
			mDistanceUpdateTime_uSec = mDistanceUpdateTimer.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;
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

			TESTING_UpdateNameplateDistanceNodes();
			mDrawHookTimer.Stop();
			mDrawHookTime_uSec = mDrawHookTimer.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;

			mNameplateDrawHook.Original( pThis );
		}

		public static void TESTING_UpdateNameplateDistanceNodes()
		{
			for( int i = 0; i < mNameplateDistanceInfoArray.Length; ++i )
			{
				if( mNameplateDistanceInfoArray[i].IsValid )	//	If it's not valid, nameplate should be hiding itself and thus our node.  Double check this.
				{
					TextNodeDrawData drawData = GetNameplateNodeDrawData( i ) ?? new TextNodeDrawData()
					{
						PositionX = (short)35,
						PositionY = (short)76,
						UseDepth = true,
						TextColorA = (byte)255,
						TextColorR = (byte)255,
						TextColorG = (byte)255,
						TextColorB = (byte)255,
						EdgeColorA = (byte)255,
						EdgeColorR = (byte)255,
						EdgeColorG = (byte)255,
						EdgeColorB = (byte)255,
						FontSize = (byte)12,
						AlignmentFontType = (byte)( 8 | 0 ),
						LineSpacing = 24,
						CharSpacing = 1
					};

					drawData.PositionX = (short)35;
					drawData.PositionY = (short)65;
					drawData.UseDepth = !ObjectIsNonDepthTarget( mNameplateDistanceInfoArray[i].ObjectID );
					drawData.FontSize = (byte)18;
					drawData.AlignmentFontType = (byte)( 8 | 0 );
					drawData.LineSpacing = 24;
					drawData.CharSpacing = 1;

					UpdateNameplateDistanceTextNode( i, $"{mNameplateDistanceInfoArray[i].DistanceFromTargetRing_Yalms:F1}y", drawData, mShouldDrawDistanceInfoArray[i] );
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

		public static TextNodeDrawData? GetNameplateNodeDrawData( int i )
		{
			var nameplateObject = GetNameplateObject( i );
			if( nameplateObject == null ) return null;

			var pTargetNameTextNode = nameplateObject.Value.NameText;
			if( pTargetNameTextNode != null  )
			{
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
		private static AddonNamePlate* mpNameplateAddon = null;
		private static readonly AtkTextNode*[] mDistanceTextNodes = new AtkTextNode*[AddonNamePlate.NumNamePlateObjects];

		private static readonly Stopwatch mDrawHookTimer = new();
		private static readonly Stopwatch mDistanceUpdateTimer = new();
		internal static Int64 mDrawHookTime_uSec = 0;
		internal static Int64 mDistanceUpdateTime_uSec = 0;

		private static readonly uint mNameplateDistanceNodeIDBase = 0x6C78C400;    //YOLO hoping for no collisions.
	}
}
