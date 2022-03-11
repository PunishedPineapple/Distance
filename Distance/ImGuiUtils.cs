using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

using ImGuiNET;

namespace Distance
{
	internal static class ImGuiUtils
	{
		public static UInt32 ColorVecToUInt( Vector4 color )
		{
			return
			(uint)( color.X * 255f ) << 0 |
			(uint)( color.Y * 255f ) << 8 |
			(uint)( color.Z * 255f ) << 16 |
			(uint)( color.W * 255f ) << 24;
		}

		public static Vector4 ColorUIntToVec( UInt32 color )
		{
			return new Vector4()
			{
				X = (float)( color & 0xFF ) / 255f,
				Y = (float)( color & 0xFF00 ) / 255f,
				Z = (float)( color & 0xFF0000 ) / 255f,
				W = (float)( color & 0xFF000000 ) / 255f
			};
		}

		public static void HelpMarker( string description, bool sameLine = true, string marker = "(?)" )
		{
			if( sameLine ) ImGui.SameLine();
			ImGui.TextDisabled( marker );
			if( ImGui.IsItemHovered() )
			{
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos( ImGui.GetFontSize() * 35.0f );
				ImGui.TextUnformatted( description );
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}

		public const ImGuiWindowFlags OverlayWindowFlags =	ImGuiWindowFlags.NoDecoration |
															ImGuiWindowFlags.NoSavedSettings |
															ImGuiWindowFlags.NoMove |
															ImGuiWindowFlags.NoMouseInputs |
															ImGuiWindowFlags.NoFocusOnAppearing |
															ImGuiWindowFlags.NoBackground |
															ImGuiWindowFlags.NoNav;
	}
}
