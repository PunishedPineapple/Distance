using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using CheapLoc;

using Dalamud.Utility;

using Dalamud.Bindings.ImGui;

using Newtonsoft.Json;

namespace Distance;

public class DistanceWidgetFiltersConfig
{
	internal bool ShowDistanceForObjectKind( Dalamud.Game.ClientState.Objects.Enums.ObjectKind objectKind )
	{
		return objectKind switch
		{
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player => ShowDistanceOnPlayers,
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc => ShowDistanceOnBattleNpc,
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc => ShowDistanceOnEventNpc,
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure => ShowDistanceOnTreasure,
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Aetheryte => ShowDistanceOnAetheryte,
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint => ShowDistanceOnGatheringNode,
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj => ShowDistanceOnEventObj,
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Companion => ShowDistanceOnCompanion,
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Housing => ShowDistanceOnHousing,
			_ => false,
		};
	}

	internal bool ShowDistanceForClassJob( UInt32 classJob )
	{
		return	classJob > 0 &&
				classJob < ApplicableClassJobsArray.Length &&
				ApplicableClassJobsArray[classJob] == true;
	}

	internal bool ShowDistanceForConditions( bool inCombat, bool inInstance )
	{
		bool combatShow = inCombat && ShowInCombat || !inCombat && ShowOutOfCombat;
		bool instanceShow = inInstance && ShowInInstance || !inInstance && ShowOutOfInstance;

		return combatShow && instanceShow;
	}

	internal void DrawObjectKindOptions()
	{
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Players", "Show the distance to players." ) + $"###ObjectsPlayersCheckbox", ref ShowDistanceOnPlayers );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on BattleNpc", "Show the distance to combatant NPCs." ) + $"###ObjectsBattleNpcsCheckbox", ref ShowDistanceOnBattleNpc );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventNpc", "Show the distance to non-combatant NPCs." ) + $"###ObjectsEventNpcsCheckbox", ref ShowDistanceOnEventNpc );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Treasure", "Show the distance to treasure chests." ) + $"###ObjectsTreasureCheckbox", ref ShowDistanceOnTreasure );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Aetheryte", "Show the distance to aetherytes." ) + $"###ObjectsAetherytesCheckbox", ref ShowDistanceOnAetheryte );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Gathering Node", "Show the distance to gathering nodes." ) + $"###ObjectsGatheringNodesCheckbox", ref ShowDistanceOnGatheringNode );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on EventObj", "Show the distance to interactable objects." ) + $"###ObjectsEventObjsCheckbox", ref ShowDistanceOnEventObj );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Companion", "Show the distance to companions." ) + $"###ObjectsCompanionsCheckbox", ref ShowDistanceOnCompanion );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show Distance on Housing", "Show the distance to housing items." ) + $"###ObjectsHousingCheckbox", ref ShowDistanceOnHousing );
	}

	internal void DrawClassJobOptions()
	{
		float maxJobTextWidth = 0;
		float currentJobTextWidth = 0;
		float checkboxWidth = 0;
		float leftMarginPos = 0;
		var classJobDict = ClassJobUtils.ClassJobDict;
		foreach( var entry in classJobDict )
		{
			if( !entry.Value.Abbreviation.IsNullOrEmpty() )
			{
				maxJobTextWidth = Math.Max( maxJobTextWidth, ImGui.CalcTextSize( entry.Value.Abbreviation ).X );
			}
		}
		foreach( var sortCategory in Enum.GetValues<ClassJobSortCategory>() )
		{
			int displayedJobsCount = 0;
			int rowLength = sortCategory < ClassJobSortCategory.Class ? 6 : 4;
			for( int i = 1; i < ApplicableClassJobsArray.Length; ++i )
			{
				if( classJobDict.ContainsKey( i ) && classJobDict[i].SortCategory == sortCategory && !classJobDict[i].Abbreviation.IsNullOrEmpty() )
				{
					int colNum = (int) displayedJobsCount % rowLength;
					currentJobTextWidth = ImGui.CalcTextSize( classJobDict[i].Abbreviation ).X;
					if( displayedJobsCount != 0 && colNum != 0 ) ImGui.SameLine( leftMarginPos + ( checkboxWidth + maxJobTextWidth + ImGui.GetStyle().FramePadding.X + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.GetStyle().ItemSpacing.X ) * colNum );
					ImGui.Checkbox( $"{classJobDict[i].Abbreviation}###ClassJobs{i}Checkbox", ref ApplicableClassJobsArray[i] );

					//	Big kludges, but I'm stupid and don't know a better way.
					if( displayedJobsCount == 0 )
					{
						checkboxWidth = ImGui.GetItemRectSize().Y;
						leftMarginPos = ImGui.GetItemRectMin().X - ImGui.GetWindowPos().X;
					}

					++displayedJobsCount;
				}
			}
		}
	}

