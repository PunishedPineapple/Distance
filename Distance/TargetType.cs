using CheapLoc;

namespace Distance;

public enum TargetType : int
{
	Target = 0,
	SoftTarget = 1,
	FocusTarget = 2,
	MouseOver_And_UIMouseOver_Target = 3,
	MouseOverTarget = 4,
	UIMouseOverTarget = 5,
	Target_And_Soft_Target = 6,
	TargetOfTarget = 7,
}

public static class TargetTypeExtensions
{
	public static string GetTranslatedName( this TargetType targetType )
	{
		return targetType switch
		{
			TargetType.Target => Loc.Localize( "Terminology: Target Type - Target", "Target" ),
			TargetType.SoftTarget => Loc.Localize( "Terminology: Target Type - Soft Target", "Soft Target" ),
			TargetType.FocusTarget => Loc.Localize( "Terminology: Target Type - Focus Target", "Focus Target" ),
			TargetType.MouseOver_And_UIMouseOver_Target => Loc.Localize( "Terminology: Target Type - Mouseover and UIMouseover Target", "Mouseover Target" ),
			TargetType.MouseOverTarget => Loc.Localize( "Terminology: Target Type - Mouseover Target", "Field Mouseover Target" ),
			TargetType.UIMouseOverTarget => Loc.Localize( "Terminology: Target Type - UIMouseover Target", "UI Mouseover Target" ),
			TargetType.Target_And_Soft_Target => Loc.Localize( "Terminology: Target Type - Target and Soft Target", "Target + Soft Target" ),
			TargetType.TargetOfTarget => Loc.Localize( "Terminology: Target Type - Target of Target", "Target of Target" ),
			_ => "You should never see this!",
		};
	}
}
