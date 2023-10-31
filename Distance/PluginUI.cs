using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

using CheapLoc;

using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;

namespace Distance;

public sealed class PluginUI : IDisposable
{
	public PluginUI( Plugin plugin, DalamudPluginInterface pluginInterface, Configuration configuration )
	{
		mPlugin = plugin;
		mPluginInterface = pluginInterface;
		mConfiguration = configuration;

		AggroArcsUI = new( plugin, this, configuration );
		CustomWidgetsUI = new( plugin, this, configuration );
		CustomArcsUI = new( plugin, this, configuration );
		GeneralSettingsUI = new( plugin, this, configuration );
		NameplatesUI = new( plugin, this, configuration );
	}

	public unsafe void Dispose()
	{
		//	This is just to make sure that no nodes get left visible after we stop managing them.  We should probably be properly removing and freeing the
		//	nodes, but by checking for a node with the right id before constructing one, we should only ever have a one-time leak per node, which is probably fine.
		for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; i++ )
		{
			HideTextNode( mDistanceNodeIDBase + (uint)i );
		}

		HideTextNode( mAggroDistanceNodeID );

		AggroArcsUI.Dispose();
		CustomWidgetsUI.Dispose();
		CustomArcsUI.Dispose();
		GeneralSettingsUI.Dispose();
		NameplatesUI.Dispose();
	}

	public void Initialize()
	{
	}

	public void Draw()
	{
		//	Draw the sub-windows.
		DrawSettingsWindow();
		DrawDebugWindow();
		DrawDebugAggroEntitiesWindow();
		DrawDebugNameplateInfoWindow();

		//	Draw other UI stuff.
		mOverlayDrawTimer.Restart();
		DrawOverlay();
		mOverlayDrawTimer.Stop();
		mOverlayDrawTime_uSec = mOverlayDrawTimer.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;

		mWidgetNodeUpdateTimer.Restart();
		DrawOnGameUI();
		mWidgetNodeUpdateTimer.Stop();
		mWidgetNodeUpdateTime_uSec = mWidgetNodeUpdateTimer.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;
	}

	private void DrawSettingsWindow()
	{
		if( !SettingsWindowVisible )
		{
			return;
		}

		ImGui.SetNextWindowSizeConstraints( new Vector2( 800f, 500f ) * ImGui.GetIO().FontGlobalScale, new( float.MaxValue ) );
		if( ImGui.Begin( Loc.Localize( "Window Title: Config", "Distance Settings" ) + "###Distance Settings", ref mSettingsWindowVisible,
			ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse ) )
		{
			if( ImGui.BeginTabBar( "###DistanceSettingsTabBar" ) )
			{
				if( ImGui.BeginTabItem( Loc.Localize( "Settings Tab: General", "General Settings" ) + "###GeneralSettingsTab" ) )
				{
					GeneralSettingsUI.DrawSettingsTab();
					ImGui.EndTabItem();
				}
				if( ImGui.BeginTabItem( Loc.Localize( "Settings Tab: Custom Widgets", "Custom Widgets" ) + "###CustomWidgetsSettingsTab" ) )
				{
					CustomWidgetsUI.DrawSettingsTab();
					ImGui.EndTabItem();
				}
				if( ImGui.BeginTabItem( Loc.Localize( "Settings Tab: Custom Arcs", "Custom Arcs" ) + "###CustomArcsSettingsTab" ) )
				{
					CustomArcsUI.DrawSettingsTab();
					ImGui.EndTabItem();
				}
				ImGui.EndTabBar();
			}

			if( ImGui.Button( Loc.Localize( "Button: Save", "Save" ) + "###Save Button" ) )
			{
				mConfiguration.Save();
			}
			ImGui.SameLine();
			if( ImGui.Button( Loc.Localize( "Button: Save and Close", "Save and Close" ) + "###Save and Close Button" ) )
			{
				mConfiguration.Save();
				SettingsWindowVisible = false;
			}
			float linkTextWidth = ImGui.CalcTextSize( Loc.Localize( "Config Text: Distance Information Link", "About Distance Measurement in FFXIV" ) ).X;
			ImGui.PushFont( UiBuilder.IconFont );
			linkTextWidth += ImGui.CalcTextSize( "\uF0C1" ).X;
			ImGui.PopFont();
			linkTextWidth += ImGui.GetStyle().ItemSpacing.X;
			ImGui.SameLine( ImGui.GetContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - linkTextWidth + ImGui.GetStyle().WindowPadding.X );
			try
			{
				ImGui.PushStyleColor( ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Button] );
				ImGui.PushFont( UiBuilder.IconFont );
				ImGui.Text( "\uF0C1" );
				ImGui.PopFont();
				ImGui.PopStyleColor();
				ImGui.SameLine();
				ImGuiUtils.URLLink( "https://github.com/PunishedPineapple/Distance/wiki/Distances-in-FFXIV", Loc.Localize( "Config Text: Distance Information Link", "About Distance Measurement in FFXIV" ), false, UiBuilder.IconFont );
			}
			catch( Exception e )
			{
				Service.PluginLog.Warning( $"Unable to open the requested link:\r\n{e}" );
			}
		}

		ImGui.End();
	}

	internal static bool IsUsableAggroDistanceFormatString( string str )
	{
		if( str.Where( x => { return x == '{'; } ).Count() != 1 || str.Where( x => { return x == '}'; } ).Count() != 1 ) return false;
		var openBraceIndex = str.IndexOf( '{' );
		var closeBraceIndex = str.IndexOf( '}' );
		if( closeBraceIndex - openBraceIndex != 2 ) return false;
		if( str[closeBraceIndex - 1] != '0' ) return false;
		return true;
	}

	private unsafe void DrawDebugWindow()
	{
		if( !DebugWindowVisible )
		{
			return;
		}

		//	Draw the window.
		ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
		ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
		if( ImGui.Begin( Loc.Localize( "Window Title: Debug Data", "Debug Data" ) + "###Debug Data", ref mDebugWindowVisible ) )
		{
			if( ImGui.Button( "Export Localizable Strings" ) )
			{
				string pwd = Directory.GetCurrentDirectory();
				Directory.SetCurrentDirectory( mPluginInterface.AssemblyLocation.DirectoryName );
				Loc.ExportLocalizable();
				Directory.SetCurrentDirectory( pwd );
			}
			ImGui.Checkbox( "Show nameplate data", ref mDebugNameplateInfoWindowVisible );
			ImGui.Checkbox( "Show known aggro range data", ref mDebugAggroEntitiesWindowVisible );

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.Text( "Nameplate Text Flags:" );
			ImGui.Checkbox( "Auto Adjust Size", ref DEBUG_TextFlags_AutoAdjustNodeSize );
			ImGui.Checkbox( "Bold", ref DEBUG_TextFlags_Bold );
			ImGui.Checkbox( "Italic", ref DEBUG_TextFlags_Italic );
			ImGui.Checkbox( "Edge", ref DEBUG_TextFlags_Edge );
			ImGui.Checkbox( "Glare", ref DEBUG_TextFlags_Glare );
			ImGui.Checkbox( "Emboss", ref DEBUG_TextFlags_Emboss );
			ImGui.Checkbox( "Word Wrap", ref DEBUG_TextFlags_WordWrap );
			ImGui.Checkbox( "Multi-line", ref DEBUG_TextFlags_MultiLine );

			ImGui.Text( "Nameplate Text Flags 2:" );
			ImGui.Checkbox( "Unknown 1", ref DEBUG_TextFlags2_Unknown1 );
			ImGui.Checkbox( "Unknown 2", ref DEBUG_TextFlags2_Unknown2 );
			ImGui.Checkbox( "Ellipsis", ref DEBUG_TextFlags2_Ellipsis );
			ImGui.Checkbox( "Unknown 8", ref DEBUG_TextFlags2_Unknown8 );
			ImGui.Checkbox( "Unknown 16 (New Nameplates)", ref DEBUG_TextFlags2_Unknown16 );
			ImGui.Checkbox( "Unknown 32", ref DEBUG_TextFlags2_Unknown32 );
			ImGui.Checkbox( "Unknown 64 (New Nameplates)", ref DEBUG_TextFlags2_Unknown64 );
			ImGui.Checkbox( "Unknown 128", ref DEBUG_TextFlags2_Unknown128 );

			if( ImGui.Button( "Set Nameplate Text Flags" ) ) NameplateHandler.DEBUG_mSetTextFlags = true;

			NameplateHandler.DEBUG_mNameplateTextFlags = 0;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_AutoAdjustNodeSize ? 1 : 0 ) << 0;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Bold ? 1 : 0 ) << 1;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Italic ? 1 : 0 ) << 2;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Edge ? 1 : 0 ) << 3;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Glare ? 1 : 0 ) << 4;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_Emboss ? 1 : 0 ) << 5;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_WordWrap ? 1 : 0 ) << 6;
			NameplateHandler.DEBUG_mNameplateTextFlags += ( DEBUG_TextFlags_MultiLine ? 1 : 0 ) << 7;

			NameplateHandler.DEBUG_mNameplateTextFlags2 = 0;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown1 ? 1 : 0 ) << 0;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown2 ? 1 : 0 ) << 1;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Ellipsis ? 1 : 0 ) << 2;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown8 ? 1 : 0 ) << 3;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown16 ? 1 : 0 ) << 4;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown32 ? 1 : 0 ) << 5;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown64 ? 1 : 0 ) << 6;
			NameplateHandler.DEBUG_mNameplateTextFlags2 += ( DEBUG_TextFlags2_Unknown128 ? 1 : 0 ) << 7;

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.Text( "Performance Timers:" );
			ImGui.Text( $"Nameplate Distance Update Time: {NameplateHandler.DistanceUpdateTime_uSec}μs" );
			ImGui.Text( $"Nameplate Node Configuration Time: {NameplateHandler.NodeUpdateTime_uSec}μs" );
			ImGui.Text( $"Widget Node Configuration Time: {mWidgetNodeUpdateTime_uSec}μs" );
			ImGui.Text( $"Overlay Draw Time: {mOverlayDrawTime_uSec}μs" );

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.Text( "Addresses:" );
			ImGui.Text( $"Nameplate Addon: 0x{Service.GameGui.GetAddonByName( "NamePlate", 1 ):X}" );
			ImGui.Text( $"Nameplate Addon (Cached): 0x{NameplateHandler.DEBUG_CachedNameplateAddonPtr:X}" );

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.Text( $"TerritoryType: {Service.ClientState.TerritoryType}" );

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			foreach( TargetType entry in Enum.GetValues( typeof( TargetType ) ) )
			{
				ImGui.Text( $"{entry.GetTranslatedName()} Distance Data:" );
				ImGui.Indent();
				ImGui.Text( mPlugin.GetDistanceInfo( entry ).ToString() );
				ImGui.Unindent();
			}
		}

		//	We're done.
		ImGui.End();
	}

	private void DrawDebugAggroEntitiesWindow()
	{
		if( !DebugAggroEntitiesWindowVisible )
		{
			return;
		}

		//	Draw the window.
		ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
		ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
		if( ImGui.Begin( Loc.Localize( "Window Title: Debug Aggro Entities", "Debug: Known Aggro Distances" ) + "###Debug: Known Aggro Distances", ref mDebugAggroEntitiesWindowVisible ) )
		{
			ImGui.Text( $"Current aggro data version: {BNpcAggroInfo.GetCurrentFileVersionAsString()}" );

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			var entries = BNpcAggroInfo.GetAllAggroEntities();
			if( entries.Count > 0 )
			{
				if( ImGui.BeginTable( "###KnownAggroEntitiesTable", 4 ) )
				{
					ImGui.TableHeadersRow();
					ImGui.TableSetColumnIndex( 0 );
					ImGui.Text( "TerritoryType" );
					ImGui.TableSetColumnIndex( 1 );
					ImGui.Text( "BNpc ID" );
					ImGui.TableSetColumnIndex( 2 );
					ImGui.Text( "Aggro Distance (y)" );
					ImGui.TableSetColumnIndex( 3 );
					ImGui.Text( "BNpc Name" );

					foreach( var entry in entries )
					{
						ImGui.TableNextRow();
						ImGui.TableSetColumnIndex( 0 );
						ImGui.Text( $"{entry.TerritoryType}" );
						ImGui.TableSetColumnIndex( 1 );
						ImGui.Text( $"{entry.BNpcID}" );
						ImGui.TableSetColumnIndex( 2 );
						ImGui.Text( $"{entry.AggroDistance_Yalms}" );
						ImGui.TableSetColumnIndex( 3 );
						ImGui.Text( $"{entry.EnglishName}" );
					}
					ImGui.EndTable();
				}
			}
			else
			{
				ImGui.Text( "Aggro entities list is empty." );
			}
		}
	}

	private void DrawDebugNameplateInfoWindow()
	{
		if( !DebugNameplateInfoWindowVisible )
		{
			return;
		}

		//	Draw the window.
		ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
		ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
		if( ImGui.Begin( Loc.Localize( "Window Title: Nameplate Info", "Debug: Nameplate Distance Info" ) + "###Debug: Nameplate Info Window", ref mDebugNameplateInfoWindowVisible ) )
		{
			for( int i = 0; i < NameplateHandler.DEBUG_NameplateDistanceInfo.Length; ++i )
			{
				ImGui.Text( $"{i}:" );
				ImGui.Text( NameplateHandler.DEBUG_NameplateDistanceInfo[i].ToString() );
				ImGui.Text( $"Should draw?: {NameplateHandler.DEBUG_ShouldDrawDistanceInfo[i]}" );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
			}
		}
	}

	private void DrawOverlay()
	{
		if( ShouldHideUIOverlays() ) return;

		ImGuiHelpers.ForceNextWindowMainViewport();
		ImGui.SetNextWindowPos( ImGui.GetMainViewport().Pos );
		ImGui.SetNextWindowSize( ImGui.GetMainViewport().Size );
		if( ImGui.Begin( "###DistanceArcsOverlayWindow", ImGuiUtils.OverlayWindowFlags ) )
		{
			AggroArcsUI.DrawOnOverlay();
			CustomArcsUI.DrawOnOverlay();
		}

		ImGui.End();
	}

	private void DrawOnGameUI()
	{
		//	Draw the aggro widget.
		UpdateAggroDistanceTextNode( mPlugin.GetDistanceInfo( mConfiguration.AggroDistanceApplicableTargetType ), mConfiguration.ShowAggroDistance && mPlugin.ShouldDrawAggroDistanceInfo() );

		//	Draw each configured distance widget.
		for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; ++i )
		{
			UpdateDistanceTextNode( (uint)i,
									mPlugin.GetDistanceInfo( mConfiguration.DistanceWidgetConfigs[i].ApplicableTargetType ),
									mConfiguration.DistanceWidgetConfigs[i],
									mPlugin.ShouldDrawDistanceInfo( mConfiguration.DistanceWidgetConfigs[i] ) );
		}

		//	Note: Nameplate drawing is handled in NameplateHandler.
	}

	private unsafe void UpdateDistanceTextNode( uint distanceWidgetNumber, DistanceInfo distanceInfo, DistanceWidgetConfig config, bool show )
	{
		if( !show ) HideTextNode( mDistanceNodeIDBase + distanceWidgetNumber );

		string str = "";
		byte nodeAlphaToUse = 255;
		Vector4 textColorToUse = config.TextColor;
		Vector4 edgeColorToUse = config.TextEdgeColor;

		if( distanceInfo.IsValid )
		{
			float distance = config.DistanceIsToRing ? distanceInfo.DistanceFromTargetRing_Yalms : distanceInfo.DistanceFromTarget_Yalms;
			distance -= config.DistanceOffset_Yalms;
			float displayDistance = config.AllowNegativeDistances ? distance : Math.Max( 0, distance );
			string unitString = config.ShowUnits ? "y" : "";
			string distanceTypeSymbol = "";
			if( config.ShowDistanceModeMarker ) distanceTypeSymbol = config.DistanceIsToRing ? "◯ " : "· ";
			str = $"{distanceTypeSymbol}{displayDistance.ToString( $"F{config.DecimalPrecision}" )}{unitString}";

			if( config.UseDistanceBasedColor )
			{
				if( distance > config.FarThresholdDistance_Yalms )
				{
					textColorToUse = config.FarThresholdTextColor;
					edgeColorToUse = config.FarThresholdTextEdgeColor;
				}
				else if( distance > config.NearThresholdDistance_Yalms )
				{
					textColorToUse = config.NearThresholdTextColor;
					edgeColorToUse = config.NearThresholdTextEdgeColor;
				}
			}
		}

		if( config.TrackTargetBarTextColor )
		{
			AtkUnitBase* pTargetAddonToUse = null;
			UInt16 targetBarNameNodeIndex = 0;
			var pTargetAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfo", 1 );
			var pTargetAddonSplit = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
			var pFocusTargetAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_FocusTargetInfo", 1 );
			if( config.ApplicableTargetType == TargetType.FocusTarget && pFocusTargetAddon != null && pFocusTargetAddon->IsVisible )
			{
				pTargetAddonToUse = pFocusTargetAddon;
				targetBarNameNodeIndex = FocusTargetBarColorNodeIndex;
			}
			else if( config.ApplicableTargetType == TargetType.TargetOfTarget && pTargetAddon != null && pTargetAddon->IsVisible )
			{
				pTargetAddonToUse = pTargetAddon;
				targetBarNameNodeIndex = TargetOfTargetBarColorNodeIndex;
			}
			else if( config.ApplicableTargetType == TargetType.TargetOfTarget && pTargetAddonSplit != null && pTargetAddonSplit->IsVisible )
			{
				pTargetAddonToUse = pTargetAddonSplit;
				targetBarNameNodeIndex = SplitTargetOfTargetBarColorNodeIndex;
			}
			else if( pTargetAddon != null && pTargetAddon->IsVisible )
			{
				pTargetAddonToUse = pTargetAddon;
				targetBarNameNodeIndex = TargetBarColorNodeIndex;
			}
			else if( pTargetAddonSplit != null && pTargetAddonSplit->IsVisible )
			{
				pTargetAddonToUse = pTargetAddonSplit;
				targetBarNameNodeIndex = SplitTargetBarColorNodeIndex;
			}

			if( pTargetAddonToUse != null )
			{
				var pTargetNameNode = pTargetAddonToUse->UldManager.NodeListSize > targetBarNameNodeIndex ? pTargetAddonToUse->UldManager.NodeList[targetBarNameNodeIndex] : null;
				if( pTargetNameNode != null && pTargetNameNode->GetAsAtkTextNode() != null )
				{
					var pTargetNameTextNode = pTargetNameNode->GetAsAtkTextNode();

					nodeAlphaToUse = pTargetNameTextNode->AtkResNode.Color.A;

					textColorToUse.W = (float)pTargetNameTextNode->TextColor.A / 255f;
					textColorToUse.X = (float)pTargetNameTextNode->TextColor.R / 255f;
					textColorToUse.Y = (float)pTargetNameTextNode->TextColor.G / 255f;
					textColorToUse.Z = (float)pTargetNameTextNode->TextColor.B / 255f;

					edgeColorToUse.W = (float)pTargetNameTextNode->EdgeColor.A / 255f;
					edgeColorToUse.X = (float)pTargetNameTextNode->EdgeColor.R / 255f;
					edgeColorToUse.Y = (float)pTargetNameTextNode->EdgeColor.G / 255f;
					edgeColorToUse.Z = (float)pTargetNameTextNode->EdgeColor.B / 255f;
				}
			}
		}

		GameAddonEnum addonToUse = config.UIAttachType.GetGameAddonToUse( config.ApplicableTargetType );

		Vector2 mouseoverOffset = Vector2.Zero;
		if( addonToUse == GameAddonEnum.ScreenText && (
			config.UIAttachType == AddonAttachType.Cursor ||
			( config.UIAttachType == AddonAttachType.Auto && ( config.ApplicableTargetType is TargetType.MouseOverTarget or TargetType.UIMouseOverTarget or TargetType.MouseOver_And_UIMouseOver_Target ) ) ) )
		{
			mouseoverOffset = ImGui.GetMousePos();
		};

		TextNodeDrawData drawData = new()
		{
			PositionX = (short)( config.TextPosition.X + mouseoverOffset.X ),
			PositionY = (short)( config.TextPosition.Y + mouseoverOffset.Y ),
			Alpha = nodeAlphaToUse,
			TextColorA = (byte)( textColorToUse.W * 255f ),
			TextColorR = (byte)( textColorToUse.X * 255f ),
			TextColorG = (byte)( textColorToUse.Y * 255f ),
			TextColorB = (byte)( textColorToUse.Z * 255f ),
			EdgeColorA = (byte)( edgeColorToUse.W * 255f ),
			EdgeColorR = (byte)( edgeColorToUse.X * 255f ),
			EdgeColorG = (byte)( edgeColorToUse.Y * 255f ),
			EdgeColorB = (byte)( edgeColorToUse.Z * 255f ),
			FontSize = (byte)config.FontSize,
			Alignment = config.FontAlignment,
			Font = config.FontHeavy ? FontType.MiedingerMed : FontType.Axis,
			LineSpacing = 24,
			CharSpacing = 1
		};

		UpdateTextNode( addonToUse, mDistanceNodeIDBase + distanceWidgetNumber, str, drawData, show );
	}

	private unsafe void UpdateAggroDistanceTextNode( DistanceInfo distanceInfo, bool show )
	{
		if( !show ) HideTextNode( mAggroDistanceNodeID );

		string str = "";
		Vector4 color = mConfiguration.AggroDistanceTextColor;
		Vector4 edgeColor = mConfiguration.AggroDistanceTextEdgeColor;
		if( distanceInfo.IsValid )
		{
			float distance = Math.Max( 0, distanceInfo.DistanceFromTargetAggro_Yalms );
			string unitString = mConfiguration.ShowUnitsOnAggroDistance ? "y" : "";
			str = $"Aggro in {distance.ToString( $"F{mConfiguration.AggroDistanceDecimalPrecision}" )}{unitString}";

			if( distance < mConfiguration.AggroWarningDistance_Yalms )
			{
				color = mConfiguration.AggroDistanceWarningTextColor;
				edgeColor = mConfiguration.AggroDistanceWarningTextEdgeColor;
			}
			else if( distance < mConfiguration.AggroCautionDistance_Yalms )
			{
				color = mConfiguration.AggroDistanceCautionTextColor;
				edgeColor = mConfiguration.AggroDistanceCautionTextEdgeColor;
			}
		}

		GameAddonEnum addonToUse = mConfiguration.AggroDistanceUIAttachType.GetGameAddonToUse( mConfiguration.AggroDistanceApplicableTargetType );

		Vector2 mouseoverOffset = Vector2.Zero;
		if( addonToUse == GameAddonEnum.ScreenText && (
			mConfiguration.AggroDistanceUIAttachType == AddonAttachType.Cursor ||
			( mConfiguration.AggroDistanceUIAttachType == AddonAttachType.Auto && ( mConfiguration.AggroDistanceApplicableTargetType is TargetType.MouseOverTarget or TargetType.UIMouseOverTarget or TargetType.MouseOver_And_UIMouseOver_Target ) ) ) )
		{
			mouseoverOffset = ImGui.GetMousePos();
		};

		TextNodeDrawData drawData = new()
		{
			PositionX = (short)( mConfiguration.AggroDistanceTextPosition.X + mouseoverOffset.X ),
			PositionY = (short)( mConfiguration.AggroDistanceTextPosition.Y + mouseoverOffset.Y ),
			Alpha = 255,	//***** TODO: Should probably grab this off of the focus target addon when this node is attached to focus target bar, since that can fade out a bit.
			TextColorA = (byte)( color.W * 255f ),
			TextColorR = (byte)( color.X * 255f ),
			TextColorG = (byte)( color.Y * 255f ),
			TextColorB = (byte)( color.Z * 255f ),
			EdgeColorA = (byte)( edgeColor.W * 255f ),
			EdgeColorR = (byte)( edgeColor.X * 255f ),
			EdgeColorG = (byte)( edgeColor.Y * 255f ),
			EdgeColorB = (byte)( edgeColor.Z * 255f ),
			FontSize = (byte)mConfiguration.AggroDistanceFontSize,
			Alignment = mConfiguration.AggroDistanceFontAlignment,
			Font = mConfiguration.AggroDistanceFontHeavy ? FontType.MiedingerMed : FontType.Axis,
			LineSpacing = 24,
			CharSpacing = 1
		};

		UpdateTextNode( addonToUse, mAggroDistanceNodeID, str, drawData, show );
	}

	private unsafe void UpdateTextNode( GameAddonEnum addonToUse, uint nodeID, string str, TextNodeDrawData drawData, bool show = true )
	{
		AtkTextNode* pNode = null;
		AtkUnitBase* pAddon = null;

		var pNormalTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfo", 1 );
		var pSplitTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
		var pFocusTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_FocusTargetInfo", 1 );
		var pScreenTextAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_ScreenText", 1 );
		if( addonToUse == GameAddonEnum.TargetBar )
		{
			pAddon = pSplitTargetBarAddon == null || !pSplitTargetBarAddon->IsVisible ? pNormalTargetBarAddon : pSplitTargetBarAddon;
		}
		else if( addonToUse == GameAddonEnum.FocusTargetBar )
		{
			pAddon = pFocusTargetBarAddon;
		}
		else
		{
			pAddon = pScreenTextAddon;
		}

		if( pAddon != null )
		{
			//	Find our node by ID.  Doing this allows us to not have to deal with freeing the node resources and removing connections to sibling nodes (we'll still leak, but only once).
			pNode = AtkNodeHelpers.GetTextNodeByID( pAddon, nodeID );

			//	If we have our node, set the colors, size, and text from settings.
			if( pNode != null )
			{
				bool visible = show && !ShouldHideUIOverlays();
				( (AtkResNode*)pNode )->ToggleVisibility( visible );
				if( visible )
				{
					pNode->AtkResNode.SetPositionShort( drawData.PositionX, drawData.PositionY );

					pNode->AtkResNode.Color.A = drawData.Alpha;

					pNode->TextColor.A = drawData.TextColorA;
					pNode->TextColor.R = drawData.TextColorR;
					pNode->TextColor.G = drawData.TextColorG;
					pNode->TextColor.B = drawData.TextColorB;

					pNode->EdgeColor.A = drawData.EdgeColorA;
					pNode->EdgeColor.R = drawData.EdgeColorR;
					pNode->EdgeColor.G = drawData.EdgeColorG;
					pNode->EdgeColor.B = drawData.EdgeColorB;

					pNode->FontSize = drawData.FontSize;
					pNode->AlignmentType = drawData.Alignment;
					pNode->FontType = drawData.Font;
					pNode->LineSpacing = drawData.LineSpacing;
					pNode->CharSpacing = drawData.CharSpacing;

					pNode->SetText( str );
				}
			}
			//	Set up the node if it hasn't been.
			else if( pAddon->RootNode != null )
			{
				pNode = AtkNodeHelpers.CreateNewTextNode( pAddon, nodeID );
			}
		}

		//	Hide the node(s) with the same ID on any of the other addons (in case we switched addon for the node recently).
		if( pAddon != pScreenTextAddon ) AtkNodeHelpers.HideNode( pScreenTextAddon, nodeID );
		if( pAddon != pFocusTargetBarAddon ) AtkNodeHelpers.HideNode( pFocusTargetBarAddon, nodeID );
		if( pAddon != pNormalTargetBarAddon ) AtkNodeHelpers.HideNode( pNormalTargetBarAddon, nodeID );
		if( pAddon != pSplitTargetBarAddon ) AtkNodeHelpers.HideNode( pSplitTargetBarAddon, nodeID );
	}

	internal static unsafe void HideTextNode( uint nodeID )
	{
		//	Get the possible addons we could be using.
		var pNormalTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfo", 1 );
		var pSplitTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
		var pFocusTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_FocusTargetInfo", 1 );
		var pScreenTextAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_ScreenText", 1 );

		//	Hide the node(s) with the specified ID in any of those addons.
		if( pScreenTextAddon != null ) AtkNodeHelpers.HideNode( pScreenTextAddon, nodeID );
		if( pFocusTargetBarAddon != null ) AtkNodeHelpers.HideNode( pFocusTargetBarAddon, nodeID );
		if( pNormalTargetBarAddon != null ) AtkNodeHelpers.HideNode( pNormalTargetBarAddon, nodeID );
		if( pSplitTargetBarAddon != null ) AtkNodeHelpers.HideNode( pSplitTargetBarAddon, nodeID );
	}

	private unsafe bool AddonIsVisible( GameAddonEnum addon )
	{
		switch( addon )
		{
			case GameAddonEnum.TargetBar:
				var pNormalTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfo", 1 );
				var pSplitTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
				return ( pNormalTargetBarAddon != null && pNormalTargetBarAddon->IsVisible ) || ( pSplitTargetBarAddon != null && pSplitTargetBarAddon->IsVisible );
			case GameAddonEnum.FocusTargetBar:
				var pFocusTargetBarAddon = (AtkUnitBase*)Service.GameGui.GetAddonByName( "_FocusTargetInfo", 1 );
				return pFocusTargetBarAddon != null && pFocusTargetBarAddon->IsVisible;
			default:
				return false;
		}
	}

	private bool ShouldHideUIOverlays()
	{
		return	Service.GameGui.GameUiHidden ||
				Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInCutSceneEvent] ||
				Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene] ||
				Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.CreatingCharacter];
	}

	private readonly Plugin mPlugin;
	private readonly DalamudPluginInterface mPluginInterface;
	private readonly Configuration mConfiguration;

	internal readonly PluginUI_AggroArcs AggroArcsUI;
	internal readonly PluginUI_CustomWidgets CustomWidgetsUI;
	internal readonly PluginUI_CustomArcs CustomArcsUI;
	internal readonly PluginUI_GeneralSettings GeneralSettingsUI;
	internal readonly PluginUI_Nameplates NameplatesUI;

	//	Need a real backing field on the following properties for use with ImGui.
	private bool mSettingsWindowVisible = false;
	internal bool SettingsWindowVisible
	{
		get { return mSettingsWindowVisible; }
		set { mSettingsWindowVisible = value; }
	}

	private bool mDebugWindowVisible = false;
	internal bool DebugWindowVisible
	{
		get { return mDebugWindowVisible; }
		set { mDebugWindowVisible = value; }
	}

	private bool mDebugAggroEntitiesWindowVisible = false;
	internal bool DebugAggroEntitiesWindowVisible
	{
		get { return mDebugAggroEntitiesWindowVisible; }
		set { mDebugAggroEntitiesWindowVisible = value; }
	}

	private bool mDebugNameplateInfoWindowVisible = false;
	internal bool DebugNameplateInfoWindowVisible
	{
		get { return mDebugNameplateInfoWindowVisible; }
		set { mDebugNameplateInfoWindowVisible = value; }
	}

	private bool DEBUG_TextFlags_AutoAdjustNodeSize = false;
	private bool DEBUG_TextFlags_Bold = false;
	private bool DEBUG_TextFlags_Italic = false;
	private bool DEBUG_TextFlags_Edge = true;
	private bool DEBUG_TextFlags_Glare = true;
	private bool DEBUG_TextFlags_Emboss = false;
	private bool DEBUG_TextFlags_WordWrap = false;
	private bool DEBUG_TextFlags_MultiLine = false;

	private bool DEBUG_TextFlags2_Unknown1 = false;
	private bool DEBUG_TextFlags2_Unknown2 = false;
	private bool DEBUG_TextFlags2_Ellipsis = false;
	private bool DEBUG_TextFlags2_Unknown8 = false;
	private bool DEBUG_TextFlags2_Unknown16 = false;
	private bool DEBUG_TextFlags2_Unknown32 = false;
	private bool DEBUG_TextFlags2_Unknown64 = false;
	private bool DEBUG_TextFlags2_Unknown128 = false;

	//	Do this to control the order of dropdown items.
	internal static readonly TargetType[] TargetDropdownMenuItems =
	{
		TargetType.Target_And_Soft_Target,
		TargetType.FocusTarget,
		TargetType.MouseOver_And_UIMouseOver_Target,
		TargetType.Target,
		TargetType.SoftTarget,
		TargetType.MouseOverTarget,
		TargetType.UIMouseOverTarget,
		TargetType.TargetOfTarget,
	};

	internal static readonly string[] UIAttachDropdownOptions =
	{
		AddonAttachType.Auto.GetTranslatedName(),
		AddonAttachType.ScreenText.GetTranslatedName(),
		AddonAttachType.Target.GetTranslatedName(),
		AddonAttachType.FocusTarget.GetTranslatedName(),
		AddonAttachType.Cursor.GetTranslatedName(),
		//AddonAttachType.Nameplate.GetTranslatedUIAttachTypeEnumString(),
	};

	private const UInt16 FocusTargetBarColorNodeIndex = 10;
	private const UInt16 TargetBarColorNodeIndex = 39;
	private const UInt16 SplitTargetBarColorNodeIndex = 8;
	private const UInt16 TargetOfTargetBarColorNodeIndex = 49;
	private const UInt16 SplitTargetOfTargetBarColorNodeIndex = 12;

	private readonly Stopwatch mWidgetNodeUpdateTimer = new();
	private readonly Stopwatch mOverlayDrawTimer = new();
	private Int64 mWidgetNodeUpdateTime_uSec = 0;
	private Int64 mOverlayDrawTime_uSec = 0;

	//	Note: Node IDs only need to be unique within a given addon.
	internal const uint mDistanceNodeIDBase = 0x6C78B300;    //YOLO hoping for no collisions.
	internal const uint mAggroDistanceNodeID = mDistanceNodeIDBase - 1;
}