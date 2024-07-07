using FFXIVClientStructs.FFXIV.Client.Game.Group;

namespace Distance;

internal static unsafe class PartyUtils
{
	internal static bool ObjectIsPartyMember( uint objectID )
	{
		if( objectID is 0 or 0xE0000000 ) return false;
		if( Service.PartyList.Length < 1 ) return false;
		foreach( var member in Service.PartyList ) if( member?.ObjectId == objectID ) return true;
		return false;
	}

	internal static bool ObjectIsAllianceMember( uint objectID )
	{
		if( objectID is 0 or 0xE0000000 ) return false;
		if( GroupManager.Instance() == null ) return false;
		//if( !GroupManager.Instance()->IsAlliance ) return false;	//***** TODO: IsAlliance always returns false; why?
		if( GroupManager.Instance()->MainGroup.IsEntityIdInParty( objectID ) ) return false;
		return GroupManager.Instance()->MainGroup.IsEntityIdInAlliance( objectID );
	}
}
