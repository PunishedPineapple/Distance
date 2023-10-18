namespace Distance
{
	public class DistanceWidgetFiltersConfig
	{
		public bool ShowDistanceOnObjectKind( Dalamud.Game.ClientState.Objects.Enums.ObjectKind objectKind )
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
	}
}
