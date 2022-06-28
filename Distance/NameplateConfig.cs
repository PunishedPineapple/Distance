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

		/*public Vector2 mAggroDistanceTextPosition = new( 130f, 0f );
		public Vector2 AggroDistanceTextPosition
		{
			get { return mAggroDistanceTextPosition; }
			set { mAggroDistanceTextPosition = value; }
		}*/

		public bool mFiltersAreExclusive = false;
		public bool FiltersAreExclusive
		{
			get { return mFiltersAreExclusive; }
			set { mFiltersAreExclusive = value; }
		}

		public DistanceWidgetFiltersConfig Filters { get; protected set; } = new DistanceWidgetFiltersConfig();
	}
}
