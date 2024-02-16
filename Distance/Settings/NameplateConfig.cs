using System.Numerics;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Distance;

public class NameplateConfig
{
	public bool ShowNameplateDistances = true;
	public bool ShowAll = false;
	public bool ShowTarget = true;
	public bool ShowSoftTarget = true;
	public bool ShowFocusTarget = true;
	public bool ShowMouseoverTarget = false;
	public bool ShowAggressive = true;
	public bool ShowPartyMembers = false;
	public bool ShowAllianceMembers = false;
	public bool DistanceIsToRing = true;
	public bool ShowUnitsOnDistance = true;
	public int DistanceDecimalPrecision = 0;
	public bool AllowNegativeDistances = false;
	public int DistanceFontSize = 14;
	public bool DistanceFontHeavy = false;
	public bool AutomaticallyAlignText = true;
	public bool PlaceTextBelowName = false;
	public Vector2 DistanceTextOffset = Vector2.Zero;
	public bool FiltersAreExclusive = false;
	public float DistanceOffset_Player_Yalms = 0;
	public float DistanceOffset_BNpc_Yalms = 0;
	public float DistanceOffset_Other_Yalms = 0;
	public bool EnableFading_BNpc = false;
	public bool InvertFading_BNpc = false;
	public float FadeoutThresholdInner_BNpc_Yalms = 10f;
	public float FadeoutIntervalInner_BNpc_Yalms = 3f;
	public float FadeoutThresholdOuter_BNpc_Yalms = 30f;
	public float FadeoutIntervalOuter_BNpc_Yalms = 5f;
	public bool EnableFading_Party = false;
	public bool InvertFading_Party = false;
	public float FadeoutThresholdInner_Party_Yalms = 10f;
	public float FadeoutIntervalInner_Party_Yalms = 3f;
	public float FadeoutThresholdOuter_Party_Yalms = 30f;
	public float FadeoutIntervalOuter_Party_Yalms = 5f;
	public bool EnableFading_Other = false;
	public bool InvertFading_Other = false;
	public float FadeoutThresholdInner_Other_Yalms = 10f;
	public float FadeoutIntervalInner_Other_Yalms = 3f;
	public float FadeoutThresholdOuter_Other_Yalms = 30f;
	public float FadeoutIntervalOuter_Other_Yalms = 5f;
	public bool UseDistanceBasedColor_BNpc = false;
	public bool NearRangeTextUseNameplateColor_BNpc = false;
	public Vector4 NearRangeTextColor_BNpc = new( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
	public Vector4 NearRangeTextEdgeColor_BNpc = new( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public bool MidRangeTextUseNameplateColor_BNpc = false;
	public Vector4 MidRangeTextColor_BNpc = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 MidRangeTextEdgeColor_BNpc = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public bool FarRangeTextUseNameplateColor_BNpc = false;
	public Vector4 FarRangeTextColor_BNpc = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
	public Vector4 FarRangeTextEdgeColor_BNpc = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public float FarThresholdDistance_BNpc_Yalms = 3.5f;
	public float NearThresholdDistance_BNpc_Yalms = 3f;
	public bool UseDistanceBasedColor_Party = false;
	public bool NearRangeTextUseNameplateColor_Party = false;
	public Vector4 NearRangeTextColor_Party = new( (float)0xB0 / 255f, (float)0xFF / 255f, (float)0xBA / 255f, (float)0xFF / 255f );
	public Vector4 NearRangeTextEdgeColor_Party = new( (float)0x00 / 255f, (float)0x63 / 255f, (float)0x0C / 255f, (float)0xFF / 255f );
	public bool MidRangeTextUseNameplateColor_Party = false;
	public Vector4 MidRangeTextColor_Party = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 MidRangeTextEdgeColor_Party = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public bool FarRangeTextUseNameplateColor_Party = false;
	public Vector4 FarRangeTextColor_Party = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
	public Vector4 FarRangeTextEdgeColor_Party = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public float FarThresholdDistance_Party_Yalms = 14.5f;
	public float NearThresholdDistance_Party_Yalms = 13.5f;

	public int mDistanceFontAlignment = (int)AlignmentType.BottomRight;
	internal AlignmentType DistanceFontAlignment
	{
		get { return (AlignmentType)mDistanceFontAlignment; }
		set { mDistanceFontAlignment = (int)value; }
	}

	public int mNameplateStyle = (int)NameplateStyle.MatchGame;
	internal NameplateStyle NameplateStyle
	{
		get { return (NameplateStyle)mNameplateStyle; }
		set { mNameplateStyle = (int)value; }
	}

	public DistanceWidgetFiltersConfig Filters { get; protected set; } = new DistanceWidgetFiltersConfig();
}
