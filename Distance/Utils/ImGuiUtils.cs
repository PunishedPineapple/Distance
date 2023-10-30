using System;
using System.Diagnostics;
using System.Numerics;

using ImGuiNET;

namespace Distance;

internal static class ImGuiUtils
{
	internal static UInt32 ColorVecToUInt( Vector4 color )
	{
		return
		(uint)( color.X * 255f ) << 0 |
		(uint)( color.Y * 255f ) << 8 |
		(uint)( color.Z * 255f ) << 16 |
		(uint)( color.W * 255f ) << 24;
	}

	internal static Vector4 ColorUIntToVec( UInt32 color )
	{
		return new Vector4()
		{
			X = (float)( color & 0xFF ) / 255f,
			Y = (float)( color & 0xFF00 ) / 255f,
			Z = (float)( color & 0xFF0000 ) / 255f,
			W = (float)( color & 0xFF000000 ) / 255f
		};
	}

	internal static void DrawTextWithShadow( string text, Vector4 textColor, Vector4 shadowColor, byte shadowWidth, float scale )
	{
		Vector2 startPos = ImGui.GetCursorPos();
		ImGui.SetWindowFontScale( scale );
		ImGui.SetCursorPos( startPos + new Vector2( -shadowWidth, -shadowWidth ) );
		ImGui.TextColored( shadowColor, text );
		ImGui.SetCursorPos( startPos + new Vector2( -shadowWidth, shadowWidth ) );
		ImGui.TextColored( shadowColor, text );
		ImGui.SetCursorPos( startPos + new Vector2( shadowWidth, shadowWidth ) );
		ImGui.TextColored( shadowColor, text );
		ImGui.SetCursorPos( startPos + new Vector2( shadowWidth, -shadowWidth ) );
		ImGui.TextColored( shadowColor, text );
		ImGui.SetCursorPos( startPos );
		ImGui.TextColored( textColor, text );
		ImGui.SetWindowFontScale( 1 );
	}

	internal static void HelpMarker( string description, bool sameLine = true, string marker = "(?)" )
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

	internal static void URLLink( string URL, string textToShow = "", bool showTooltip = true, ImFontPtr? iconFont = null )
	{
		ImGui.PushStyleColor( ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] );
		ImGui.Text( textToShow.Length > 0 ? textToShow : URL );
		ImGui.PopStyleColor();

		if( ImGui.IsItemHovered() )
		{
			ImGui.SetMouseCursor( ImGuiMouseCursor.Hand );
			if( ImGui.IsMouseClicked( ImGuiMouseButton.Left ) )
			{
				Process.Start( new ProcessStartInfo( URL ) { UseShellExecute = true } );
			}

			AddUnderline( ImGui.GetStyle().Colors[(int)ImGuiCol.Text], 1.0f );

			if( showTooltip )
			{
				ImGui.BeginTooltip();
				if( iconFont != null )
				{
					ImGui.PushFont( iconFont.Value );
					ImGui.Text( "\uF0C1" );
					ImGui.PopFont();
					ImGui.SameLine();
				}
				ImGui.Text( URL );
				ImGui.EndTooltip();
			}
		}
		else
		{
			AddUnderline( ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled], 1.0f );
		}
	}

	internal static void AddUnderline( Vector4 color, float thickness )
	{
		Vector2 min = ImGui.GetItemRectMin();
		Vector2 max = ImGui.GetItemRectMax();
		min.Y = max.Y;
		ImGui.GetWindowDrawList().AddLine( min, max, ColorVecToUInt( color ), thickness );
	}

	internal static void AddOverline( Vector4 color, float thickness )
	{
		Vector2 min = ImGui.GetItemRectMin();
		Vector2 max = ImGui.GetItemRectMax();
		max.Y = min.Y;
		ImGui.GetWindowDrawList().AddLine( min, max, ColorVecToUInt( color ), thickness );
	}

	internal const ImGuiWindowFlags OverlayWindowFlags =    ImGuiWindowFlags.NoDecoration |
															ImGuiWindowFlags.NoSavedSettings |
															ImGuiWindowFlags.NoMove |
															ImGuiWindowFlags.NoMouseInputs |
															ImGuiWindowFlags.NoFocusOnAppearing |
															ImGuiWindowFlags.NoBackground |
															ImGuiWindowFlags.NoNav;
}
