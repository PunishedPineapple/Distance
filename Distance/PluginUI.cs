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
		public void Dispose()
		{
		}

		public void Initialize()
		{
			//Load Skeleton sheet?  Put in own class probably
		}

		public void Draw()
		{
			//	Draw the sub-windows.
			DrawSettingsWindow();
			DrawMainWindow();

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
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Players", "Show the distance to players." ) + "###Show distance to players.", ref mConfiguration.mShowDistanceOnPlayers );
				ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance Units", "Show units on distance values." ) + "###Show distance units.", ref mConfiguration.mShowUnitsOnDistances );
				ImGui.Checkbox( Loc.Localize( "Config Option: Distance is to Ring", "Show distance to target ring, not target center." ) + "###Distance is to ring.", ref mConfiguration.mDistanceIsToRing );
				ImGui.Text( Loc.Localize( "Config Option: Decimal Precision", "Number of decimal places to show on distances:" ) );
				ImGui.SliderInt( "##DistancePrecisionSlider", ref mConfiguration.mDecimalPrecision, 0, 3 );

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

		protected void DrawMainWindow()
		{
			if( !MainWindowVisible )
			{
				return;
			}

			//	Draw the window.
			ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
			if( ImGui.Begin( Loc.Localize( "Window Title: Distance Data", "Distance Data" ) + "###Distance Data", ref mMainWindowVisible ) )
			{
				if( mPlugin.CurrentDistanceInfo != null )
				{
					ImGui.Text( $"Player: ({mPlugin.CurrentDistanceInfo.Position.X}, {mPlugin.CurrentDistanceInfo.Position.Y}, {mPlugin.CurrentDistanceInfo.Position.Z})" );
					ImGui.Text( $"Target: ({mPlugin.CurrentDistanceInfo.TargetPosition.X}, {mPlugin.CurrentDistanceInfo.TargetPosition.Y}, {mPlugin.CurrentDistanceInfo.TargetPosition.Z})" );
					ImGui.Text( $"Distance (y): {mPlugin.CurrentDistanceInfo.DistanceFromTarget_Yalms}" );
					ImGui.Text( $"Distance from Ring (y): {mPlugin.CurrentDistanceInfo.DistanceFromTargetRing_Yalms}" );
					ImGui.Text( $"Distance from Aggro (y): {mPlugin.CurrentDistanceInfo.DistanceFromTargetAggro_Yalms}" );
				}
			}

			//	We're done.
			ImGui.End();
		}

		protected void DrawOnGameUI()
		{
			if( mPlugin.CurrentDistanceInfo != null )
			{
				string text = "";
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

		protected bool mMainWindowVisible = false;
		public bool MainWindowVisible
		{
			get { return mMainWindowVisible; }
			set { mMainWindowVisible = value; }
		}
	}
}