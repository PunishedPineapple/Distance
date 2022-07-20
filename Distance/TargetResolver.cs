using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Logging;

namespace Distance
{
	internal static unsafe class TargetResolver
	{
		public static void Init( SigScanner sigScanner, TargetManager targetManager, ObjectTable objectTable )
		{
			mTargetManager = targetManager;
			mObjectTable = objectTable;

			IntPtr fpUIMouseover = sigScanner.ScanText( "E8 ?? ?? ?? ?? 48 8B 6C 24 58 48 8B 5C 24 50 4C 8B 7C" );
			if( fpUIMouseover != IntPtr.Zero )
			{
				PluginLog.LogInformation( $"UIMouseover function signature found at 0x{fpUIMouseover:X}." );
				mUIMouseoverHook = Hook<UIMouseoverDelegate>.FromAddress( fpUIMouseover, new( UIMouseoverDetour ) );
				mUIMouseoverHook.Enable();
			}
			else
			{
				throw new Exception( "Unable to find the specified function signature for UI mouseover." );
			}
		}

		public static void Uninit()
		{
			mUIMouseoverHook?.Disable();
			mUIMouseoverHook?.Dispose();
			mUIMouseoverHook = null;
			mTargetManager = null;
			mObjectTable = null;
		}

		public static GameObject GetTarget( TargetType targetType )
		{
			return targetType switch
			{
				TargetType.Target_And_Soft_Target => mTargetManager.SoftTarget ?? mTargetManager.Target,
				TargetType.FocusTarget => mTargetManager.FocusTarget,
				TargetType.MouseOver_And_UIMouseOver_Target => mUIMouseoverTarget ?? mTargetManager.MouseOverTarget,
				TargetType.Target => mTargetManager.Target,
				TargetType.SoftTarget => mTargetManager.SoftTarget,
				TargetType.MouseOverTarget => mTargetManager.MouseOverTarget,
				TargetType.UIMouseOverTarget => mUIMouseoverTarget,
				TargetType.TargetOfTarget => GetTargetOfTarget(),
				_ => throw new Exception( $"Request to resolve unknown target type: \"{targetType}\"." ),
			};
		}

		private static GameObject GetTargetOfTarget()
		{
			var target = mTargetManager.SoftTarget ?? mTargetManager.Target;
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
				mUIMouseoverTarget = pActor != IntPtr.Zero ? mObjectTable.CreateObjectReference( pActor ) : null;
			}
			else
			{
				mUIMouseoverTarget = null;
			}
		}

		private static TargetManager mTargetManager;
		private static ObjectTable mObjectTable;

		private delegate void UIMouseoverDelegate( IntPtr pThis, IntPtr pActor );
		private static Hook<UIMouseoverDelegate> mUIMouseoverHook;
		private static GameObject mUIMouseoverTarget = null;
	}
}
