using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Distance;

internal static class ArcUtils
{
	internal static void DrawArc_ScreenSpace( Vector3 targetPosition, Vector3 playerPosition, float radius_Yalms, float length, bool lengthIsYalms, bool drawPip, Vector4 arcColor, Vector4 arcEdgeColor )
	{
		double arcLength_Deg = lengthIsYalms ? length / radius_Yalms * 180f / Math.PI : length;

		float lineLength_Yalms = Vector2.Distance(new(targetPosition.X, targetPosition.Z), new(playerPosition.X, playerPosition.Z));
		float distance_Norm = lineLength_Yalms < 0.5f ? 0f : radius_Yalms / lineLength_Yalms;
		if( distance_Norm > 0 )
		{
			//	Get the point at the arc distance on the line between the player and the target.
			Vector3 worldCoords = new()
			{
				X = distance_Norm * (playerPosition.X - targetPosition.X) + targetPosition.X,
				Y = distance_Norm * (playerPosition.Y - targetPosition.Y) + targetPosition.Y,
				Z = distance_Norm * (playerPosition.Z - targetPosition.Z) + targetPosition.Z
			};

			var arcPoints = GetArcPoints(targetPosition, worldCoords, arcLength_Deg, worldCoords.Y);
			var arcScreenPoints = new Vector2[arcPoints.Length];

			bool isScreenPosValid = true;
			isScreenPosValid &= Service.GameGui.WorldToScreen( worldCoords, out Vector2 screenPos );
			for( int i = 0; i < arcPoints.Length; ++i )
			{
				isScreenPosValid &= Service.GameGui.WorldToScreen( arcPoints[i], out arcScreenPoints[i] );
			}

			uint color = ImGuiUtils.ColorVecToUInt(arcColor);
			uint edgeColor = ImGuiUtils.ColorVecToUInt(arcEdgeColor);

			if( drawPip ) ImGui.GetWindowDrawList().AddCircle( screenPos, 5.0f, edgeColor, 36, 5 );
			for( int i = 1; i < arcScreenPoints.Length; ++i )
			{
				ImGui.GetWindowDrawList().AddLine( arcScreenPoints[i - 1], arcScreenPoints[i], edgeColor, 5 );
			}

			if( drawPip ) ImGui.GetWindowDrawList().AddCircle( screenPos, 5.0f, color, 36, 3 );
			for( int i = 1; i < arcScreenPoints.Length; ++i )
			{
				ImGui.GetWindowDrawList().AddLine( arcScreenPoints[i - 1], arcScreenPoints[i], color, 3 );
			}
		}
	}

	internal static Vector3[] GetArcPoints( Vector3 center, Vector3 tangentPoint, double arcLength_Deg, float y, int numPoints = -1 )
	{
		//	Compute some points that are on an arc intersecting that point.
		Vector3 translatedTangentPoint = tangentPoint - center;
		float distance = new Vector2(translatedTangentPoint.X, translatedTangentPoint.Z).Length();
		double arcLength_Rad = arcLength_Deg * Math.PI / 180.0;
		double angle_Rad = Math.Atan2(translatedTangentPoint.Z, translatedTangentPoint.X);

		if( numPoints < 2 ) numPoints = Math.Max( (int)arcLength_Deg, 2 );
		Vector3[] arcPoints = new Vector3[numPoints];
		double angleStep_Rad = arcLength_Rad / ( numPoints - 1 );

		double angleOffset_Rad = -arcLength_Rad / 2.0;
		for( int i = 0; i < arcPoints.Length; ++i )
		{
			arcPoints[i].X = (float)Math.Cos( angle_Rad + angleOffset_Rad ) * distance + center.X;
			arcPoints[i].Z = (float)Math.Sin( angle_Rad + angleOffset_Rad ) * distance + center.Z;
			arcPoints[i].Y = y;
			angleOffset_Rad += angleStep_Rad;
		}

		return arcPoints;
	}
}
