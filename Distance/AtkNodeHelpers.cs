using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Distance
{
	internal static class AtkNodeHelpers
	{
		public static unsafe AtkTextNode* GetTextNodeByID( AtkUnitBase* pAddon, uint nodeID )
		{
			if( pAddon == null ) return null;
			for( var i = 0; i < pAddon->UldManager.NodeListCount; ++i )
			{
				if( pAddon->UldManager.NodeList[i] == null ) continue;
				if( pAddon->UldManager.NodeList[i]->NodeID == nodeID )
				{
					return (AtkTextNode*)pAddon->UldManager.NodeList[i];
				}
			}
			return null;
		}

		public static unsafe AtkTextNode* CreateNewTextNode( AtkUnitBase* pAddon, uint nodeID )
		{
			if( pAddon == null ) return null;

			var pNode = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc( (ulong)sizeof( AtkTextNode ), 8 );

			if( pNode != null )
			{
				IMemorySpace.Memset( pNode, 0, (ulong)sizeof( AtkTextNode ) );
				pNode->Ctor();

				pNode->AtkResNode.Type = NodeType.Text;
				pNode->AtkResNode.Flags = (short)( NodeFlags.AnchorLeft | NodeFlags.AnchorTop );
				pNode->AtkResNode.DrawFlags = 0;
				pNode->AtkResNode.SetPositionShort( 0, 0 );
				pNode->AtkResNode.SetWidth( 200 );
				pNode->AtkResNode.SetHeight( 14 );

				pNode->LineSpacing = 24;
				pNode->CharSpacing = 1;
				pNode->AlignmentFontType = (byte)AlignmentType.TopLeft;
				pNode->FontSize = 12;
				pNode->TextFlags = (byte)( TextFlags.Edge );
				pNode->TextFlags2 = 0;

				pNode->AtkResNode.NodeID = nodeID;

				pNode->AtkResNode.Color.A = 0xFF;
				pNode->AtkResNode.Color.R = 0xFF;
				pNode->AtkResNode.Color.G = 0xFF;
				pNode->AtkResNode.Color.B = 0xFF;

				var lastNode = pAddon->RootNode;
				if( lastNode->ChildNode != null )
				{
					lastNode = lastNode->ChildNode;
					while( lastNode->PrevSiblingNode != null )
					{
						lastNode = lastNode->PrevSiblingNode;
					}

					pNode->AtkResNode.NextSiblingNode = lastNode;
					pNode->AtkResNode.ParentNode = pAddon->RootNode;
					lastNode->PrevSiblingNode = (AtkResNode*)pNode;
				}
				else
				{
					lastNode->ChildNode = (AtkResNode*)pNode;
					pNode->AtkResNode.ParentNode = lastNode;
				}

				pAddon->UldManager.UpdateDrawNodeList();
			}

			return pNode;
		}

		public static unsafe void HideNode( AtkUnitBase* pAddon, uint nodeID )
		{
			var pNode = GetTextNodeByID( pAddon, nodeID );
			if( pNode != null ) ( (AtkResNode*)pNode )->ToggleVisibility( false );
		}
	}
}
