using System;
using System.Numerics;

namespace Distance;

internal struct DistanceInfo
{
	internal bool IsValid { get; set; }
	internal Dalamud.Game.ClientState.Objects.Enums.ObjectKind TargetKind { get; set; }
	internal UInt32 ObjectID { get; set; }
	internal IntPtr ObjectAddress { get; set; }	//	This is a kludge to enable 0xE0000000 object comparisons for nameplates.  Should compare by object ID whenever possible.
	internal UInt32 BNpcID { get; set; }
	internal Vector3 PlayerPosition { get; set; }
	internal Vector3 TargetPosition { get; set; }
	internal float TargetRadius_Yalms { get; set; }
	internal bool HasAggroRangeData { get; set; }
	internal float AggroRange_Yalms { get; set; }

	internal void Invalidate()
	{
		IsValid = false;
		TargetKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind.None;
		ObjectID = 0;
		ObjectAddress = IntPtr.Zero;
		BNpcID = 0;
		PlayerPosition = Vector3.Zero;
		TargetPosition = Vector3.Zero;
		TargetRadius_Yalms = 0;
		HasAggroRangeData = false;
		AggroRange_Yalms = 0;
	}

	internal float DistanceFromTarget_Yalms => PlayerPosition.DistanceTo_XZ( TargetPosition );
	internal float DistanceFromTargetRing_Yalms => DistanceFromTarget_Yalms - TargetRadius_Yalms;
	internal float EffectiveRangeFromTarget_Yalms => DistanceFromTarget_Yalms - TargetRadius_Yalms - PlayerHitRingRadius;
	internal float DistanceFromTargetAggro_Yalms => DistanceFromTargetRing_Yalms - AggroRange_Yalms;

	public override string ToString()
	{
		string str = "";

		str += $"Is Valid: {IsValid}\r\n";
		str += $"Target Kind: {TargetKind}\r\n";
		str += $"Object ID: {ObjectID:X8}\r\n";
		str += $"Object Address: 0x{ObjectAddress:X}\r\n";
		str += $"BNpc ID: {BNpcID}\r\n";
		str += $"Player Position: {PlayerPosition.X:F3}, {PlayerPosition.Y:F3}, {PlayerPosition.Z:F3}\r\n";
		str += $"Target Position: {TargetPosition.X:F3}, {TargetPosition.Y:F3}, {TargetPosition.Z:F3}\r\n";
		str += $"Aggro Range: {( HasAggroRangeData ? $"{AggroRange_Yalms:F3}" : "No Data" )}\r\n";
		str += $"Target Radius (y): {TargetRadius_Yalms:F3}\r\n";
		str += $"Distance To Target (y): {DistanceFromTarget_Yalms:F3}\r\n";
		str += $"Distance To Ring (y): {DistanceFromTargetRing_Yalms:F3}\r\n";
		str += $"Effective Targeted Range (y): {EffectiveRangeFromTarget_Yalms:F3}\r\n";
		str += $"Distance To Aggro (y): {( HasAggroRangeData ? $"{DistanceFromTargetAggro_Yalms:F3} " : "No Data" )}";
		
		return str;
	}

	internal const float PlayerHitRingRadius = 0.5f;
}
