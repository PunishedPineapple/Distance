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
}
