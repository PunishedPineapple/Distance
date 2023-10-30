using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Configuration;
using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Distance;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
	#region Interface

	public void Initialize( DalamudPluginInterface pluginInterface )
	{
		mPluginInterface = pluginInterface;
	}

	public void Save()
	{
		mPluginInterface.SavePluginConfig( this );
	}
	public int Version { get; set; } = 0;

	[NonSerialized]
	private DalamudPluginInterface mPluginInterface;

	#endregion

	#region Options

	//	Need a real field (not just a property) available for use with ImGui.
	public bool SuppressCommandLineResponses = false;
	public bool AutoUpdateAggroData = true;
	public bool ShowAggroDistance = false;
	public bool ShowUnitsOnAggroDistance = true;
	public int AggroDistanceDecimalPrecision = 2;
	public int AggroDistanceFontSize = 16;
	public bool AggroDistanceFontHeavy = true;
	public Vector2 AggroDistanceTextPosition = new( 130f, 0f );
	public Vector4 AggroDistanceTextColor = new( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
	public Vector4 AggroDistanceTextEdgeColor = new( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 AggroDistanceCautionTextColor = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 AggroDistanceCautionTextEdgeColor = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 AggroDistanceWarningTextColor = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
	public Vector4 AggroDistanceWarningTextEdgeColor = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public float AggroCautionDistance_Yalms = 6f;
	public float AggroWarningDistance_Yalms = 3f;
	public bool DrawAggroArc = true;
	public int AggroArcLength_Deg = 8;

	public int mAggroDistanceApplicableTargetType = (int)TargetType.Target_And_Soft_Target;
	internal TargetType AggroDistanceApplicableTargetType
	{
		get { return (TargetType)mAggroDistanceApplicableTargetType; }
		set { mAggroDistanceApplicableTargetType = (int)value; }
	}

	public int mAggroDistanceUIAttachType = (int)AddonAttachType.Auto;
	internal AddonAttachType AggroDistanceUIAttachType
	{
		get { return (AddonAttachType)mAggroDistanceUIAttachType; }
		set { mAggroDistanceUIAttachType = (int)value; }
	}

	public int mAggroDistanceFontAlignment = (int)AlignmentType.Bottom;
	internal AlignmentType AggroDistanceFontAlignment
	{
		get { return (AlignmentType)mAggroDistanceFontAlignment; }
		set { mAggroDistanceFontAlignment = (int)value; }
	}

	public NameplateConfig NameplateDistancesConfig { get; private set; } = new();
	public List<DistanceWidgetConfig> DistanceWidgetConfigs { get; private set; } = new();
	public List<DistanceArcConfig> DistanceArcConfigs { get; private set; } = new();

	#endregion
}
