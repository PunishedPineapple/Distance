using System.Numerics;

using Dalamud.Utility;

namespace Distance;

public class DistanceArcConfig
{
	internal string DisplayedArcName => ArcName.IsNullOrWhitespace() ?
										ApplicableTargetCategory == TargetCategory.Targets ? ApplicableTargetType.GetTranslatedName() : ApplicableTargetCategory.GetTranslatedName() :
										ArcName;

	internal bool WithinDisplayRangeOfArc( float distanceFromArc_Yalms )
	{
		bool inRange;

		if( InvertFading )
		{
			inRange =	distanceFromArc_Yalms < -FadeoutThresholdInner_Yalms ||
						distanceFromArc_Yalms > FadeoutThresholdOuter_Yalms;
		}
		else
		{

			inRange =	distanceFromArc_Yalms > -( FadeoutThresholdInner_Yalms + FadeoutIntervalInner_Yalms ) &&
						distanceFromArc_Yalms < FadeoutThresholdOuter_Yalms + FadeoutIntervalOuter_Yalms;
		}

		return inRange;
	}

	internal (Vector4,Vector4) GetColors( float distanceFromArc_Yalms )
	{
		Vector4 color = Color;
		Vector4 edgeColor = EdgeColor;

		if( UseDistanceBasedColor )
		{
			if( distanceFromArc_Yalms < 0 )
			{
				if( distanceFromArc_Yalms <= -InnerFarThresholdDistance_Yalms )
				{
					color = InnerFarThresholdColor;
					edgeColor = InnerFarThresholdEdgeColor;
				}
				else if( distanceFromArc_Yalms <= -InnerNearThresholdDistance_Yalms )
				{
					color = InnerNearThresholdColor;
					edgeColor = InnerNearThresholdEdgeColor;
				}
			}
			else
			{
				if( distanceFromArc_Yalms >= OuterFarThresholdDistance_Yalms )
				{
					color = OuterFarThresholdColor;
					edgeColor = OuterFarThresholdEdgeColor;
				}
				else if( distanceFromArc_Yalms >= OuterNearThresholdDistance_Yalms )
				{
					color = OuterNearThresholdColor;
					edgeColor = OuterNearThresholdEdgeColor;
				}
			}
		}

		float fadeAlphaGain = 1f;
		if( distanceFromArc_Yalms < 0 )
		{
			fadeAlphaGain = MathUtils.LinearInterpolation( -FadeoutThresholdInner_Yalms - FadeoutIntervalInner_Yalms, 0f, -FadeoutThresholdInner_Yalms, 1f, distanceFromArc_Yalms );
		}
		else
		{
			fadeAlphaGain = MathUtils.LinearInterpolation( FadeoutThresholdOuter_Yalms, 1f, FadeoutThresholdOuter_Yalms + FadeoutIntervalOuter_Yalms, 0f, distanceFromArc_Yalms );
		}

		if( InvertFading )
		{
			fadeAlphaGain = 1f - fadeAlphaGain;
		}
		
		color.W *= fadeAlphaGain; ;
		edgeColor.W *= fadeAlphaGain;

		return ( color, edgeColor );
	}

	#region Options

	public string ArcName = "";
	public bool Enabled = true;
	public bool AllEnemiesShowAggressive = true;
	public bool AllEnemiesShowUnaggressive = true;
	public bool AllPlayersShowParty = true;
	public bool AllPlayersShowAlliance = false;
	public bool AllPlayersShowOthers = false;
	public bool ShowDeadObjects = false;
	public bool DistanceIsToRing = true;
	public float ArcRadius_Yalms = 3.5f;
	public float FadeoutThresholdInner_Yalms = 3f;
	public float FadeoutIntervalInner_Yalms = 3f;
	public float FadeoutThresholdOuter_Yalms = 10f;
	public float FadeoutIntervalOuter_Yalms = 5f;
	public bool InvertFading = false;
	public bool ShowPip = true;
	public float ArcLength = 2f;
	public bool ArcLengthIsYalms = true;
	public int SelfTargetedArcAzimuth_Deg = 0;
	public bool SelfTargetedArcCameraRelative = false;
	public Vector4 Color = new( (float)0xFF / 255f, (float)0xF8 / 255f, (float)0xB0 / 255f, (float)0xFF / 255f );
	public Vector4 EdgeColor = new( (float)0x63 / 255f, (float)0x4F / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public bool UseDistanceBasedColor = false;
	public Vector4 InnerNearThresholdColor = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 InnerNearThresholdEdgeColor = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 InnerFarThresholdColor = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
	public Vector4 InnerFarThresholdEdgeColor = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 OuterNearThresholdColor = new( (float)0xFF / 255f, (float)0xB3 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 OuterNearThresholdEdgeColor = new( (float)0x66 / 255f, (float)0x43 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public Vector4 OuterFarThresholdColor = new( (float)0xEF / 255f, (float)0x48 / 255f, (float)0x12 / 255f, (float)0xFF / 255f );
	public Vector4 OuterFarThresholdEdgeColor = new( (float)0x4E / 255f, (float)0x11 / 255f, (float)0x00 / 255f, (float)0xFF / 255f );
	public float InnerFarThresholdDistance_Yalms = 3f;
	public float InnerNearThresholdDistance_Yalms = 1f;
	public float OuterFarThresholdDistance_Yalms = 3f;
	public float OuterNearThresholdDistance_Yalms = 1f;

	public int mApplicableTargetCategory = (int)TargetCategory.Targets;
	internal TargetCategory ApplicableTargetCategory
	{
		get { return (TargetCategory)mApplicableTargetCategory; }
		set { mApplicableTargetCategory = (int)value; }
	}

	public int mApplicableTargetType = (int)TargetType.Target_And_Soft_Target;
	internal TargetType ApplicableTargetType
	{
		get { return (TargetType)mApplicableTargetType; }
		set { mApplicableTargetType = (int)value; }
	}

	public DistanceWidgetFiltersConfig Filters { get; protected set; } = new();

	#endregion
}
