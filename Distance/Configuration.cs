using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Configuration;
using Dalamud.Plugin;

namespace Distance
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		//	Need a real backing field on the properties for use with ImGui.
		public bool mSuppressCommandLineResponses = false;
		public bool SuppressCommandLineResponses
		{
			get { return mSuppressCommandLineResponses; }
			set { mSuppressCommandLineResponses = value; }
		}

		public bool mAutoUpdateAggroData = false;
		public bool AutoUpdateAggroData
		{
			get { return mAutoUpdateAggroData; }
			set { mAutoUpdateAggroData = value; }
		}

		public bool mShowAggroDistance = false;
		public bool ShowAggroDistance
		{
			get { return mShowAggroDistance; }
			set { mShowAggroDistance = value; }
		}

		//	Backing field as an int to work with ImGui.
		public int mAggroDistanceApplicableTargetType = (int)TargetType.Target_And_Soft_Target;
		public TargetType AggroDistanceApplicableTargetType
		{
			get { return (TargetType)mAggroDistanceApplicableTargetType; }
			set { mAggroDistanceApplicableTargetType = (int)value; }
		}

		//	Backing field as an int to work with ImGui.
		public int mAggroDistanceUIAttachType = (int)AddonAttachType.Auto;
		public AddonAttachType AggroDistanceUIAttachType
		{
			get { return (AddonAttachType)mAggroDistanceUIAttachType; }
			set { mAggroDistanceUIAttachType = (int)value; }
		}

		public bool mShowUnitsOnAggroDistance = true;
		public bool ShowUnitsOnAggroDistance
		{
			get { return mShowUnitsOnAggroDistance; }
			set { mShowUnitsOnAggroDistance = value; }
		}

		public int mAggroDistanceDecimalPrecision = 2;
		public int AggroDistanceDecimalPrecision
		{
			get { return mAggroDistanceDecimalPrecision; }
			set { mAggroDistanceDecimalPrecision = value; }
		}

		public int mAggroDistanceFontSize = 16;
		public int AggroDistanceFontSize
		{
			get { return mAggroDistanceFontSize; }
			set { mAggroDistanceFontSize = value; }
		}

		public bool mAggroDistanceFontHeavy = true;
		public bool AggroDistanceFontHeavy
		{
			get { return mAggroDistanceFontHeavy; }
			set { mAggroDistanceFontHeavy = value; }
		}

		public int mAggroDistanceFontAlignment = 7;
		public int AggroDistanceFontAlignment
		{
			get { return mAggroDistanceFontAlignment; }
			set { mAggroDistanceFontAlignment = value; }
		}

		public Vector2 mAggroDistanceTextPosition = new( 130f, 0f );
		public Vector2 AggroDistanceTextPosition
		{
			get { return mAggroDistanceTextPosition; }
			set { mAggroDistanceTextPosition = value; }
		}

		public Vector4 mAggroDistanceTextColor = new( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
		public Vector4 AggroDistanceTextColor
		{
			get { return mAggroDistanceTextColor; }
			set { mAggroDistanceTextColor = value; }
		}

		public Vector4 mAggroDistanceTextEdgeColor = new( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 AggroDistanceTextEdgeColor
		{
			get { return mAggroDistanceTextEdgeColor; }
			set { mAggroDistanceTextEdgeColor = value; }
		}

		public Vector4 mAggroDistanceCautionTextColor = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 AggroDistanceCautionTextColor
		{
			get { return mAggroDistanceCautionTextColor; }
			set { mAggroDistanceCautionTextColor = value; }
		}

		public Vector4 mAggroDistanceCautionTextEdgeColor = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 AggroDistanceCautionTextEdgeColor
		{
			get { return mAggroDistanceCautionTextEdgeColor; }
			set { mAggroDistanceCautionTextEdgeColor = value; }
		}

		public Vector4 mAggroDistanceWarningTextColor = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
		public Vector4 AggroDistanceWarningTextColor
		{
			get { return mAggroDistanceWarningTextColor; }
			set { mAggroDistanceWarningTextColor = value; }
		}

		public Vector4 mAggroDistanceWarningTextEdgeColor = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 AggroDistanceWarningTextEdgeColor
		{
			get { return mAggroDistanceWarningTextEdgeColor; }
			set { mAggroDistanceWarningTextEdgeColor = value; }
		}

		public int mAggroCautionDistance_Yalms = 6;
		public int AggroCautionDistance_Yalms
		{
			get { return mAggroCautionDistance_Yalms; }
			set { mAggroCautionDistance_Yalms = value; }
		}

		public int mAggroWarningDistance_Yalms = 3;
		public int AggroWarningDistance_Yalms
		{
			get { return mAggroWarningDistance_Yalms; }
			set { mAggroWarningDistance_Yalms = value; }
		}

		public bool mDrawAggroArc = false;
		public bool DrawAggroArc
		{
			get { return mDrawAggroArc; }
			set { mDrawAggroArc = value; }
		}

		public int mAggroArcLength_Deg = 8;
		public int AggroArcLength_Deg
		{
			get { return mAggroArcLength_Deg; }
			set { mAggroArcLength_Deg = value; }
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

		public NameplateConfig NameplateDistancesConfig { get; protected set; } = new();
		public List<DistanceWidgetConfig> DistanceWidgetConfigs { get; protected set; } = new();

		[NonSerialized]
		protected DalamudPluginInterface mPluginInterface;

		public int Version { get; set; } = 0;
	}
}
