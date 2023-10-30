using System;

using Dalamud.Game.ClientState.Objects.Types;

namespace Distance;

internal static unsafe class GameObjectExtensions
{
	internal static bool IsPartyMember( this GameObject obj )
	{
		if( obj == null ) return false;
		return PartyUtils.ObjectIsPartyMember( obj.ObjectId );
	}

	internal static bool IsAllianceMember( this GameObject obj )
	{
		if( obj == null ) return false;
		return PartyUtils.ObjectIsAllianceMember( obj.ObjectId );
	}

	internal static bool IsAggressive( this GameObject obj )
	{
		return GameObjectUtils.ObjectIsAggressive( obj.ObjectId );
	}

	//***** TODO: It's sloppy and ugly to take in both an object ID and a pointer to the same object, but otherwise, we'd have to refactor how the distance data stores targets, and it's not clear that storing anything other than object ID is safe anyway for the lifetime we need.
	internal static bool IsSameObject( this GameObject obj, UInt32 objectID, IntPtr pObject )
	{
		if( obj == null || obj.ObjectId == 0 ) return false;
		if( obj.ObjectId == 0xE0000000 ) return obj.Address == pObject;
		return obj.ObjectId == objectID;
	}

	internal static bool IsSameObject( this GameObject obj, GameObject otherObj )
	{
		return obj.IsSameObject( otherObj.ObjectId, otherObj.Address );
	}
}
