using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Numerics;

namespace Distance
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public Configuration()
		{
		}

		//  Our own configuration options and data.

		//	Need a real backing field on the properties for use with ImGui.
		public bool mSuppressCommandLineResponses = false;
		public bool SuppressCommandLineResponses
		{
			get { return mSuppressCommandLineResponses; }
			set { mSuppressCommandLineResponses = value; }
		}

		public bool mShowAggroDistance = true;
		public bool ShowAggroDistance
		{
			get { return mShowAggroDistance; }
			set { mShowAggroDistance = value; }
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

		public bool mDistanceIsToRing = true;
		public bool DistanceIsToRing
		{
			get { return mDistanceIsToRing; }
			set { mDistanceIsToRing = value; }
		}

		public bool mShowUnitsOnDistances = true;
		public bool ShowUnitsOnDistances
		{
			get { return mShowUnitsOnDistances; }
			set { mShowUnitsOnDistances = value; }
		}

		public bool mShowDistanceModeMarker = true;
		public bool ShowDistanceModeMarker
		{
			get { return mShowDistanceModeMarker; }
			set { mShowDistanceModeMarker = value; }
		}

		public int mDecimalPrecision = 2;
		public int DecimalPrecision
		{
			get { return mDecimalPrecision; }
			set { mDecimalPrecision = value; }
		}

		public int mDistanceFontSize = 14;
		public int DistanceFontSize
		{
			get { return mDistanceFontSize; }
			set { mDistanceFontSize = value; }
		}

		public bool mDistanceFontHeavy = false;
		public bool DistanceFontHeavy
		{
			get { return mDistanceFontHeavy; }
			set { mDistanceFontHeavy = value; }
		}

		public Vector2 mDistanceTextPosition = Vector2.One;
		public Vector2 DistanceTextPosition
		{
			get { return mDistanceTextPosition; }
			set { mDistanceTextPosition = value; }
		}

		public Vector4 mDistanceTextColor = new Vector4( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
		public Vector4 DistanceTextColor
		{
			get { return mDistanceTextColor; }
			set { mDistanceTextColor = value; }
		}

		public Vector4 mDistanceTextEdgeColor = new Vector4( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 DistanceTextEdgeColor
		{
			get { return mDistanceTextEdgeColor; }
			set { mDistanceTextEdgeColor = value; }
		}

		//  Plugin framework and related convenience functions below.
		public void Initialize( DalamudPluginInterface pluginInterface )
		{
			mPluginInterface = pluginInterface;
		}

		public void Save()
		{
			mPluginInterface.SavePluginConfig( this );
		}

		[NonSerialized]
		protected DalamudPluginInterface mPluginInterface;

		public int Version { get; set; } = 0;
	}
}
