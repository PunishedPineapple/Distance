using System;

using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Distance;

internal static unsafe class TargetResolver
{
	internal static void Init()
	{
	}

	internal static void Uninit()
	{
	}

	internal static IGameObject GetTarget( TargetType targetType )
	{
		return targetType switch
		{
			TargetType.Target_And_Soft_Target => Service.TargetManager.SoftTarget ?? Service.TargetManager.Target,
			TargetType.FocusTarget => Service.TargetManager.FocusTarget,
			TargetType.MouseOver_And_UIMouseOver_Target => GetUIMouseoverTarget() ?? Service.TargetManager.MouseOverTarget,
			TargetType.Target => Service.TargetManager.Target,
			TargetType.SoftTarget => Service.TargetManager.SoftTarget,
			TargetType.MouseOverTarget => Service.TargetManager.MouseOverTarget,
			TargetType.UIMouseOverTarget => GetUIMouseoverTarget(),
			TargetType.TargetOfTarget => GetTargetOfTarget(),
			_ => throw new Exception( $"Request to resolve unknown target type: \"{targetType}\"." ),
		};
	}

	private static IGameObject GetTargetOfTarget()
	{
		var target = Service.TargetManager.SoftTarget ?? Service.TargetManager.Target;
		if( target != null && target.TargetObjectId != 0xE000000 )
		{
			return target.TargetObject;
		}
		else
		{
			return null;
		}
	}

	private static IGameObject GetUIMouseoverTarget()
	{
		if( PronounModule.Instance() != null )
		{
			var pActor = (IntPtr)PronounModule.Instance()->UiMouseOverTarget;
			if( pActor != IntPtr.Zero )
			{
				return Service.ObjectTable.CreateObjectReference( (IntPtr)pActor );
			}
		}

		return null;
	}
}
