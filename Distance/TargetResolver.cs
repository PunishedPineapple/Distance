using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;

namespace Distance;

internal static unsafe class TargetResolver
{
	internal static void Init()
	{
		IntPtr fpUIMouseover = Service.SigScanner.ScanText( "E8 ?? ?? ?? ?? 48 8B 5C 24 40 4C 8B 74 24 58 83 FD 02" );
		if( fpUIMouseover != IntPtr.Zero )
		{
			Service.PluginLog.Information( $"UIMouseover function signature found at 0x{fpUIMouseover:X}." );
			mUIMouseoverHook = Service.GameInteropProvider.HookFromAddress<UIMouseoverDelegate>(fpUIMouseover, UIMouseoverDetour);
			mUIMouseoverHook.Enable();
		}
		else
		{
			throw new Exception( "Unable to find the specified function signature for UI mouseover." );
		}
	}

	internal static void Uninit()
	{
		mUIMouseoverHook?.Disable();
		mUIMouseoverHook?.Dispose();
		mUIMouseoverHook = null;
	}

	internal static GameObject GetTarget( TargetType targetType )
	{
		return targetType switch
		{
			TargetType.Target_And_Soft_Target => Service.TargetManager.SoftTarget ?? Service.TargetManager.Target,
			TargetType.FocusTarget => Service.TargetManager.FocusTarget,
			TargetType.MouseOver_And_UIMouseOver_Target => mUIMouseoverTarget ?? Service.TargetManager.MouseOverTarget,
			TargetType.Target => Service.TargetManager.Target,
			TargetType.SoftTarget => Service.TargetManager.SoftTarget,
			TargetType.MouseOverTarget => Service.TargetManager.MouseOverTarget,
			TargetType.UIMouseOverTarget => mUIMouseoverTarget,
			TargetType.TargetOfTarget => GetTargetOfTarget(),
			_ => throw new Exception( $"Request to resolve unknown target type: \"{targetType}\"." ),
		};
	}

	private static GameObject GetTargetOfTarget()
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

	private static void UIMouseoverDetour( IntPtr pThis, IntPtr pActor )
	{
		mUIMouseoverHook.Original( pThis, pActor );

		if( pActor != IntPtr.Zero )
		{
			mUIMouseoverTarget = pActor != IntPtr.Zero ? Service.ObjectTable.CreateObjectReference( pActor ) : null;
		}
		else
		{
			mUIMouseoverTarget = null;
		}
	}

	private delegate void UIMouseoverDelegate( IntPtr pThis, IntPtr pActor );
	private static Hook<UIMouseoverDelegate> mUIMouseoverHook;
	private static GameObject mUIMouseoverTarget = null;
}
