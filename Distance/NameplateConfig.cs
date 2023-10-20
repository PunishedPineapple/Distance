using System.Numerics;

namespace Distance
{
	public class NameplateConfig
	{
		public bool mShowNameplateDistances = true;
		public bool ShowNameplateDistances
		{
			get { return mShowNameplateDistances; }
			set { mShowNameplateDistances = value; }
		}

		public bool mShowAll = false;
		public bool ShowAll
		{
			get { return mShowAll; }
			set { mShowAll = value; }
		}

		public bool mShowTarget = true;
		public bool ShowTarget
		{
			get { return mShowTarget; }
			set { mShowTarget = value; }
		}

		public bool mShowSoftTarget = true;
		public bool ShowSoftTarget
		{
			get { return mShowSoftTarget; }
			set { mShowSoftTarget = value; }
		}

		public bool mShowFocusTarget = true;
		public bool ShowFocusTarget
		{
			get { return mShowFocusTarget; }
			set { mShowFocusTarget = value; }
		}

		public bool mShowMouseoverTarget = false;
		public bool ShowMouseoverTarget
		{
			get { return mShowMouseoverTarget; }
			set { mShowMouseoverTarget = value; }
		}

		public bool mShowAggressive = true;
		public bool ShowAggressive
		{
			get { return mShowAggressive; }
			set { mShowAggressive = value; }
		}

		public bool mShowPartyMembers = false;
		public bool ShowPartyMembers
		{
			get { return mShowPartyMembers; }
			set { mShowPartyMembers = value; }
		}

