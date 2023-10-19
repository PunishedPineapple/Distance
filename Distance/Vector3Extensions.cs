using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Distance;

internal static class Vector3Extensions
{
	internal static float Length_XZ( this Vector3 vec )
	{
		return (float)Math.Sqrt( vec.X * vec.X + vec.Z * vec.Z );
	}
}
