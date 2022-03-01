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
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using CheapLoc;


namespace Distance
{
	// It is good to have this be disposable in general, in case you ever need it
	// to do any cleanup
	public class PluginUI : IDisposable
	{
		//	Construction
		public PluginUI( Plugin plugin, DalamudPluginInterface pluginInterface, Configuration configuration, DataManager dataManager, GameGui gameGui, SigScanner sigScanner, ClientState clientState, Dalamud.Game.ClientState.Conditions.Condition condition )
		{
			mPlugin = plugin;
			mPluginInterface = pluginInterface;
			mConfiguration = configuration;
			mDataManager = dataManager;
			mGameGui = gameGui;
			mClientState = clientState;
			mCondition = condition;
		}

		//	Destruction
		unsafe public void Dispose()
		{
			UpdateDistanceTextNode( false );
			UpdateAggroDistanceTextNode( false );
			//	We should probably be properly removing the nodes, but by checking for a node with the right id before constructing one, we should only ever leak a single node, which is probably fine.
		}

		public void Initialize()
		{
			//Load Skeleton sheet?  Put in own class probably
		}

		public void Draw()
		{
			//	Draw the sub-windows.
			DrawSettingsWindow();
			DrawDebugWindow();
			DrawDebugAggroEntitiesWindow();

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

		protected void DrawDebugWindow()
		{
			if( !DebugWindowVisible )
			{
				return;
			}

			//	Draw the window.
			ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
			if( ImGui.Begin( Loc.Localize( "Window Title: Distance Data", "Distance Data" ) + "###Distance Data", ref mDebugWindowVisible ) )
			{
				ImGui.Checkbox( "Show known aggro range data", ref mDebugAggroEntitiesWindowVisible );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( $"Draw Distance: {mPlugin.CurrentDistanceDrawInfo.ShowDistance}" );
				ImGui.Text( $"Draw Aggro Distance: {mPlugin.CurrentDistanceDrawInfo.ShowAggroDistance}" );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				if( mPlugin.CurrentDistanceInfo != null )
				{
					ImGui.Text( $"Target Kind: {mPlugin.CurrentDistanceInfo.TargetKind}" );
					ImGui.Text( $"BNpcName ID: {mPlugin.CurrentDistanceInfo.BNpcNameID}" );
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
				if( mPlugin.CurrentDistanceInfo != null )
				{
					float? aggroDistance = BNpcAggroInfo.GetAggroRange( mPlugin.CurrentDistanceInfo.BNpcNameID, mClientState.TerritoryType );
					if( aggroDistance!= null )
					{
						ImGui.Text( $"Aggro Distance: {aggroDistance.Value}" );
					}
					else
					{
						ImGui.Text( "No aggro distance data found." );
					}
				}
			}

			//	We're done.
			ImGui.End();
		}

		protected void DrawDebugAggroEntitiesWindow()
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
				var entries = BNpcAggroInfo.GetAllAggroEntities();
				if( entries.Count > 0 )
				{
					if( ImGui.BeginTable( "###KnownAggroEntitiesTable", 4 ) )
					{
						ImGui.TableHeadersRow();
						ImGui.TableSetColumnIndex( 0 );
						ImGui.Text( "TerritoryType" );
						ImGui.TableSetColumnIndex( 1 );
						ImGui.Text( "BNpcName ID" );
						ImGui.TableSetColumnIndex( 2 );
						ImGui.Text( "Aggro Distance (y)" );
						ImGui.TableSetColumnIndex( 3 );
						ImGui.Text( "BNpcName Text" );

						foreach( var entry in entries )
						{
							ImGui.TableNextRow();
							ImGui.TableSetColumnIndex( 0 );
							ImGui.Text( $"{entry.TerritoryType}" );
							ImGui.TableSetColumnIndex( 1 );
							ImGui.Text( $"{entry.NameID}" );
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

		protected void DrawOnGameUI()
		{
			UpdateDistanceTextNode( mPlugin.CurrentDistanceDrawInfo.ShowDistance );
			UpdateAggroDistanceTextNode( mPlugin.CurrentDistanceDrawInfo.ShowAggroDistance );
		}

		unsafe protected void UpdateDistanceTextNode( bool show )
		{
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

			TextNodeDrawData drawData = new TextNodeDrawData()
			{
				PositionX = (short)mConfiguration.DistanceTextPosition.X,
				PositionY = (short)mConfiguration.DistanceTextPosition.Y,
				TextColorA = (byte)( mConfiguration.mDistanceTextColor.W * 255f ),
				TextColorR = (byte)( mConfiguration.mDistanceTextColor.X * 255f ),
				TextColorG = (byte)( mConfiguration.mDistanceTextColor.Y * 255f ),
				TextColorB = (byte)( mConfiguration.mDistanceTextColor.Z * 255f ),
				EdgeColorA = (byte)( mConfiguration.mDistanceTextEdgeColor.W * 255f ),
				EdgeColorR = (byte)( mConfiguration.mDistanceTextEdgeColor.X * 255f ),
				EdgeColorG = (byte)( mConfiguration.mDistanceTextEdgeColor.Y * 255f ),
				EdgeColorB = (byte)( mConfiguration.mDistanceTextEdgeColor.Z * 255f ),
				FontSize = (byte)mConfiguration.DistanceFontSize,
				AlignmentFontType = (byte)( (int)AlignmentType.BottomRight | ( mConfiguration.mDistanceFontHeavy ? 0x10 : 0 ) ),
				LineSpacing = 24,
				CharSpacing = 1
			};

			UpdateTextNode( mDistanceNodeID, str, drawData, show );
		}

		unsafe protected void UpdateAggroDistanceTextNode( bool show = true )
		{
			string str = "";
			if( mPlugin.CurrentDistanceInfo != null )
			{
				float distance = Math.Max( 0, mPlugin.CurrentDistanceInfo.DistanceFromTargetAggro_Yalms );
				string unitString = mConfiguration.ShowUnitsOnDistances ? "y" : "";
				str = $"Aggro in {distance.ToString( $"F{mConfiguration.DecimalPrecision}" )}{unitString}";
			}

			TextNodeDrawData drawData = new TextNodeDrawData()
			{
				PositionX = (short)mConfiguration.DistanceTextPosition.X,
				PositionY = (short)(mConfiguration.DistanceTextPosition.Y + TextNodeDrawData.Default.LineSpacing ),
				TextColorA = (byte)( mConfiguration.mDistanceTextColor.W * 255f ),
				TextColorR = (byte)( mConfiguration.mDistanceTextColor.X * 255f ),
				TextColorG = (byte)( mConfiguration.mDistanceTextColor.Y * 255f ),
				TextColorB = (byte)( mConfiguration.mDistanceTextColor.Z * 255f ),
				EdgeColorA = (byte)( mConfiguration.mDistanceTextEdgeColor.W * 255f ),
				EdgeColorR = (byte)( mConfiguration.mDistanceTextEdgeColor.X * 255f ),
				EdgeColorG = (byte)( mConfiguration.mDistanceTextEdgeColor.Y * 255f ),
				EdgeColorB = (byte)( mConfiguration.mDistanceTextEdgeColor.Z * 255f ),
				FontSize = (byte)mConfiguration.DistanceFontSize,
				AlignmentFontType = (byte)( (int)AlignmentType.BottomRight | ( mConfiguration.mDistanceFontHeavy ? 0x10 : 0 ) ),
				LineSpacing = 24,
				CharSpacing = 1
			};

			UpdateTextNode( mAggroDistanceNodeID, str, drawData, show );
		}

		unsafe protected void UpdateTextNode( uint nodeID, string str, TextNodeDrawData drawData, bool show = true )
		{
			AtkTextNode* pNode = null;
			var pAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_ScreenText", 1 );
			if( pAddon != null )
			{
				//	Find our node by ID.  Doing this allows us to not have to deal with freeing the node resources and removing connections to sibling nodes (we'll still leak, but only once).
				for( var i = 0; i < pAddon->UldManager.NodeListCount; i++ )
				{
					if( pAddon->UldManager.NodeList[i] == null ) continue;
					if( pAddon->UldManager.NodeList[i]->NodeID == nodeID )
					{
						pNode = (AtkTextNode*)pAddon->UldManager.NodeList[i];
						break;
					}
				}

				//	If we have our node, set the colors, size, and text from settings.
				if( pNode != null )
				{
					bool visible =  show && !mCondition[Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene];
					( (AtkResNode*)pNode )->ToggleVisibility( visible );
					if( visible )
					{
						pNode->AtkResNode.SetPositionShort( drawData.PositionX, drawData.PositionY );

						pNode->TextColor.A = drawData.TextColorA;
						pNode->TextColor.R = drawData.TextColorR;
						pNode->TextColor.G = drawData.TextColorG;
						pNode->TextColor.B = drawData.TextColorB;

						pNode->EdgeColor.A = drawData.EdgeColorA;
						pNode->EdgeColor.R = drawData.EdgeColorR;
						pNode->EdgeColor.G = drawData.EdgeColorG;
						pNode->EdgeColor.B = drawData.EdgeColorB;

						pNode->FontSize = drawData.FontSize;
						pNode->AlignmentFontType = drawData.AlignmentFontType;
						pNode->LineSpacing = drawData.LineSpacing;
						pNode->CharSpacing = drawData.CharSpacing;

						pNode->SetText( str );
					}
				}
				//	Set up the node if it hasn't been.
				else if( pAddon->RootNode != null )
				{
					var pNewTextNode = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTextNode), 8);
					if( pNewTextNode != null )
					{
						pNode = pNewTextNode;
						IMemorySpace.Memset( pNode, 0, (ulong)sizeof( AtkTextNode ) );
						pNode->Ctor();

						pNode->AtkResNode.Type = NodeType.Text;
						pNode->AtkResNode.Flags = (short)( NodeFlags.AnchorLeft | NodeFlags.AnchorTop );
						pNode->AtkResNode.DrawFlags = 0;
						pNode->AtkResNode.SetPositionShort( drawData.PositionX, drawData.PositionY );
						pNode->AtkResNode.SetWidth( 200 );
						pNode->AtkResNode.SetHeight( 14 );

						pNode->LineSpacing = drawData.LineSpacing;
						pNode->CharSpacing = drawData.CharSpacing;
						pNode->AlignmentFontType = drawData.AlignmentFontType;
						pNode->FontSize = drawData.FontSize;
						pNode->TextFlags = (byte)( TextFlags.Edge );
						pNode->TextFlags2 = 0;

						pNode->AtkResNode.NodeID = nodeID;

						pNode->AtkResNode.Color.A = 0xFF;
						pNode->AtkResNode.Color.R = 0xFF;
						pNode->AtkResNode.Color.G = 0xFF;
						pNode->AtkResNode.Color.B = 0xFF;

						var lastNode = pAddon->RootNode;
						if( lastNode->ChildNode != null )
						{
							lastNode = lastNode->ChildNode;
							while( lastNode->PrevSiblingNode != null )
							{
								lastNode = lastNode->PrevSiblingNode;
							}

							pNode->AtkResNode.NextSiblingNode = lastNode;
							pNode->AtkResNode.ParentNode = pAddon->RootNode;
							lastNode->PrevSiblingNode = (AtkResNode*)pNode;
						}
						else
						{
							lastNode->ChildNode = (AtkResNode*)pNode;
							pNode->AtkResNode.ParentNode = lastNode;
						}

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
		protected Dalamud.Game.ClientState.Conditions.Condition mCondition;
		protected ClientState mClientState;

		//	Need a real backing field on the following properties for use with ImGui.
		protected bool mSettingsWindowVisible = false;
		public bool SettingsWindowVisible
		{
			get { return mSettingsWindowVisible; }
			set { mSettingsWindowVisible = value; }
		}

		protected bool mDebugWindowVisible = false;
		public bool DebugWindowVisible
		{
			get { return mDebugWindowVisible; }
			set { mDebugWindowVisible = value; }
		}

		protected bool mDebugAggroEntitiesWindowVisible = false;
		public bool DebugAggroEntitiesWindowVisible
		{
			get { return mDebugAggroEntitiesWindowVisible; }
			set { mDebugAggroEntitiesWindowVisible = value; }
		}

		protected static readonly uint mDistanceNodeID = 0x6C78B300;	//YOLO hoping for no collisions.
		protected static readonly uint mAggroDistanceNodeID = mDistanceNodeID + 1;
	}
}