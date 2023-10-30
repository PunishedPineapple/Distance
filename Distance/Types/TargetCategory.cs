using CheapLoc;

namespace Distance;

public enum TargetCategory : int
{
	Targets = 0,
	Self = 1,
	AllEnemies = 2,
	AllPlayers = 3,
}

public static class TargetCategoryExtensions
{
	public static string GetTranslatedName( this TargetCategory targetCategory )
	{
		return targetCategory switch
		{
			TargetCategory.Targets => Loc.Localize( "Terminology: Target Category - Targets", "Targets" ),
			TargetCategory.Self => Loc.Localize( "Terminology: Target Category - Self", "Yourself" ),
			TargetCategory.AllEnemies => Loc.Localize( "Terminology: Target Category - All Enemies", "All Enemies" ),
			TargetCategory.AllPlayers => Loc.Localize( "Terminology: Target Category - All Players", "All Players" ),
			_ => "You should never see this!",
		};
	}
}
