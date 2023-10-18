using System;
using System.Collections.Generic;
using System.Numerics;

using Distance.Services;

using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using System.Linq;
using System.Runtime.Serialization;

namespace Distance
{
	public class DistanceArcConfig
	{
		public string mArcName = "";
		public string ArcName
		{
			get { return mArcName; }
			set { mArcName = value; }
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

		//	Backing field as an int to work with ImGui.
		public int mApplicableTargetCategory = (int)TargetCategory.Targets;
		public TargetCategory ApplicableTargetCategory
		{
			get { return (TargetCategory)mApplicableTargetCategory; }
			set { mApplicableTargetCategory = (int)value; }
		}

		//	Backing field as an int to work with ImGui.
		public int mApplicableTargetType = (int)TargetType.Target_And_Soft_Target;
		public TargetType ApplicableTargetType
		{
			get { return (TargetType)mApplicableTargetType; }
			set { mApplicableTargetType = (int)value; }
		}

		public bool mDistanceIsToRing = true;
		public bool DistanceIsToRing
		{
			get { return mDistanceIsToRing; }
			set { mDistanceIsToRing = value; }
		}

		public float mArcRadius_Yalms = 3.5f;
		public float ArcRadius_Yalms
		{
			get { return mArcRadius_Yalms; }
			set { mArcRadius_Yalms = value; }
		}

		public bool mShowPip = true;
		public bool ShowPip
		{
			get { return mShowPip; }
			set { mShowPip = value; }
		}

		public float mArcLength = 2f;
		public float ArcLength
		{
			get { return mArcLength; }
			set { mArcLength = value; }
		}

		public bool mArcLengthIsYalms = true;
		public bool ArcLengthIsYalms
		{
			get { return mArcLengthIsYalms; }
			set { mArcLengthIsYalms = value; }
		}

		public int mSelfTargetedArcAzimuth_Deg = 0;
		public int SelfTargetedArcAzimuth_Deg
		{
			get { return mSelfTargetedArcAzimuth_Deg; }
			set { mSelfTargetedArcAzimuth_Deg = value; }
		}

		public Vector4 mColor = new( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
		public Vector4 Color
		{
			get { return mColor; }
			set { mColor = value; }
		}

		public Vector4 mEdgeColor = new( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 EdgeColor
		{
			get { return mEdgeColor; }
			set { mEdgeColor = value; }
		}

		public bool mUseDistanceBasedColor = false;
		public bool UseDistanceBasedColor
		{
			get { return mUseDistanceBasedColor; }
			set { mUseDistanceBasedColor = value; }
		}

		public Vector4 mInnerNearThresholdColor = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 InnerNearThresholdColor
		{
			get { return mInnerNearThresholdColor; }
			set { mInnerNearThresholdColor = value; }
		}

		public Vector4 mInnerNearThresholdEdgeColor = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 InnerNearThresholdEdgeColor
		{
			get { return mInnerNearThresholdEdgeColor; }
			set { mInnerNearThresholdEdgeColor = value; }
		}

		public Vector4 mInnerFarThresholdColor = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
		public Vector4 InnerFarThresholdColor
		{
			get { return mInnerFarThresholdColor; }
			set { mInnerFarThresholdColor = value; }
		}

		public Vector4 mInnerFarThresholdEdgeColor = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 InnerFarThresholdEdgeColor
		{
			get { return mInnerFarThresholdEdgeColor; }
			set { mInnerFarThresholdEdgeColor = value; }
		}

		public Vector4 mOuterNearThresholdColor = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 OuterNearThresholdColor
		{
			get { return mOuterNearThresholdColor; }
			set { mOuterNearThresholdColor = value; }
		}

		public Vector4 mOuterNearThresholdEdgeColor = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 OuterNearThresholdEdgeColor
		{
			get { return mOuterNearThresholdEdgeColor; }
			set { mOuterNearThresholdEdgeColor = value; }
		}

		public Vector4 mOuterFarThresholdColor = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
		public Vector4 OuterFarThresholdColor
		{
			get { return mOuterFarThresholdColor; }
			set { mOuterFarThresholdColor = value; }
		}

		public Vector4 mOuterFarThresholdEdgeColor = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 OuterFarThresholdEdgeColor
		{
			get { return mOuterFarThresholdEdgeColor; }
			set { mOuterFarThresholdEdgeColor = value; }
		}

		public float mInnerFarThresholdDistance_Yalms = 3f;
		public float InnerFarThresholdDistance_Yalms
		{
			get { return mInnerFarThresholdDistance_Yalms; }
			set { mInnerFarThresholdDistance_Yalms = value; }
		}

		public float mInnerNearThresholdDistance_Yalms = 1f;
		public float InnerNearThresholdDistance_Yalms
		{
			get { return mInnerNearThresholdDistance_Yalms; }
			set { mInnerNearThresholdDistance_Yalms = value; }
		}

		public float mOuterFarThresholdDistance_Yalms = 3f;
		public float OuterFarThresholdDistance_Yalms
		{
			get { return mOuterFarThresholdDistance_Yalms; }
			set { mOuterFarThresholdDistance_Yalms = value; }
		}

		public float mOuterNearThresholdDistance_Yalms = 1f;
		public float OuterNearThresholdDistance_Yalms
		{
			get { return mOuterNearThresholdDistance_Yalms; }
			set { mOuterNearThresholdDistance_Yalms = value; }
		}

		public DistanceWidgetFiltersConfig Filters { get; protected set; } = new();
		public DistanceWidgetClassJobConfig ClassJobs { get; protected set; } = new();
	}
}
