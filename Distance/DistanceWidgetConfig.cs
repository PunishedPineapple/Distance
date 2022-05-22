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

		public Vector2 mTextPosition = new( -200, 30 );
		public Vector2 TextPosition
		{
			get { return mTextPosition; }
			set { mTextPosition = value; }
		}

		//	Backing field as an int to work with ImGui.
		public int mApplicableTargetType = (int)Distance.Plugin.TargetType.Target;
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

		//	Backing field as an int to work with ImGui.
		public int mUIAttachType = (int)Distance.Plugin.WidgetUIAttachType.Auto;
		public Distance.Plugin.WidgetUIAttachType UIAttachType
		{
			get { return (Distance.Plugin.WidgetUIAttachType)mUIAttachType; }
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

		public DistanceWidgetFiltersConfig Filters { get; protected set; } = new DistanceWidgetFiltersConfig();

		public GameAddonEnum GetGameAddonToUse()
		{
			if( UIAttachType == Plugin.WidgetUIAttachType.ScreenText )
			{
				return GameAddonEnum.ScreenText;
			}
			else if( UIAttachType == Plugin.WidgetUIAttachType.Cursor )
			{
				return GameAddonEnum.ScreenText;
			}
			else if( UIAttachType == Plugin.WidgetUIAttachType.Target )
			{
				return GameAddonEnum.TargetBar;
			}
			else if( UIAttachType == Plugin.WidgetUIAttachType.FocusTarget )
			{
				return GameAddonEnum.FocusTargetBar;
			}
			/*else if( UIAttachType == Plugin.WidgetUIAttachType.Nameplate )
			{
				return GameAddonToUse.Nameplate;
			}*/
			else
			{
				if( ApplicableTargetType == Plugin.TargetType.Target || ApplicableTargetType == Plugin.TargetType.SoftTarget )
				{
					return GameAddonEnum.TargetBar;
				}
				else if( ApplicableTargetType == Plugin.TargetType.FocusTarget )
				{
					return GameAddonEnum.FocusTargetBar;
				}
				else
				{
					return GameAddonEnum.ScreenText;
				}
			}
		}
	}
}
