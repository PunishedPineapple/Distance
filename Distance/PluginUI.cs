using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Globalization;
using System.IO;

using ImGuiNET;
using ImGuiScene;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Plugin;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using CheapLoc;


namespace ReadyCheckHelper
{
	// It is good to have this be disposable in general, in case you ever need it
	// to do any cleanup
	public class PluginUI : IDisposable
	{
		//	Construction
		public PluginUI( Plugin plugin, DalamudPluginInterface pluginInterface, Configuration configuration, DataManager dataManager, GameGui gameGui, SigScanner sigScanner )
		{
			mPlugin = plugin;
			mPluginInterface = pluginInterface;
			mConfiguration = configuration;
			mDataManager = dataManager;
			mGameGui = gameGui;
		}

		//	Destruction
		unsafe public void Dispose()
		{
			UpdateDistanceTextNode( "", false );
			//	We should probably be properly removing the nodes, but by checking for a node with the right id before constructing one, we should only ever leak a single node, which is probably fine.
			mpDistanceTextNode = null;
			mpAggroDistanceTextNode = null;
		}

		public void Initialize()
		{
			//Load Skeleton sheet?  Put in own class probably
		}

		public void Draw()
		{
			//	Draw the sub-windows.
			DrawSettingsWindow();
			DrawDataWindow();

			//	Draw other UI stuff.
			DrawOnGameUI();
		}

		protected void DrawSettingsWindow()
		{
			if( !SettingsWindowVisible )
			{
				return;
			}

			if( ImGui.Begin( Loc.Localize( "Window Title: Config", "Ready Check Helper Settings" ) + "###Ready Check Helper Settings", ref mSettingsWindowVisible,
				ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Aggro Distance", "Show the remaining distance from the enemy before they will detect you." ) + "###Show aggro distance.", ref mConfiguration.mShowAggroDistance );
				ImGuiHelpMarker( Loc.Localize( "Help: Show Aggro Distance", "This distance will only be shown when it is known, and only on major bosses.  Additionally, it will only be shown until you enter combat." ) );
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + "###Show distance units.", ref mConfiguration.mShowUnitsOnDistances );
				ImGui.Checkbox( Loc.Localize( "Config Option: Distance is to Ring", "Show distance to target ring, not target center." ) + "###Distance is to ring.", ref mConfiguration.mDistanceIsToRing );
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Mode Indicator", "Show the distance mode indicator." ) + "###Show distance type marker.", ref mConfiguration.mShowDistanceModeMarker ); 
				ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distances:" ) );
				ImGui.SliderInt( "##DistancePrecisionSlider", ref mConfiguration.mDecimalPrecision, 0, 3 );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Distance Widget Filters", "Distance Widget Filters" ) + "###Distance Widget Filters." ) )
				{
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Players", "Show the distance to players." ) + "###Show distance to players.", ref mConfiguration.mShowDistanceOnPlayers );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on BattleNpc", "Show the distance to combatant NPCs." ) + "###Show distance to BattleNpc.", ref mConfiguration.mShowDistanceOnBattleNpc );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventNpc", "Show the distance to non-combatant NPCs." ) + "###Show distance to EventNpc.", ref mConfiguration.mShowDistanceOnEventNpc );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Treasure", "Show the distance to treasure chests." ) + "###Show distance to treasure.", ref mConfiguration.mShowDistanceOnTreasure );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Aetheryte", "Show the distance to aetherytes." ) + "###Show distance to aetheryte.", ref mConfiguration.mShowDistanceOnAetheryte );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Gathering Node", "Show the distance to gathering nodes." ) + "###Show distance to gathering node.", ref mConfiguration.mShowDistanceOnGatheringNode );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventObj", "Show the distance to interactable objects." ) + "###Show distance to EventObj.", ref mConfiguration.mShowDistanceOnEventObj );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Companion", "Show the distance to companions." ) + "###Show distance to companion.", ref mConfiguration.mShowDistanceOnCompanion );
					ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Housing", "Show the distance to housing items." ) + "###Show distance to housing.", ref mConfiguration.mShowDistanceOnHousing );
				}

				if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Distance Widget Appearance", "Distance Widget Appearance" ) + "###Distance Widget Appearance." ) )
				{
					ImGui.Text( Loc.Localize( "Config Option: Distance Text Position", "Position of the distance readout (x,y):" ) );
					ImGui.DragFloat2( "###DistanceTextPositionSlider", ref mConfiguration.mDistanceTextPosition, 1f, 0f, Math.Max( ImGuiHelpers.MainViewport.Size.X, ImGuiHelpers.MainViewport.Size.Y ), "%g" );
					ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Size", "Distance text font size" ) );
					ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Use Heavy Font", "Use heavy font for distance text" ) + "###Distance font heavy.", ref mConfiguration.mDistanceFontHeavy );
					ImGui.SliderInt( "##DistanceTextFontSizeSlider", ref mConfiguration.mDistanceFontSize, 6, 36 );
					ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color", "Distance text color" ) + "###DistanceTextColorPicker", ref mConfiguration.mDistanceTextColor, ImGuiColorEditFlags.NoInputs );
					ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color", "Distance text glow color" ) + "###DistanceTextEdgeColorPicker", ref mConfiguration.mDistanceTextEdgeColor, ImGuiColorEditFlags.NoInputs );
				}

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				if( ImGui.Button( Loc.Localize( "Button: Save and Close", "Save and Close" ) + "###Save and Close" ) )
				{
					mConfiguration.Save();
					SettingsWindowVisible = false;
				}
			}

			ImGui.End();
		}

