using System;

namespace Distance;
internal static class GameObjectUtils
{
	//***** TODO: Get this data from a more reliable place (i.e., character flags or something).
	internal static unsafe bool ObjectIsAggressive( UInt32 objectID )
	{
		if( objectID is 0 or 0xE0000000 ) return false;

		if( FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance() != null &&
			FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule() != null &&
			FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetRaptureAtkModule() != null )
		{
			var atkArrayDataHolder = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
			if( atkArrayDataHolder.NumberArrayCount >= 22 )
			{
				var pEnmityListArray = atkArrayDataHolder.NumberArrays[21];
				int enemyCount = pEnmityListArray->AtkArrayData.Size > 1 ? pEnmityListArray->IntArray[1] : 0;

				for( int i = 0; i < enemyCount; ++i )
				{
					int index = 8 + i * 6;
					if( index >= pEnmityListArray->AtkArrayData.Size ) return false;
					if( pEnmityListArray->IntArray[index] == objectID ) return true;
				}
			}
		}

		return false;
	}
}
