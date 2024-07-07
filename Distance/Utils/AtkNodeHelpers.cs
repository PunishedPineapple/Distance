using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Distance;

internal static unsafe class AtkNodeHelpers
{
	internal static AtkTextNode* GetTextNodeByID( AtkUnitBase* pAddon, uint nodeID )
	{
		if( pAddon == null ) return null;
		for( var i = 0; i < pAddon->UldManager.NodeListCount; ++i )
		{
			if( pAddon->UldManager.NodeList[i] == null ) continue;
			if( pAddon->UldManager.NodeList[i]->NodeId == nodeID )
			{
				return (AtkTextNode*)pAddon->UldManager.NodeList[i];
			}
		}
		return null;
	}

	internal static AtkTextNode* CreateNewTextNode( AtkUnitBase* pAddon, uint nodeID )
	{
		if( pAddon == null ) return null;
		var pNewNode = CreateOrphanTextNode( nodeID );
		if( pNewNode != null ) AttachTextNode( pAddon, pNewNode );
		return pNewNode;
	}

	internal static void AttachTextNode( AtkUnitBase* pAddon, AtkTextNode* pNode )
	{
		if( pAddon == null ) return;

		if( pNode != null )
		{
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
	}

	internal static AtkTextNode* CreateOrphanTextNode( uint nodeID, TextFlags textFlags = TextFlags.Edge, TextFlags2 textFlags2 = 0 )
	{
		//	Just use some sane defaults.
		var pNewNode = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTextNode), 8);
		if( pNewNode != null )
		{
			IMemorySpace.Memset( pNewNode, 0, (ulong)sizeof( AtkTextNode ) );
			pNewNode->Ctor();

			pNewNode->AtkResNode.Type = NodeType.Text;
			pNewNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
			pNewNode->AtkResNode.DrawFlags = 0;
			pNewNode->AtkResNode.SetPositionShort( 0, 0 );
			pNewNode->AtkResNode.SetWidth( DefaultTextNodeWidth );
			pNewNode->AtkResNode.SetHeight( DefaultTextNodeHeight );

			pNewNode->LineSpacing = 24;
			pNewNode->CharSpacing = 1;
			pNewNode->AlignmentFontType = (byte)AlignmentType.BottomLeft;
			pNewNode->FontSize = 12;
			pNewNode->TextFlags = (byte)textFlags;
			pNewNode->TextFlags2 = (byte)textFlags2;

			pNewNode->AtkResNode.NodeId = nodeID;

			pNewNode->AtkResNode.Color.A = 0xFF;
			pNewNode->AtkResNode.Color.R = 0xFF;
			pNewNode->AtkResNode.Color.G = 0xFF;
			pNewNode->AtkResNode.Color.B = 0xFF;
		}

		return pNewNode;
	}

	internal static void HideNode( AtkUnitBase* pAddon, uint nodeID )
	{
		var pNode = GetTextNodeByID( pAddon, nodeID );
		if( pNode != null ) ( (AtkResNode*)pNode )->ToggleVisibility( false );
	}


	internal const ushort DefaultTextNodeWidth = 200;
	internal const ushort DefaultTextNodeHeight = 14;
}
