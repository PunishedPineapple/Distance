using System;
using System.Runtime.Serialization;

namespace Distance
{
	public class DistanceWidgetFiltersConfig
	{
		public bool ShowDistanceForObjectKind( Dalamud.Game.ClientState.Objects.Enums.ObjectKind objectKind )
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

		public bool ShowDistanceForClassJob( UInt32 classJob )
		{
			return	classJob > 0 &&
					classJob < mApplicableClassJobsArray.Length &&
					mApplicableClassJobsArray[classJob] == true;
		}

		#region Object Types

		public bool mShowDistanceOnPlayers = true;
		public bool ShowDistanceOnPlayers
		{
			get { return mShowDistanceOnPlayers; }
			set { mShowDistanceOnPlayers = value; }
		}

		public bool mShowDistanceOnBattleNpc = true;
		public bool ShowDistanceOnBattleNpc
		{
			get { return mShowDistanceOnBattleNpc; }
			set { mShowDistanceOnBattleNpc = value; }
		}

		public bool mShowDistanceOnEventNpc = false;
		public bool ShowDistanceOnEventNpc
		{
			get { return mShowDistanceOnEventNpc; }
			set { mShowDistanceOnEventNpc = value; }
		}

		public bool mShowDistanceOnTreasure = false;
		public bool ShowDistanceOnTreasure
		{
			get { return mShowDistanceOnTreasure; }
			set { mShowDistanceOnTreasure = value; }
		}

		public bool mShowDistanceOnAetheryte = false;
		public bool ShowDistanceOnAetheryte
		{
			get { return mShowDistanceOnAetheryte; }
			set { mShowDistanceOnAetheryte = value; }
		}

		public bool mShowDistanceOnGatheringNode = false;
		public bool ShowDistanceOnGatheringNode
		{
			get { return mShowDistanceOnGatheringNode; }
			set { mShowDistanceOnGatheringNode = value; }
		}

		public bool mShowDistanceOnEventObj = false;
		public bool ShowDistanceOnEventObj
		{
			get { return mShowDistanceOnEventObj; }
			set { mShowDistanceOnEventObj = value; }
		}

		public bool mShowDistanceOnCompanion = false;
		public bool ShowDistanceOnCompanion
		{
			get { return mShowDistanceOnCompanion; }
			set { mShowDistanceOnCompanion = value; }
		}

		public bool mShowDistanceOnHousing = false;
		public bool ShowDistanceOnHousing
		{
			get { return mShowDistanceOnHousing; }
			set { mShowDistanceOnHousing = value; }
		}

		#endregion

		#region ClassJobs

		internal bool[] ApplicableClassJobsArray => mApplicableClassJobsArray;

		public DistanceWidgetFiltersConfig()
		{
			mApplicableClassJobsArray = new bool[ClassJobUtils.ClassJobDict.Count];
			for( int i = 0; i < mApplicableClassJobsArray.Length; ++i ) mApplicableClassJobsArray[i] = ClassJobUtils.ClassJobDict[(uint)i].DefaultSelected;
		}

		[OnDeserialized]
		internal void ValidateClassJobData( StreamingContext s )
		{
			if( mApplicableClassJobsArray?.Length != ClassJobUtils.ClassJobDict.Count )
			{
				bool[] newArray = new bool[ClassJobUtils.ClassJobDict.Count];
				try
				{
					mApplicableClassJobsArray?.CopyTo( newArray, 0 );
				}
				catch( Exception e )
				{
					for( int i = 0; i < mApplicableClassJobsArray.Length; ++i ) newArray[i] = ClassJobUtils.ClassJobDict[(uint)i].DefaultSelected;
					Service.PluginLog.Warning( $"Exception while validating ClassJob filters; using defaults for all ClassJobs:\r\n{e}" );
				}
				mApplicableClassJobsArray = newArray;
			}
		}

		public bool[] mApplicableClassJobsArray = null;

		#endregion
	}
}