		protected void DrawDataWindow()
		{
			if( !DataWindowVisible )
			{
				return;
			}

			//	Draw the window.
			ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
			if( ImGui.Begin( Loc.Localize( "Window Title: Distance Data", "Distance Data" ) + "###Distance Data", ref mDataWindowVisible ) )
			{
				ImGui.Text( $"Draw Distance: {mPlugin.CurrentDistanceDrawInfo.ShowDistance}" );
				ImGui.Text( $"Draw Aggro Distance: {mPlugin.CurrentDistanceDrawInfo.ShowAggroDistance}" );
				if( mPlugin.CurrentDistanceInfo != null )
				{
					ImGui.Text( $"Target Kind: {mPlugin.CurrentDistanceInfo.TargetKind}" );
					ImGui.Text( $"Player: ({mPlugin.CurrentDistanceInfo.Position.X}, {mPlugin.CurrentDistanceInfo.Position.Y}, {mPlugin.CurrentDistanceInfo.Position.Z})" );
					ImGui.Text( $"Target: ({mPlugin.CurrentDistanceInfo.TargetPosition.X}, {mPlugin.CurrentDistanceInfo.TargetPosition.Y}, {mPlugin.CurrentDistanceInfo.TargetPosition.Z})" );
					ImGui.Text( $"Distance (y): {mPlugin.CurrentDistanceInfo.DistanceFromTarget_Yalms}" );
					ImGui.Text( $"Distance from Ring (y): {mPlugin.CurrentDistanceInfo.DistanceFromTargetRing_Yalms}" );
					ImGui.Text( $"Distance from Aggro (y): {mPlugin.CurrentDistanceInfo.DistanceFromTargetAggro_Yalms}" );
				}
				else
				{
					ImGui.Text( "Distance data is null!" );
				}
			}

			//	We're done.
			ImGui.End();
		}

