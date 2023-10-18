using CheapLoc;

namespace Distance
{
	public enum TargetCategory : int
	{
		Targets = 0,
		Self = 1,
		AllBNpc = 2,
	}

	public static class TargetCategoryExtensions
	{
		public static string GetTranslatedName( this TargetCategory targetCategory )
		{
			return targetCategory switch
			{
				TargetCategory.Targets => Loc.Localize( "Terminology: Target Category - Targets", "Targets" ),
				TargetCategory.Self => Loc.Localize( "Terminology: Target Category - Self", "Yourself" ),
				TargetCategory.AllBNpc => Loc.Localize( "Terminology: Target Category - All BNpc", "All Combatant NPCs" ),
				_ => "You should never see this!",
			};
		}
	}
}
