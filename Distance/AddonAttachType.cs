using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CheapLoc;

namespace Distance
{
	public enum AddonAttachType : int
	{
		Auto,
		ScreenText,
		Target,
		FocusTarget,
		Cursor,
		//Nameplate
	}

	public static class AddonAttachTypeExtensions
	{
		public static string GetTranslatedName( this AddonAttachType attachType )
		{
			return attachType switch
			{
				AddonAttachType.Auto => Loc.Localize( "Terminology: UI Attach Point - Auto", "Automatic" ),
				AddonAttachType.ScreenText => Loc.Localize( "Terminology: UI Attach Point - Screen Text", "Screen Space" ),
				AddonAttachType.Target => Loc.Localize( "Terminology: UI Attach Point - Target", "Target Bar" ),
				AddonAttachType.FocusTarget => Loc.Localize( "Terminology: UI Attach Point - Focus Target", "Focus Target Bar" ),
				AddonAttachType.Cursor => Loc.Localize( "Terminology: UI Attach Point - Mouse Cursor", "Mouse Cursor" ),
				//WidgetUIAttachType.Nameplate => Loc.Localize( "Terminology: UI Attach Point - Nameplate", "Target Nameplate" ),
				_ => "You should never see this!",
			};
		}


		public static GameAddonEnum GetGameAddonToUse( this AddonAttachType attachType, TargetType widgetTargetType )
		{
			if( attachType == AddonAttachType.ScreenText )
			{
				return GameAddonEnum.ScreenText;
			}
			else if( attachType == AddonAttachType.Cursor )
			{
				return GameAddonEnum.ScreenText;
			}
			else if( attachType == AddonAttachType.Target )
			{
				return GameAddonEnum.TargetBar;
			}
			else if( attachType == AddonAttachType.FocusTarget )
			{
				return GameAddonEnum.FocusTargetBar;
			}
			/*else if( attachType == AddonAttachType.Nameplate )
			{
				return GameAddonEnum.Nameplate;
			}*/
			else /*Auto*/
			{
				if( widgetTargetType is TargetType.Target or TargetType.SoftTarget or TargetType.Target_And_Soft_Target )
				{
					return GameAddonEnum.TargetBar;
				}
				else if( widgetTargetType is TargetType.FocusTarget )
				{
					return GameAddonEnum.FocusTargetBar;
				}
				else if( widgetTargetType is TargetType.TargetOfTarget )
				{
					return GameAddonEnum.TargetBar;
				}
				else
				{
					return GameAddonEnum.ScreenText;
				}
			}
		}
	}
}
