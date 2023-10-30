using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Distance;

internal static class StopwatchExtensions
{
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	internal static long ElapsedMicroseconds( this Stopwatch obj )
	{
		return obj.ElapsedTicks * 1_000_000L / Stopwatch.Frequency;
	}
}
