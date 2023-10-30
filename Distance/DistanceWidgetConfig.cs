using System.Numerics;

namespace Distance;

public class DistanceWidgetConfig
{
	public string mWidgetName = "";
	public string WidgetName
	{
		get { return mWidgetName; }
		set { mWidgetName = value; }
	}

	public bool mEnabled = true;
	public bool Enabled
	{
		get { return mEnabled; }
		set { mEnabled = value; }
	}

	public bool mHideInCombat = false;
	public bool HideInCombat
	{
		get { return mHideInCombat; }
		set { mHideInCombat = value; }
	}

	public bool mHideOutOfCombat = false;
	public bool HideOutOfCombat
	{
		get { return mHideOutOfCombat; }
		set { mHideOutOfCombat = value; }
	}

	public bool mHideInInstance = false;
	public bool HideInInstance
	{
		get { return mHideInInstance; }
		set { mHideInInstance = value; }
	}

	public bool mHideOutOfInstance = false;
	public bool HideOutOfInstance
	{
		get { return mHideOutOfInstance; }
		set { mHideOutOfInstance = value; }
	}

	public Vector2 mTextPosition = new( -AtkNodeHelpers.DefaultTextNodeWidth, 30 );
	public Vector2 TextPosition
	{
		get { return mTextPosition; }
		set { mTextPosition = value; }
	}

	//	Backing field as an int to work with ImGui.
	public int mApplicableTargetType = (int)TargetType.Target_And_Soft_Target;
	public TargetType ApplicableTargetType
	{
		get { return (TargetType) mApplicableTargetType; }
		set { mApplicableTargetType = (int)value; }
	}

	//	Backing field as an int to work with ImGui.
	public int mUIAttachType = (int)AddonAttachType.Auto;
	internal AddonAttachType UIAttachType
	{
		get { return (AddonAttachType)mUIAttachType; }
		set { mUIAttachType = (int)value; }
	}

	public bool mDistanceIsToRing = true;
	public bool DistanceIsToRing
	{
		get { return mDistanceIsToRing; }
		set { mDistanceIsToRing = value; }
	}

	public float mDistanceOffset_Yalms = 0;
	public float DistanceOffset_Yalms
	{
		get { return mDistanceOffset_Yalms; }
		set { mDistanceOffset_Yalms = value; }
	}

	public bool mShowUnits = true;
	public bool ShowUnits
	{
		get { return mShowUnits; }
		set { mShowUnits = value; }
	}

	public bool mShowDistanceModeMarker = false;
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

	public bool mAllowNegativeDistances = false;
	public bool AllowNegativeDistances
	{
		get { return mAllowNegativeDistances; }
		set { mAllowNegativeDistances = value; }
	}

	public int mFontSize = 14;
	public int FontSize
	{
		get { return mFontSize; }
		set { mFontSize = value; }
	}

	public bool mFontHeavy = false;
	public bool FontHeavy
	{
		get { return mFontHeavy; }
		set { mFontHeavy = value; }
	}

	public int mFontAlignment = 8;
	public int FontAlignment
	{
		get { return mFontAlignment; }
		set { mFontAlignment = value; }
	}

	public Vector4 mTextColor = new( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
	public Vector4 TextColor
	{
		get { return mTextColor; }
		set { mTextColor = value; }
	}

	public Vector4 mTextEdgeColor = new( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 TextEdgeColor
	{
		get { return mTextEdgeColor; }
		set { mTextEdgeColor = value; }
	}

	public bool mTrackTargetBarTextColor = true;
	public bool TrackTargetBarTextColor
	{
		get { return mTrackTargetBarTextColor; }
		set { mTrackTargetBarTextColor = value; }
	}
	public bool mUseDistanceBasedColor = false;
	public bool UseDistanceBasedColor
	{
		get { return mUseDistanceBasedColor; }
		set { mUseDistanceBasedColor = value; }
	}

	public Vector4 mNearThresholdTextColor = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 NearThresholdTextColor
	{
		get { return mNearThresholdTextColor; }
		set { mNearThresholdTextColor = value; }
	}

	public Vector4 mNearThresholdTextEdgeColor = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 NearThresholdTextEdgeColor
	{
		get { return mNearThresholdTextEdgeColor; }
		set { mNearThresholdTextEdgeColor = value; }
	}

	public Vector4 mFarThresholdTextColor = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
	public Vector4 FarThresholdTextColor
	{
		get { return mFarThresholdTextColor; }
		set { mFarThresholdTextColor = value; }
	}

	public Vector4 mFarThresholdTextEdgeColor = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 FarThresholdTextEdgeColor
	{
		get { return mFarThresholdTextEdgeColor; }
		set { mFarThresholdTextEdgeColor = value; }
	}

	public float mFarThresholdDistance_Yalms = 3.5f;
	public float FarThresholdDistance_Yalms
	{
		get { return mFarThresholdDistance_Yalms; }
		set { mFarThresholdDistance_Yalms = value; }
	}

	public float mNearThresholdDistance_Yalms = 2f;
	public float NearThresholdDistance_Yalms
	{
		get { return mNearThresholdDistance_Yalms; }
		set { mNearThresholdDistance_Yalms = value; }
	}

	public DistanceWidgetFiltersConfig Filters { get; protected set; } = new DistanceWidgetFiltersConfig();
}