		protected void DrawOnGameUI()
		{

			/*string text = "";
			string unitString = mConfiguration.ShowUnitsOnDistances ? "y" : "";
			if( mPlugin.CurrentDistanceDrawInfo.TargetName != null )
			{
				text += mPlugin.CurrentDistanceDrawInfo.TargetName.ToString();
			}

			if( mPlugin.CurrentDistanceDrawInfo.ShowDistance )
			{
				float distance = mConfiguration.DistanceIsToRing ? mPlugin.CurrentDistanceInfo.DistanceFromTargetRing_Yalms : mPlugin.CurrentDistanceInfo.DistanceFromTarget_Yalms;
				text += $" ({Math.Max( 0, distance ).ToString( $"F{mConfiguration.DecimalPrecision}" )}{unitString})";
			}

			if( mPlugin.CurrentDistanceDrawInfo.ShowAggroDistance )
			{
				text += $" (Aggro in {Math.Max( 0, mPlugin.CurrentDistanceInfo.DistanceFromTargetAggro_Yalms ).ToString( $"F{mConfiguration.DecimalPrecision}" )}{unitString})";
			}

			if( mPlugin.CurrentDistanceDrawInfo.ShowDistance || mPlugin.CurrentDistanceDrawInfo.ShowAggroDistance )
			{
				unsafe
				{
					var pTargetBar = (AtkUnitBase*)mGameGui.GetAddonByName( "_TargetInfo", 1 );
					if( (IntPtr)pTargetBar != IntPtr.Zero )
					{
						var pTargetNameNode = pTargetBar->GetTextNodeById( 16 );
						if( (IntPtr)pTargetNameNode != IntPtr.Zero )
						{
							pTargetNameNode->SetText( text );
						}
					}
				}
			}*/

			string str = "";
			if( mPlugin.CurrentDistanceInfo != null )
			{
				float distance = mConfiguration.DistanceIsToRing ? mPlugin.CurrentDistanceInfo.DistanceFromTargetRing_Yalms : mPlugin.CurrentDistanceInfo.DistanceFromTarget_Yalms;
				distance = Math.Max( 0, distance );
				string unitString = mConfiguration.ShowUnitsOnDistances ? "y" : "";
				string distanceTypeSymbol = "";
				if( mConfiguration.ShowDistanceModeMarker ) distanceTypeSymbol = mConfiguration.DistanceIsToRing ? "◯ " : "· ";
				str = $"{distanceTypeSymbol}{distance.ToString( $"F{mConfiguration.DecimalPrecision}" )}{unitString}";
			}

			UpdateDistanceTextNode( str, mPlugin.CurrentDistanceDrawInfo.ShowDistance );
		}

