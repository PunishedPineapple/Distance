using System;
using System.Numerics;
using System.Runtime.Serialization;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Newtonsoft.Json;

namespace Distance;

public class DistanceWidgetConfig
{
	internal string DisplayedWidgetName => WidgetName.IsNullOrWhitespace() ? ApplicableTargetType.GetTranslatedName() : WidgetName;

	public string WidgetName = "";
	public bool Enabled = true;
	public Vector2 TextPosition = new( -AtkNodeHelpers.DefaultTextNodeWidth, 30f );
	public bool DistanceIsToRing = true;
	public float DistanceOffset_Yalms = 0;
	public bool ShowUnits = true;
	public bool ShowDistanceModeMarker = false;
	public int DecimalPrecision = 2;
	public bool AllowNegativeDistances = false;
	public int FontSize = 14;
	public bool FontHeavy = false;
	public Vector4 TextColor = new( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
	public Vector4 TextEdgeColor = new( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public bool TrackTargetBarTextColor = true;
	public bool UseDistanceBasedColor = false;
	public Vector4 NearThresholdTextColor = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 NearThresholdTextEdgeColor = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 FarThresholdTextColor = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
	public Vector4 FarThresholdTextEdgeColor = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public float FarThresholdDistance_Yalms = 3.5f;
	public float NearThresholdDistance_Yalms = 2f;

	public int mApplicableTargetType = (int)TargetType.Target_And_Soft_Target;
	internal TargetType ApplicableTargetType
	{
		get { return (TargetType)mApplicableTargetType; }
		set { mApplicableTargetType = (int)value; }
	}

	public int mUIAttachType = (int)AddonAttachType.Auto;
	internal AddonAttachType UIAttachType
	{
		get { return (AddonAttachType)mUIAttachType; }
		set { mUIAttachType = (int)value; }
	}

	public int mFontAlignment = (int)AlignmentType.BottomRight;
	internal AlignmentType FontAlignment
	{
		get { return (AlignmentType)mFontAlignment; }
		set { mFontAlignment = (int)value; }
	}

	#region Old Setting Migration

	[Obsolete("Use the corresponding \"Show\" flag instead.  Only exists for old config migration.")]
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool HideInCombat = false;
	[Obsolete("Use the corresponding \"Show\" flag instead.  Only exists for old config migration.")]
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool HideOutOfCombat = false;

	[OnDeserialized]
	protected void MigrateOldSettings( StreamingContext s )
	{
		#pragma warning disable CS0618 // Type or member is obsolete
		if( HideInCombat ) Filters.ShowInCombat = false;
		if( HideOutOfCombat ) Filters.ShowOutOfCombat = false;
		HideInCombat = false;
		HideOutOfCombat = false;
		#pragma warning restore CS0618 // Type or member is obsolete
	}

	#endregion

	public DistanceWidgetFiltersConfig Filters { get; protected set; } = new DistanceWidgetFiltersConfig();
}