	internal void DrawConditionOptions()
	{
		ImGui.Checkbox( Loc.Localize( "Config Option: Show in Combat", "Show in combat." ) + "###ShowInCombatCheckbox", ref ShowInCombat );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show out of Combat", "Show out of combat." ) + "###ShowOutOfCombatCheckbox", ref ShowOutOfCombat );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show in Instance", "Show in instance." ) + "###ShowInInstanceCheckbox", ref ShowInInstance );
		ImGui.Checkbox( Loc.Localize( "Config Option: Show out of Instance", "Show out of instance." ) + "###ShowOutOfInstanceCheckbox", ref ShowOutOfInstance );
	}

	#region Object Types

	public bool ShowDistanceOnPlayers = true;
	public bool ShowDistanceOnBattleNpc = true;
	public bool ShowDistanceOnEventNpc = false;
	public bool ShowDistanceOnTreasure = false;
	public bool ShowDistanceOnAetheryte = false;
	public bool ShowDistanceOnGatheringNode = false;
	public bool ShowDistanceOnEventObj = false;
	public bool ShowDistanceOnCompanion = false;
	public bool ShowDistanceOnHousing = false;

	#endregion

	#region ClassJobs

	//	Note: This is all a bit of a mess because of needing to have easily-accessible references for
	//	use with ImGui while trying to gracefully handle any changes to ClassJobs over game versions.

	public DistanceWidgetFiltersConfig()
	{
		for( int i = 0; i < ApplicableClassJobsArray.Length; ++i )
		{
			ApplicableClassJobsArray[i] = ClassJobUtils.ClassJobDict.ContainsKey( i ) && ClassJobUtils.ClassJobDict[i].DefaultSelected;
		}
	}

	[OnDeserialized]
	protected void PostDeserializeTasks( StreamingContext s )
	{
		if( mApplicableClassJobs == null ) return;

		for( int i = 0; i < ApplicableClassJobsArray.Length; ++i )
		{
			if( ClassJobUtils.ClassJobDict.ContainsKey( i ) )
			{
				string abbreviation_En = ClassJobUtils.ClassJobDict[i].Abbreviation_En;
				if( mApplicableClassJobs.ContainsKey( abbreviation_En ) )
				{
					ApplicableClassJobsArray[i] = mApplicableClassJobs[abbreviation_En];
				}
			}
		}
	}

	[OnSerializing]
	protected void PreSerializeTasks( StreamingContext s )
	{
		Dictionary<string, bool> dict = new();

		for( int i = 0; i < ApplicableClassJobsArray.Length; ++i )
		{
			if( ClassJobUtils.ClassJobDict.ContainsKey( i ) )
			{
				dict.Add( ClassJobUtils.ClassJobDict[i].Abbreviation_En, ApplicableClassJobsArray[i] );
			}
		}

		mApplicableClassJobs = dict;
	}

	[OnSerialized]
	protected void PostSerializeTasks( StreamingContext s )
	{
		mApplicableClassJobs = null;
	}

	//	What we actually use while running.
	[NonSerialized]
	internal readonly bool[] ApplicableClassJobsArray = new bool[ClassJobUtils.ClassJobDict.Count];

	//	Used for serialization/deserialization to have more robust at-rest storage.
	[JsonProperty]
	protected Dictionary<string, bool> mApplicableClassJobs = null;

	#endregion

	#region Conditions

	public bool ShowInCombat = true;
	public bool ShowOutOfCombat = true;
	public bool ShowInInstance = true;
	public bool ShowOutOfInstance = true;

	#endregion
}
