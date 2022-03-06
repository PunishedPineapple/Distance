using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Distance
{
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

		public Vector2 mTextPosition = Vector2.One;
		public Vector2 TextPosition
		{
			get { return mTextPosition; }
			set { mTextPosition = value; }
		}

		public int mApplicableTargetType;	//	Backing field as an int to work with ImGui.
		public Distance.Plugin.TargetType ApplicableTargetType
		{
			get { return (Distance.Plugin.TargetType) mApplicableTargetType; }
			set { mApplicableTargetType = (int)value; }
		}

		public bool mTargetIncludesSoftTarget = true;
		public bool TargetIncludesSoftTarget
		{
			get { return mTargetIncludesSoftTarget; }
			set { mTargetIncludesSoftTarget = value; }
		}

		public bool mMouseoverTargetFollowsMouse = false;
		public bool MouseoverTargetFollowsMouse
		{
			get { return mMouseoverTargetFollowsMouse; }
			set { mMouseoverTargetFollowsMouse = value; }
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

		public int mFontSize = 16;
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

		public Vector4 mTextColor = new Vector4( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
		public Vector4 TextColor
		{
			get { return mTextColor; }
			set { mTextColor = value; }
		}

		public Vector4 mTextEdgeColor = new Vector4( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
		public Vector4 TextEdgeColor
		{
			get { return mTextEdgeColor; }
			set { mTextEdgeColor = value; }
		}

		public bool mTrackTargetBarTextColor = false;
		public bool TrackTargetBarTextColor
		{
			get { return mTrackTargetBarTextColor; }
			set { mTrackTargetBarTextColor = value; }
		}

		public DistanceWidgetFiltersConfig Filters { get; protected set; } = new DistanceWidgetFiltersConfig();
	}
}
