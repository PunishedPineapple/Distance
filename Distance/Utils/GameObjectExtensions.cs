using System;

using Dalamud.Game.ClientState.Objects.Types;

namespace Distance;

internal static unsafe class GameObjectExtensions
{
	internal static bool IsPartyMember( this IGameObject obj )
	{
		if( obj == null ) return false;
		return PartyUtils.ObjectIsPartyMember( obj.EntityId );
	}

	internal static bool IsAllianceMember( this IGameObject obj )
	{
		if( obj == null ) return false;
		return PartyUtils.ObjectIsAllianceMember( obj.EntityId);
	}

	internal static bool IsAggressive( this IGameObject obj )
	{
		return GameObjectUtils.ObjectIsAggressive( obj.EntityId);
	}

	//***** TODO: It's sloppy and ugly to take in both an object ID and a pointer to the same object, but otherwise, we'd have to refactor how the distance data stores targets, and it's not clear that storing anything other than object ID is safe anyway for the lifetime we need.
	internal static bool IsSameObject( this IGameObject obj, UInt32 objectID, IntPtr pObject )
	{
		if( obj == null || obj.EntityId == 0 ) return false;
		if( obj.EntityId == 0xE0000000 ) return obj.Address == pObject;
		return obj.EntityId == objectID;
	}

	internal static bool IsSameObject( this IGameObject obj, IGameObject otherObj )
	{
		return obj.IsSameObject( otherObj.EntityId, otherObj.Address );
	}
}
