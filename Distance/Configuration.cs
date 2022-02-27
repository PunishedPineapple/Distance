using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;

namespace ReadyCheckHelper
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

		public int mDecimalPrecision = 2;
		public int DecimalPrecision
		{
			get { return mDecimalPrecision; }
			set { mDecimalPrecision = value; }
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
