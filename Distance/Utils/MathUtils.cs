using System;
using System.Numerics;

namespace Distance;

internal static class MathUtils
{
	internal static void Wrap<T>( ref T value, T min, T max ) where T : INumber<T>
	{
		if( min == max ) throw new ArgumentException( $"Invalid Argument(s): min must not be equal to max ({min} and {max} were passed, respectively)." );
		if( min > max ) Swap( ref min, ref max );
		value = value % ( max - min ) + min;
	}

	internal static T Wrap<T>( T value, T min, T max ) where T : INumber<T>
	{
		Wrap( ref value, min, max );
		return value;
	}

	internal static T LinearInterpolation<T>( T x1, T y1, T x2, T y2, T xLookup ) where T : INumber<T>
	{
		if( xLookup <= x1 ) return y1;
		else if( xLookup >= x2 ) return y2;
		else
		{
			T xNorm = ( xLookup - x1 ) / ( x2 - x1 );
			T result = xNorm * ( y2 - y1 ) + y1;
			return result;
		}
	}

	internal static float LinearInterpolation( Vector2 p1, Vector2 p2, float xLookup )
	{
		return LinearInterpolation( p1.X, p1.Y, p2.X, p2.Y, xLookup );
	}

	internal static void Swap<T>( ref T val1, ref T val2  )
	{
		T temp = val1;
		val1 = val2;
		val2 = temp;
	}
}