		public bool mShowAllianceMembers = false;
		public bool ShowAllianceMembers
		{
			get { return mShowAllianceMembers; }
			set { mShowAllianceMembers = value; }
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

		public bool mDistanceIsToRing = true;
		public bool DistanceIsToRing
		{
			get { return mDistanceIsToRing; }
			set { mDistanceIsToRing = value; }
		}

		public bool mShowUnitsOnDistance = true;
		public bool ShowUnitsOnDistance
		{
			get { return mShowUnitsOnDistance; }
			set { mShowUnitsOnDistance = value; }
		}

		public int mDistanceDecimalPrecision = 0;
		public int DistanceDecimalPrecision
		{
			get { return mDistanceDecimalPrecision; }
			set { mDistanceDecimalPrecision = value; }
		}

		public bool mAllowNegativeDistances = false;
		public bool AllowNegativeDistances
		{
			get { return mAllowNegativeDistances; }
			set { mAllowNegativeDistances = value; }
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

		public int mDistanceFontAlignment = 8;
		public int DistanceFontAlignment
		{
			get { return mDistanceFontAlignment; }
			set { mDistanceFontAlignment = value; }
		}

		public bool mAutomaticallyAlignText = true;
		public bool AutomaticallyAlignText
		{
			get { return mAutomaticallyAlignText; }
			set { mAutomaticallyAlignText = value; }
		}

		public bool mPlaceTextBelowName = false;
		public bool PlaceTextBelowName
		{
			get { return mPlaceTextBelowName; }
			set { mPlaceTextBelowName = value; }
		}

		public Vector2 mDistanceTextOffset = Vector2.Zero;
		public Vector2 DistanceTextOffset
		{
			get { return mDistanceTextOffset; }
			set { mDistanceTextOffset = value; }
		}

		public bool mFiltersAreExclusive = false;
		public bool FiltersAreExclusive
		{
			get { return mFiltersAreExclusive; }
			set { mFiltersAreExclusive = value; }
		}

		public bool mUseDistanceBasedColor_BNpc = false;
		public bool UseDistanceBasedColor_BNpc
		{
			get { return mUseDistanceBasedColor_BNpc; }
			set { mUseDistanceBasedColor_BNpc = value; }
		}

		public bool mNearRangeTextUseNameplateColor_BNpc = false;
		public bool NearRangeTextUseNameplateColor_BNpc
		{
			get { return mNearRangeTextUseNameplateColor_BNpc; }
			set { mNearRangeTextUseNameplateColor_BNpc = value; }
		}

		public Vector4 mNearRangeTextColor_BNpc = new( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
		public Vector4 NearRangeTextColor_BNpc
		{
			get { return mNearRangeTextColor_BNpc; }
			set { mNearRangeTextColor_BNpc = value; }
		}

		public Vector4 mNearRangeTextEdgeColor_BNpc = new( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 NearRangeTextEdgeColor_BNpc
		{
			get { return mNearRangeTextEdgeColor_BNpc; }
			set { mNearRangeTextEdgeColor_BNpc = value; }
		}

		public bool mMidRangeTextUseNameplateColor_BNpc = false;
		public bool MidRangeTextUseNameplateColor_BNpc
		{
			get { return mMidRangeTextUseNameplateColor_BNpc; }
			set { mMidRangeTextUseNameplateColor_BNpc = value; }
		}

		public Vector4 mMidRangeTextColor_BNpc = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 MidRangeTextColor_BNpc
		{
			get { return mMidRangeTextColor_BNpc; }
			set { mMidRangeTextColor_BNpc = value; }
		}

		public Vector4 mMidRangeTextEdgeColor_BNpc = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 MidRangeTextEdgeColor_BNpc
		{
			get { return mMidRangeTextEdgeColor_BNpc; }
			set { mMidRangeTextEdgeColor_BNpc = value; }
		}

		public bool mFarRangeTextUseNameplateColor_BNpc = false;
		public bool FarRangeTextUseNameplateColor_BNpc
		{
			get { return mFarRangeTextUseNameplateColor_BNpc; }
			set { mFarRangeTextUseNameplateColor_BNpc = value; }
		}

		public Vector4 mFarRangeTextColor_BNpc = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
		public Vector4 FarRangeTextColor_BNpc
		{
			get { return mFarRangeTextColor_BNpc; }
			set { mFarRangeTextColor_BNpc = value; }
		}

		public Vector4 mFarRangeTextEdgeColor_BNpc = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 FarRangeTextEdgeColor_BNpc
		{
			get { return mFarRangeTextEdgeColor_BNpc; }
			set { mFarRangeTextEdgeColor_BNpc = value; }
		}

		public float mFarThresholdDistance_BNpc_Yalms = 3.5f;
		public float FarThresholdDistance_BNpc_Yalms
		{
			get { return mFarThresholdDistance_BNpc_Yalms; }
			set { mFarThresholdDistance_BNpc_Yalms = value; }
		}

		public float mNearThresholdDistance_BNpc_Yalms = 3f;
		public float NearThresholdDistance_BNpc_Yalms
		{
			get { return mNearThresholdDistance_BNpc_Yalms; }
			set { mNearThresholdDistance_BNpc_Yalms = value; }
		}

		public bool mUseDistanceBasedColor_Party = false;
		public bool UseDistanceBasedColor_Party
		{
			get { return mUseDistanceBasedColor_Party; }
			set { mUseDistanceBasedColor_Party = value; }
		}

		public bool mNearRangeTextUseNameplateColor_Party = false;
		public bool NearRangeTextUseNameplateColor_Party
		{
			get { return mNearRangeTextUseNameplateColor_Party; }
			set { mNearRangeTextUseNameplateColor_Party = value; }
		}

		public Vector4 mNearRangeTextColor_Party = new( (float)0xB0 / 255f, (float)0xFF / 255f, (float)0xBA / 255f, (float)0xFF / 255f );
		public Vector4 NearRangeTextColor_Party
		{
			get { return mNearRangeTextColor_Party; }
			set { mNearRangeTextColor_Party = value; }
		}

		public Vector4 mNearRangeTextEdgeColor_Party = new( (float)0x00 / 255f, (float)0x63 / 255f, (float)0x0C / 255f, (float)0xFF / 255f );
		public Vector4 NearRangeTextEdgeColor_Party
		{
			get { return mNearRangeTextEdgeColor_Party; }
			set { mNearRangeTextEdgeColor_Party = value; }
		}

		public bool mMidRangeTextUseNameplateColor_Party = false;
		public bool MidRangeTextUseNameplateColor_Party
		{
			get { return mMidRangeTextUseNameplateColor_Party; }
			set { mMidRangeTextUseNameplateColor_Party = value; }
		}

		public Vector4 mMidRangeTextColor_Party = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 MidRangeTextColor_Party
		{
			get { return mMidRangeTextColor_Party; }
			set { mMidRangeTextColor_Party = value; }
		}

		public Vector4 mMidRangeTextEdgeColor_Party = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 MidRangeTextEdgeColor_Party
		{
			get { return mMidRangeTextEdgeColor_Party; }
			set { mMidRangeTextEdgeColor_Party = value; }
		}

		public bool mFarRangeTextUseNameplateColor_Party = false;
		public bool FarRangeTextUseNameplateColor_Party
		{
			get { return mFarRangeTextUseNameplateColor_Party; }
			set { mFarRangeTextUseNameplateColor_Party = value; }
		}

		public Vector4 mFarRangeTextColor_Party = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
		public Vector4 FarRangeTextColor_Party
		{
			get { return mFarRangeTextColor_Party; }
			set { mFarRangeTextColor_Party = value; }
		}

		public Vector4 mFarRangeTextEdgeColor_Party = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 FarRangeTextEdgeColor_Party
		{
			get { return mFarRangeTextEdgeColor_Party; }
			set { mFarRangeTextEdgeColor_Party = value; }
		}

		public float mFarThresholdDistance_Party_Yalms = 14.5f;
		public float FarThresholdDistance_Party_Yalms
		{
			get { return mFarThresholdDistance_Party_Yalms; }
			set { mFarThresholdDistance_Party_Yalms = value; }
		}

		public float mNearThresholdDistance_Party_Yalms = 13.5f;
		public float NearThresholdDistance_Party_Yalms
		{
			get { return mNearThresholdDistance_Party_Yalms; }
			set { mNearThresholdDistance_Party_Yalms = value; }
		}

		public DistanceWidgetFiltersConfig Filters { get; protected set; } = new DistanceWidgetFiltersConfig();
	}
}
