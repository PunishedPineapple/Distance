using CheapLoc;

namespace Distance;

internal enum GeneralSettingsCategory : int
{
	AggroWidget,
	AggroArcs,
	AggroData,
	Nameplates,
	Miscellaneous,
}

internal static class GeneralSettingsCategoryExtensions
{
	internal static string GetTranslatedName( this GeneralSettingsCategory category )
	{
		return category switch
		{
			GeneralSettingsCategory.AggroWidget => Loc.Localize( "Config Category: Aggro Widget", "Aggro Widget Settings" ),
			GeneralSettingsCategory.AggroArcs => Loc.Localize( "Config Category: Aggro Arcs", "Aggro Arc Settings" ),
			GeneralSettingsCategory.AggroData => Loc.Localize( "Config Category: Aggro Data", "Aggro Distance Data" ),
			GeneralSettingsCategory.Nameplates => Loc.Localize( "Config Category: Nameplates", "Nameplate Settings" ),
			GeneralSettingsCategory.Miscellaneous => Loc.Localize( "Config Category: Miscellaneous", "Miscellaneous Settings" ),
			_ => "You should never see this!",
		};
	}
}