		//***** TODO: Think about making this function reusable for both text nodes. *****
		unsafe protected void UpdateDistanceTextNode( string str, bool show = true )
		{
			var pAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_ScreenText", 1 );
			if( pAddon != null )
			{
				//	If we don't have a pointer to our text node, first see if we can find it by ID.  Doing this allows us to not have to deal with freeing the node resources and removing connections to sibling nodes (we'll still leak, but only once).
				if( mpDistanceTextNode == null )
				{
					for( var i = 0; i < pAddon->UldManager.NodeListCount; i++ )
					{
						if( pAddon->UldManager.NodeList[i] == null ) continue;
						if( pAddon->UldManager.NodeList[i]->NodeID == mDistanceNodeID )
						{
							mpDistanceTextNode = (AtkTextNode*)pAddon->UldManager.NodeList[i];
							break;
						}
					}
				}

				//	If we have our node, set the colors, size, and text from settings.
				if( mpDistanceTextNode != null )
				{
					bool visible = show;// && mCondition.Cutscene();
					( (AtkResNode*)mpDistanceTextNode )->ToggleVisibility( visible );
					if( visible )
					{
						//***** TODO *****
						mpDistanceTextNode->AtkResNode.SetPositionShort( (short)mConfiguration.DistanceTextPosition.X, (short)mConfiguration.DistanceTextPosition.Y );

						mpDistanceTextNode->TextColor.A = (byte)( mConfiguration.mDistanceTextColor.W * 255f );
						mpDistanceTextNode->TextColor.R = (byte)( mConfiguration.mDistanceTextColor.X * 255f );
						mpDistanceTextNode->TextColor.G = (byte)( mConfiguration.mDistanceTextColor.Y * 255f );
						mpDistanceTextNode->TextColor.B = (byte)( mConfiguration.mDistanceTextColor.Z * 255f );

						mpDistanceTextNode->EdgeColor.A = (byte)( mConfiguration.mDistanceTextEdgeColor.W * 255f );
						mpDistanceTextNode->EdgeColor.R = (byte)( mConfiguration.mDistanceTextEdgeColor.X * 255f );
						mpDistanceTextNode->EdgeColor.G = (byte)( mConfiguration.mDistanceTextEdgeColor.Y * 255f );
						mpDistanceTextNode->EdgeColor.B = (byte)( mConfiguration.mDistanceTextEdgeColor.Z * 255f );

						mpDistanceTextNode->FontSize = (byte)mConfiguration.DistanceFontSize;
						mpDistanceTextNode->AlignmentFontType = (byte)( (int)AlignmentType.BottomRight | ( mConfiguration.mDistanceFontHeavy ? 0x10 : 0 ) );
						mpDistanceTextNode->LineSpacing = 24;
						mpDistanceTextNode->CharSpacing = 1;

						mpDistanceTextNode->SetText( str );
					}
				}
				//	Set up the node if it hasn't been.
				else if( pAddon->RootNode != null )
				{
					var pNewTextNode = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTextNode), 8);
					if( pNewTextNode != null )
					{
						mpDistanceTextNode = pNewTextNode;
						IMemorySpace.Memset( mpDistanceTextNode, 0, (ulong)sizeof( AtkTextNode ) );
						mpDistanceTextNode->Ctor();

						mpDistanceTextNode->AtkResNode.Type = NodeType.Text;
						mpDistanceTextNode->AtkResNode.Flags = (short)( NodeFlags.AnchorLeft | NodeFlags.AnchorTop );
						mpDistanceTextNode->AtkResNode.DrawFlags = 0;
						//mpDistanceTextNode->AtkResNode.SetPositionShort( 1, 1 );
						mpDistanceTextNode->AtkResNode.SetWidth( 200 );
						mpDistanceTextNode->AtkResNode.SetHeight( 14 );

						//***** TODO: Right-align text *****
						mpDistanceTextNode->LineSpacing = 24;
						mpDistanceTextNode->AlignmentFontType = (byte)AlignmentType.BottomRight;
						mpDistanceTextNode->FontSize = (byte)mConfiguration.DistanceFontSize;
						mpDistanceTextNode->TextFlags = (byte)( TextFlags.Edge );
						mpDistanceTextNode->TextFlags2 = 0;

						mpDistanceTextNode->AtkResNode.NodeID = mDistanceNodeID;

						mpDistanceTextNode->AtkResNode.Color.A = 0xFF;
						mpDistanceTextNode->AtkResNode.Color.R = 0xFF;
						mpDistanceTextNode->AtkResNode.Color.G = 0xFF;
						mpDistanceTextNode->AtkResNode.Color.B = 0xFF;

						var lastNode = pAddon->RootNode;
						if( lastNode->ChildNode != null )
						{
							lastNode = lastNode->ChildNode;
							while( lastNode->PrevSiblingNode != null )
							{
								lastNode = lastNode->PrevSiblingNode;
							}

							mpDistanceTextNode->AtkResNode.NextSiblingNode = lastNode;
							mpDistanceTextNode->AtkResNode.ParentNode = pAddon->RootNode;
							lastNode->PrevSiblingNode = (AtkResNode*)mpDistanceTextNode;
						}
						else
						{
							lastNode->ChildNode = (AtkResNode*)mpDistanceTextNode;
							mpDistanceTextNode->AtkResNode.ParentNode = lastNode;
						}

						/*mpDistanceTextNode->TextColor.A = 0xFF;
						mpDistanceTextNode->TextColor.R = 0xFF;
						mpDistanceTextNode->TextColor.G = 0xFF;
						mpDistanceTextNode->TextColor.B = 0xFF;

						mpDistanceTextNode->EdgeColor.A = 0xFF;
						mpDistanceTextNode->EdgeColor.R = 0xF0;
						mpDistanceTextNode->EdgeColor.G = 0x8E;
						mpDistanceTextNode->EdgeColor.B = 0x37;*/

						pAddon->UldManager.UpdateDrawNodeList();
					}
				}
			}
		}

		protected void ImGuiHelpMarker( string description, bool sameLine = true, string marker = "(?)" )
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

		protected Plugin mPlugin;
		protected DalamudPluginInterface mPluginInterface;
		protected Configuration mConfiguration;
		protected DataManager mDataManager;
		protected GameGui mGameGui;

		//	Need a real backing field on the following properties for use with ImGui.
		protected bool mSettingsWindowVisible = false;
		public bool SettingsWindowVisible
		{
			get { return mSettingsWindowVisible; }
			set { mSettingsWindowVisible = value; }
		}

		protected bool mDataWindowVisible = false;
		public bool DataWindowVisible
		{
			get { return mDataWindowVisible; }
			set { mDataWindowVisible = value; }
		}

		protected static readonly uint mDistanceNodeID = 0x6C78B300;	//YOLO hoping for no collisions.
		protected static readonly uint mAggroDistanceNodeID = mDistanceNodeID + 1;
		unsafe protected AtkTextNode* mpDistanceTextNode = null;
		unsafe protected AtkTextNode* mpAggroDistanceTextNode = null;
	}
}