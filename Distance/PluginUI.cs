using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

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
			//	This is just to make sure that no nodes get left visible after we stop managing them.  We should probably be properly removing and freeing the
			//	nodes, but by checking for a node with the right id before constructing one, we should only ever leak a single node, which is probably fine.
			for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; i++ )
			{
				UpdateDistanceTextNode( (uint)i, new(), new(), false );
			}

			UpdateAggroDistanceTextNode( new(), false );
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

			//	Draw other UI stuff.
			DrawOnGameUI();
		}

		protected void DrawSettingsWindow()
		{
			if( !SettingsWindowVisible )
			{
				return;
			}

			if( ImGui.Begin( Loc.Localize( "Window Title: Config", "Distance Settings" ) + "###Distance Settings", ref mSettingsWindowVisible,
				ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Aggro Distance", "Show the remaining distance from the enemy before they will detect you." ) + "###Show aggro distance.", ref mConfiguration.mShowAggroDistance );
				ImGuiHelpMarker( Loc.Localize( "Help: Show Aggro Distance", "This distance will only be shown when it is known, and only on major bosses.  Additionally, it will only be shown until you enter combat." ) );

				if( mConfiguration.ShowAggroDistance )
				{
					if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Aggro Widget Appearance", "Aggro Widget Appearance" ) + "###Aggro Widget Appearance Header." ) )
					{
						ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Text Position", "Position of the aggro widget (X,Y):" ) );
						ImGui.DragFloat2( "###AggroDistanceTextPositionSlider", ref mConfiguration.mAggroDistanceTextPosition, 1f, 0f, Math.Max( ImGuiHelpers.MainViewport.Size.X, ImGuiHelpers.MainViewport.Size.Y ), "%g" );
						ImGui.Checkbox( Loc.Localize( "Config Option: Aggro Distance Text Use Heavy Font", "Use heavy font for aggro widget" ) + "###Aggro Distance font heavy.", ref mConfiguration.mAggroDistanceFontHeavy );
						ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Text Font Size", "Aggro widget font size:" ) );
						ImGui.SliderInt( "##AggroDistanceTextFontSizeSlider", ref mConfiguration.mAggroDistanceFontSize, 6, 36 );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Color", "Aggro widget text color" ) + "###AggroDistanceTextColorPicker", ref mConfiguration.mAggroDistanceTextColor, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Glow Color", "Aggro widget text glow color" ) + "###AggroDistanceTextEdgeColorPicker", ref mConfiguration.mAggroDistanceTextEdgeColor, ImGuiColorEditFlags.NoInputs );

						ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Color Caution", "Aggro widget text color (caution range)" ) + "###AggroDistanceCautionTextColorPicker", ref mConfiguration.mAggroDistanceCautionTextColor, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Glow Color Caution", "Aggro widget text glow color (caution range)" ) + "###AggroDistanceCautionTextEdgeColorPicker", ref mConfiguration.mAggroDistanceCautionTextEdgeColor, ImGuiColorEditFlags.NoInputs );

						ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Color Warning", "Aggro widget text color (warning range)" ) + "###AggroDistanceWarningTextColorPicker", ref mConfiguration.mAggroDistanceWarningTextColor, ImGuiColorEditFlags.NoInputs );
						ImGui.ColorEdit4( Loc.Localize( "Config Option: Aggro Distance Text Glow Color Warning", "Aggro widget text glow color (warning range)" ) + "###AggroDistanceWarningTextEdgeColorPicker", ref mConfiguration.mAggroDistanceWarningTextEdgeColor, ImGuiColorEditFlags.NoInputs );

						ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Caution Range", "Aggro distance \"caution\" range (y):" ) );
						ImGui.SliderInt( "##AggroDistanceCautionRangeSlider", ref mConfiguration.mAggroCautionDistance_Yalms, 0, 30 );

						ImGui.Text( Loc.Localize( "Config Option: Aggro Distance Warning Range", "Aggro distance \"warning\" range (y):" ) );
						ImGui.SliderInt( "##AggroDistanceWarningRangeSlider", ref mConfiguration.mAggroWarningDistance_Yalms, 0, 30 );

						ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + "###Show aggro distance units.", ref mConfiguration.mShowUnitsOnAggroDistance );
						ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distance:" ) );
						ImGuiHelpMarker( Loc.Localize( "Help: Aggro Distance Precision", "Aggro ranges are only accurate to within ~0.05 yalms, so please be wary when using more than one decimal point of precision." ) );
						ImGui.SliderInt( "##AggroDistancePrecisionSlider", ref mConfiguration.mAggroDistanceDecimalPrecision, 0, 3 );
					}

					if( ImGui.CollapsingHeader( Loc.Localize( "Config Section Header: Aggro Distance Data", "Aggro Distance Data" ) + "###Aggro Distance Data Header." ) )
					{
						ImGui.Checkbox( Loc.Localize( "Config Option: Auto Update Aggro Data", "Try to automatically fetch the most recent aggro distance data on startup." ) + "###Auto Update Aggro Data.", ref mConfiguration.mAutoUpdateAggroData );
						if( ImGui.Button( Loc.Localize( "Button: Download Aggro Distances", "Check for Updated Aggro Distances" ) + "###Download updated aggro distances." ) )
						{
							Task.Run( async () =>
							{
								var downloadedFile = await BNpcAggroInfoDownloader.DownloadUpdatedAggroDataAsync( Path.Join( mPluginInterface.GetPluginConfigDirectory(), "AggroDistances.dat" ) );
								if( downloadedFile != null ) BNpcAggroInfo.Init( mDataManager, downloadedFile );
							} );
						}
						if( BNpcAggroInfoDownloader.CurrentDownloadStatus != BNpcAggroInfoDownloader.DownloadStatus.None )
						{
							ImGui.Text( $"Status of most recent update attempt: {BNpcAggroInfoDownloader.GetCurrentDownloadStatusMessage()}" );
						}
					}
				}

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				if( ImGui.Button( Loc.Localize( "Button: Add Distance Widget", "Add Widget" ) + $"###Add Widget Button." ) )
				{
					mConfiguration.DistanceWidgetConfigs.Add( new() );
				}

				for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; ++i )
				{
					var config = mConfiguration.DistanceWidgetConfigs[i];
					var filters = config.Filters;
					if( ImGui.CollapsingHeader( String.Format( Loc.Localize( "Config Section Header: Distance Widget", "Distance Widget" ), i ) + $"###Distance Widget Header {i}." ) )
					{
						if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Rules", "Distance Rules" ) + $"###Distance Widget Rules Header {i}." ) )
						{
							string str =	$"{Plugin.GetLocalizedTargetTypeEnumString( Plugin.TargetType.Target )}\0" +
											$"{Plugin.GetLocalizedTargetTypeEnumString( Plugin.TargetType.SoftTarget )}\0" +
											$"{ Plugin.GetLocalizedTargetTypeEnumString( Plugin.TargetType.FocusTarget )}\0" +
											$"{ Plugin.GetLocalizedTargetTypeEnumString( Plugin.TargetType.MouseOverTarget )}";
							ImGui.Combo( "###DistanceTypeDropdown", ref config.mApplicableTargetType, str );
							ImGuiHelpMarker( Loc.Localize( "Help: Applicable Target Type", "The type of target for which this widget will show distance." ) );
							if( config.ApplicableTargetType == Plugin.TargetType.Target )
							{
								ImGui.Indent();
								ImGui.Checkbox( Loc.Localize( "Config Option: Target Includes Soft Target", "\"Target\" includes soft target" ) + $"###Target Includes Soft Target {i}.", ref config.mTargetIncludesSoftTarget );
								ImGuiHelpMarker( Loc.Localize( "Help: Show Target Includes Soft Target", "When the target type above is set to \"Target\", also show the distance to the soft target when it is valid.  This generally only matters for controller players and some two-handed keyboard players." ) );
								ImGui.Unindent();
							}
							ImGui.Checkbox( Loc.Localize( "Config Option: Distance is to Ring", "Show distance to target ring, not target center." ) + $"###Distance is to ring {i}.", ref config.mDistanceIsToRing );
							ImGui.Text( Loc.Localize( "Config Option: Distance Measurement Offset", "Amount to offset the distance readout (y):" ) );
							ImGuiHelpMarker( Loc.Localize( "Help: Distance Readout Offset", "This value is subtracted from the real distance to determine the displayed distance.  This can be used to get the widget to show the distance from being able to hit the boss with a skill, for example." ) );
							ImGui.DragFloat( $"###DistanceOffsetSlider {i}", ref config.mDistanceOffset_Yalms, 0.01f, 0f, 30f );
							ImGui.TreePop();
						}

						if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Filters", "Target Filters" ) + $"###Distance Widget Filters Header {i}." ) )
						{
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Players", "Show the distance to players." ) + $"###Show distance to players .", ref filters.mShowDistanceOnPlayers );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on BattleNpc", "Show the distance to combatant NPCs." ) + $"###Show distance to BattleNpc {i}.", ref filters.mShowDistanceOnBattleNpc );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventNpc", "Show the distance to non-combatant NPCs." ) + $"###Show distance to EventNpc {i}.", ref filters.mShowDistanceOnEventNpc );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Treasure", "Show the distance to treasure chests." ) + $"###Show distance to treasure {i}.", ref filters.mShowDistanceOnTreasure );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Aetheryte", "Show the distance to aetherytes." ) + $"###Show distance to aetheryte {i}.", ref filters.mShowDistanceOnAetheryte );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Gathering Node", "Show the distance to gathering nodes." ) + $"###Show distance to gathering node {i}.", ref filters.mShowDistanceOnGatheringNode );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventObj", "Show the distance to interactable objects." ) + $"###Show distance to EventObj {i}.", ref filters.mShowDistanceOnEventObj );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Companion", "Show the distance to companions." ) + $"###Show distance to companion {i}.", ref filters.mShowDistanceOnCompanion );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Housing", "Show the distance to housing items." ) + $"###Show distance to housing {i}.", ref filters.mShowDistanceOnHousing );
							ImGui.TreePop();
						}

						if( ImGui.TreeNode( Loc.Localize( "Config Section Header: Distance Widget Appearance", "Appearance" ) + $"###Distance Widget Appearance Header {i}." ) )
						{
							Vector2 sliderLimits = new( config.MouseoverTargetFollowsMouse ? -300 : 0, config.MouseoverTargetFollowsMouse ? 300 : Math.Max( ImGuiHelpers.MainViewport.Size.X, ImGuiHelpers.MainViewport.Size.Y ) );
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Position", "Position of the distance readout (X,Y):" ) );
							ImGui.DragFloat2( $"###DistanceTextPositionSlider {i}", ref config.mTextPosition, 1f, sliderLimits.X, sliderLimits.Y, "%g" );
							if( config.ApplicableTargetType == Plugin.TargetType.MouseOverTarget )
							{
								ImGui.Checkbox( Loc.Localize( "Config Option: Mouseover Widget Follows Mouse", "Widget follows the cursor" ) + $"###Mouseover Target Follow Mouse {i}.", ref config.mMouseoverTargetFollowsMouse );
								ImGuiHelpMarker( Loc.Localize( "Help: Mouseover Widget Follows Mouse", "The widget will follow the mouse, and the position above becomes an offset from the cursor location." ) );
							}
							ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Use Heavy Font", "Use heavy font for distance text" ) + $"###Distance font heavy {i}.", ref config.mFontHeavy );
							ImGui.Text( Loc.Localize( "Config Option: Distance Text Font Size", "Distance text font size:" ) );
							ImGui.SliderInt( $"##DistanceTextFontSizeSlider {i}", ref config.mFontSize, 6, 36 );
							ImGui.Checkbox( Loc.Localize( "Config Option: Distance Text Track Target Bar Color", "Attempt to use target bar text color" ) + $"###Distance Text Use Target Bar Color {i}.", ref config.mTrackTargetBarTextColor );
							ImGuiHelpMarker( Loc.Localize( "Help: Distance Text Track Target Bar Color", "If the color of the target bar text can be determined, it will take precedence; otherwise the colors set below will be used." ) );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Color", "Distance text color" ) + $"###DistanceTextColorPicker {i}", ref config.mTextColor, ImGuiColorEditFlags.NoInputs );
							ImGui.ColorEdit4( Loc.Localize( "Config Option: Distance Text Glow Color", "Distance text glow color" ) + $"###DistanceTextEdgeColorPicker {i}", ref config.mTextEdgeColor, ImGuiColorEditFlags.NoInputs );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + $"###Show distance units {i}.", ref config.mShowUnits );
							ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Mode Indicator", "Show the distance mode indicator." ) + $"###Show distance type marker {i}.", ref config.mShowDistanceModeMarker );
							ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distances:" ) );
							ImGui.SliderInt( $"##DistancePrecisionSlider {i}", ref config.mDecimalPrecision, 0, 3 );
							ImGui.TreePop();
						}

						if( ImGui.Button( Loc.Localize( "Button: Delete Distance Widget", "Delete Widget" ) + $"###Delete Widget Button {i}." ) )
						{
							UpdateDistanceTextNode( (uint)i, new(), new(), false );
							mConfiguration.DistanceWidgetConfigs.RemoveAt( i );
						}
					}
				}

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

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
			if( ImGui.Begin( Loc.Localize( "Window Title: Debug Data", "Debug Data" ) + "###Debug Data", ref mDebugWindowVisible ) )
			{
				ImGui.Checkbox( "Show known aggro range data", ref mDebugAggroEntitiesWindowVisible );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( $"TerritoryType: {mClientState.TerritoryType}" );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( $"Target Distance Data:" );
				ImGui.Indent();
				ImGui.Text( $"{mPlugin.GetDistanceInfo( Plugin.TargetType.Target, false ).ToString()}" );
				ImGui.Unindent();

				ImGui.Text( $"Soft Target Distance Data:" );
				ImGui.Indent();
				ImGui.Text( $"{mPlugin.GetDistanceInfo( Plugin.TargetType.SoftTarget, false ).ToString()}" );
				ImGui.Unindent();

				ImGui.Text( $"Focus Target Distance Data:" );
				ImGui.Indent();
				ImGui.Text( $"{mPlugin.GetDistanceInfo( Plugin.TargetType.FocusTarget, false ).ToString()}" );
				ImGui.Unindent();

				ImGui.Text( $"MO Target Distance Data:" );
				ImGui.Indent();
				ImGui.Text( $"{mPlugin.GetDistanceInfo( Plugin.TargetType.MouseOverTarget, false ).ToString()}" );
				ImGui.Unindent();
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
			//	Draw the aggro widget.
			UpdateAggroDistanceTextNode( mPlugin.GetDistanceInfo( Plugin.TargetType.Target, true ), mPlugin.ShouldDrawAggroDistanceInfo() );

			//	Draw each configured distance widget.
			for( int i = 0; i < mConfiguration.DistanceWidgetConfigs.Count; ++i )
			{
				UpdateDistanceTextNode( (uint)i,
										mPlugin.GetDistanceInfo( mConfiguration.DistanceWidgetConfigs[i].ApplicableTargetType, mConfiguration.DistanceWidgetConfigs[i].TargetIncludesSoftTarget ),
										mConfiguration.DistanceWidgetConfigs[i],
										mPlugin.ShouldDrawDistanceInfo( mConfiguration.DistanceWidgetConfigs[i] ) );
			}
		}

		unsafe protected void UpdateDistanceTextNode( uint distanceWidgetNumber, DistanceInfo distanceInfo, DistanceWidgetConfig config, bool show )
		{
			string str = "";
			Vector4 textColorToUse = config.TextColor;
			Vector4 edgeColorToUse = config.TextEdgeColor;

			if( distanceInfo.IsValid )
			{
				float distance = config.DistanceIsToRing ? distanceInfo.DistanceFromTargetRing_Yalms : distanceInfo.DistanceFromTarget_Yalms;
				distance -= config.DistanceOffset_Yalms;
				distance = Math.Max( 0, distance );
				string unitString = config.ShowUnits ? "y" : "";
				string distanceTypeSymbol = "";
				if( config.ShowDistanceModeMarker ) distanceTypeSymbol = config.DistanceIsToRing ? "◯ " : "· ";
				str = $"{distanceTypeSymbol}{distance.ToString( $"F{config.DecimalPrecision}" )}{unitString}";
			}

			if( config.TrackTargetBarTextColor )
			{
				AtkUnitBase* pTargetAddonToUse = null;
				UInt16 targetBarNameNodeIndex = 0;
				var pTargetAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_TargetInfo", 1 );
				var pTargetAddonSplit = (AtkUnitBase*)mGameGui.GetAddonByName( "_TargetInfoMainTarget", 1 );
				if( pTargetAddon != null && pTargetAddon->IsVisible )
				{
					pTargetAddonToUse = pTargetAddon;
					targetBarNameNodeIndex = 39;
				}
				else if( pTargetAddonSplit != null && pTargetAddonSplit->IsVisible )
				{
					pTargetAddonToUse = pTargetAddonSplit;
					targetBarNameNodeIndex = 8;
				}

				if( pTargetAddonToUse != null )
				{
					var pTargetNameNode = pTargetAddonToUse->UldManager.NodeListSize > targetBarNameNodeIndex ? pTargetAddonToUse->UldManager.NodeList[targetBarNameNodeIndex] : null;
					if( pTargetNameNode != null && pTargetNameNode->GetAsAtkTextNode() != null )
					{
						var pTargetNameTextNode = pTargetNameNode->GetAsAtkTextNode();

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

			Vector2 mouseoverOffset = new()
			{
				X = config.MouseoverTargetFollowsMouse && config.ApplicableTargetType == Plugin.TargetType.MouseOverTarget ? ImGui.GetMousePos().X : 0,
				Y = config.MouseoverTargetFollowsMouse && config.ApplicableTargetType == Plugin.TargetType.MouseOverTarget ? ImGui.GetMousePos().Y : 0
			};

			TextNodeDrawData drawData = new TextNodeDrawData()
			{
				PositionX = (short)( config.TextPosition.X + mouseoverOffset.X ),
				PositionY = (short)( config.TextPosition.Y + mouseoverOffset.Y ),
				TextColorA = (byte)( textColorToUse.W * 255f ),
				TextColorR = (byte)( textColorToUse.X * 255f ),
				TextColorG = (byte)( textColorToUse.Y * 255f ),
				TextColorB = (byte)( textColorToUse.Z * 255f ),
				EdgeColorA = (byte)( edgeColorToUse.W * 255f ),
				EdgeColorR = (byte)( edgeColorToUse.X * 255f ),
				EdgeColorG = (byte)( edgeColorToUse.Y * 255f ),
				EdgeColorB = (byte)( edgeColorToUse.Z * 255f ),
				FontSize = (byte)config.FontSize,
				AlignmentFontType = (byte)( (int)AlignmentType.BottomRight | ( config.FontHeavy ? 0x10 : 0 ) ),
				LineSpacing = 24,
				CharSpacing = 1
			};

			UpdateTextNode( mDistanceNodeIDBase + distanceWidgetNumber, str, drawData, show );
		}

		unsafe protected void UpdateAggroDistanceTextNode( DistanceInfo distanceInfo, bool show )
		{
			string str = "";
			Vector4 color = mConfiguration.mAggroDistanceTextColor;
			Vector4 edgeColor = mConfiguration.mAggroDistanceTextEdgeColor;
			if( distanceInfo.IsValid )
			{
				float distance = Math.Max( 0, distanceInfo.DistanceFromTargetAggro_Yalms );
				string unitString = mConfiguration.ShowUnitsOnAggroDistance ? "y" : "";
				str = $"Aggro in {distance.ToString( $"F{mConfiguration.AggroDistanceDecimalPrecision}" )}{unitString}";

				if( distance < mConfiguration.AggroWarningDistance_Yalms)
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

			TextNodeDrawData drawData = new TextNodeDrawData()
			{
				PositionX = (short)mConfiguration.AggroDistanceTextPosition.X,
				PositionY = (short)mConfiguration.AggroDistanceTextPosition.Y,
				TextColorA = (byte)( color.W * 255f ),
				TextColorR = (byte)( color.X * 255f ),
				TextColorG = (byte)( color.Y * 255f ),
				TextColorB = (byte)( color.Z * 255f ),
				EdgeColorA = (byte)( edgeColor.W * 255f ),
				EdgeColorR = (byte)( edgeColor.X * 255f ),
				EdgeColorG = (byte)( edgeColor.Y * 255f ),
				EdgeColorB = (byte)( edgeColor.Z * 255f ),
				FontSize = (byte)mConfiguration.AggroDistanceFontSize,
				AlignmentFontType = (byte)( (int)AlignmentType.BottomRight | ( mConfiguration.AggroDistanceFontHeavy ? 0x10 : 0 ) ),
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

		protected static readonly uint mDistanceNodeIDBase = 0x6C78B300;    //YOLO hoping for no collisions.
		protected static readonly uint mAggroDistanceNodeID = mDistanceNodeIDBase - 1;
	}
}