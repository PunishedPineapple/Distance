using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Distance;
internal static class LocalizationHelpers
{
	internal static string DistanceUnitUpper =>
		Service.ClientState.ClientLanguage switch
		{
			Dalamud.Game.ClientLanguage.Japanese => "メートル",
			Dalamud.Game.ClientLanguage.English => "Yalm",
			Dalamud.Game.ClientLanguage.German => "Yalm",
			Dalamud.Game.ClientLanguage.French => "Yalm",
			_ => "Yalm"
		};

	internal static string DistanceUnitUpperPlural =>
		Service.ClientState.ClientLanguage switch
		{
			Dalamud.Game.ClientLanguage.Japanese => "メートル",
			Dalamud.Game.ClientLanguage.English => "Yalms",
			Dalamud.Game.ClientLanguage.German => "Yalms",
			Dalamud.Game.ClientLanguage.French => "Yalms",
			_ => "Yalms"
		};

	internal static string DistanceUnitLower =>
		Service.ClientState.ClientLanguage switch
		{
			Dalamud.Game.ClientLanguage.Japanese => "メートル",
			Dalamud.Game.ClientLanguage.English => "yalm",
			Dalamud.Game.ClientLanguage.German => "yalm",
			Dalamud.Game.ClientLanguage.French => "yalm",
			_ => "yalm"
		};

	internal static string DistanceUnitLowerPlural =>
		Service.ClientState.ClientLanguage switch
		{
			Dalamud.Game.ClientLanguage.Japanese => "メートル",
			Dalamud.Game.ClientLanguage.English => "yalms",
			Dalamud.Game.ClientLanguage.German => "yalms",
			Dalamud.Game.ClientLanguage.French => "yalms",
			_ => "yalms"
		};

	internal static string DistanceUnitShort =>
		Service.ClientState.ClientLanguage switch
		{
			Dalamud.Game.ClientLanguage.Japanese => "m",
			Dalamud.Game.ClientLanguage.English => "y",
			Dalamud.Game.ClientLanguage.German => "y",
			Dalamud.Game.ClientLanguage.French => "y",
			_ => "y"
		};
}
