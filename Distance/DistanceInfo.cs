using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Distance
{
	public class DistanceInfo
	{
		public Vector3 Position { get; set; }
		public Vector3 TargetPosition { get; set; }
		public float TargetRadius_Yalms { get; set; }
		public float TargetScale_Yalms { get; set; }
		public float AggroRange_Yalms { get; set; } = 1f;

		public float DistanceFromTarget_Yalms
		{
			get
			{
				return Vector2.Distance( new Vector2( Position.X, Position.Z ), new Vector2( TargetPosition.X, TargetPosition.Z ) );
			}
			private set
			{
			}
		}

		public float DistanceFromTargetRing_Yalms
		{
			get
			{
				return DistanceFromTarget_Yalms - TargetRadius_Yalms;
			}
			private set
			{
			}
		}

		public float DistanceFromTargetAggro_Yalms
		{
			get
			{
				return DistanceFromTarget_Yalms - AggroRange_Yalms;
			}
			private set
			{
			}
		}
	}
}
