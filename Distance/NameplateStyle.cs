using CheapLoc;

namespace Distance;

public enum NameplateStyle : int
{
	MatchGame = 0,
	Old = 1,
	New = 2
}

public static class NameplateStyleExtensions
{
	public static string GetTranslatedName( this NameplateStyle style )
	{
		return style switch
		{
			NameplateStyle.MatchGame => Loc.Localize( "Terminology: Nameplate Style - Match Game", "Match Game" ),
			NameplateStyle.Old => Loc.Localize( "Terminology: Nameplate Style - Old", "Old" ),
			NameplateStyle.New => Loc.Localize( "Terminology: Nameplate Style", "New" ),
			_ => "You should never see this!",
		};
	}
}
