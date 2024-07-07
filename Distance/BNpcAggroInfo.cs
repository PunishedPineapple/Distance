using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Plugin.Services;

using Lumina.Excel;

namespace Distance;

internal static class BNpcAggroInfo
{
	internal static void Init( IDataManager dataManager, string filePath  )
	{
		Task.Run( () =>
		{
			//	Read in the file.
			var file = new BNpcAggroInfoFile();
			try
			{
				Service.PluginLog.Debug( $"Trying to read aggro info file at {filePath}" );
				if( file.ReadFromFile( filePath ) )
				{
					Init( dataManager, file );
				}
				else
				{
					Service.PluginLog.Warning( $"Unable to read BNpc aggro file." );
				}
			}
			catch( Exception e )
			{
				Service.PluginLog.Warning( $"Unable to read BNpc aggro file:\r\n{e}" );
			}
		} );
	}

	internal static void Init( IDataManager dataManager, BNpcAggroInfoFile file )
	{
		if( !file.FileLoaded ) return;

		lock( mLockObj )
		{
			mKnownAggroEntities.Clear();
			mKnownAggroEntities.InsertRange( 0, file.GetEntries() );
			mLoadedInfoFile = file;
			Service.PluginLog.Information( $"Loaded BNpc aggro range data version {GetCurrentFileVersionAsString()} ({GetCurrentFileVersion()})" );

			//	Verify entries against the english name in the sheet as a sanity check.  Remove those that no longer match, or have invalid TerritoryType.
			ExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType> territorySheet = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType>();
			ExcelSheet<Lumina.Excel.GeneratedSheets.BNpcName> BNpcNameSheet = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.BNpcName>( Dalamud.Game.ClientLanguage.English );
			for( int i = mKnownAggroEntities.Count - 1; i >= 0; i-- )
			{
				if( mKnownAggroEntities[i].TerritoryType < 1 || territorySheet.GetRow( mKnownAggroEntities[i].TerritoryType ) == null )
				{
					Service.PluginLog.Debug( $"Aggro data entry removed because no such TerritoryType ID exists: {mKnownAggroEntities[i].TerritoryType}, {mKnownAggroEntities[i].BNpcID}, {mKnownAggroEntities[i].EnglishName}" );
					mKnownAggroEntities.RemoveAt( i );
				}
				else if( mKnownAggroEntities[i].BNpcID < 1 || BNpcNameSheet.GetRow( mKnownAggroEntities[i].BNpcID ) == null )
				{
					Service.PluginLog.Debug( $"Aggro data entry removed because no such BNpcName ID exists: {mKnownAggroEntities[i].TerritoryType}, {mKnownAggroEntities[i].BNpcID}, {mKnownAggroEntities[i].EnglishName}" );
					mKnownAggroEntities.RemoveAt( i );
				}
				else if( !mKnownAggroEntities[i].EnglishName.Equals( BNpcNameSheet.GetRow( mKnownAggroEntities[i].BNpcID ).Singular, StringComparison.InvariantCultureIgnoreCase ) )
				{
					//	Ignore BNpc name mismatch if it's an RSV value.  There's probably not a reliable way to handle RSV'd names without shipping
					//	manual mappings, and doing that has some issues unless they were included with Dalamud itself.  It's already extremly unlikely
					//	that a name ID ever gets reassigned anyway, and since RSVs are only for new content so far, any issues should be quickly noticed.
					if( BNpcNameSheet.GetRow( mKnownAggroEntities[i].BNpcID ).Singular.ToString().Contains( $"_rsv_{mKnownAggroEntities[i].BNpcID}" ) )
					{
						Service.PluginLog.Debug( $"Aggro data entry BNpcName mismatch ignored due to RSV: {mKnownAggroEntities[i].TerritoryType}, {mKnownAggroEntities[i].BNpcID}, {mKnownAggroEntities[i].EnglishName} (The game says \"{BNpcNameSheet.GetRow( mKnownAggroEntities[i].BNpcID ).Singular}\" is the name for this ID.)" );
					}
					else
					{
						Service.PluginLog.Debug( $"Aggro data entry removed because BNpcName mismatch: {mKnownAggroEntities[i].TerritoryType}, {mKnownAggroEntities[i].BNpcID}, {mKnownAggroEntities[i].EnglishName} (The game says \"{BNpcNameSheet.GetRow( mKnownAggroEntities[i].BNpcID ).Singular}\" is the name for this ID.)" );
						mKnownAggroEntities.RemoveAt( i );
					}
				}
			}
		}

		FilterAggroEntities( mCurrentFilteredTerritoryType, true );
	}

	internal static List<BNpcAggroEntity> GetAllAggroEntities()
	{
		lock( mLockObj )
		{
			return new( mKnownAggroEntities );
		}
	}

	internal static List<BNpcAggroEntity> GetFilteredAggroEntities( UInt32 territoryType, bool forceRefresh = false )
	{
		FilterAggroEntities( territoryType, forceRefresh );

		lock( mLockObj )
		{
			return new( mFilteredAggroEntities );
		}
	}

	internal static void FilterAggroEntities( UInt32 territoryType, bool forceRefresh = false )
	{
		lock( mLockObj )
		{
			//	If we already did this territory, skip doing the work.
			if( territoryType == mCurrentFilteredTerritoryType && !forceRefresh ) return;

			mCurrentFilteredTerritoryType = 0;
			mFilteredAggroEntities.Clear();

			var filteredEntries = mKnownAggroEntities.Where( x => x.TerritoryType == territoryType );
			if( filteredEntries.Any() )
			{
				mFilteredAggroEntities.InsertRange( 0, filteredEntries );
				mCurrentFilteredTerritoryType = territoryType;
			}
		}
	}

	internal static float? GetAggroRange( UInt32 BNpcID, UInt32 territoryType )
	{
		FilterAggroEntities( territoryType );

		lock( mLockObj )
		{
			int index = mFilteredAggroEntities.FindIndex( x => x.BNpcID == BNpcID  );
			return index >= 0 ? mFilteredAggroEntities[index].AggroDistance_Yalms : null;
		}
	}

	internal static UInt64 GetCurrentFileVersion()
	{
		return mLoadedInfoFile?.FileVersion ?? 0;
	}

	internal static string GetCurrentFileVersionAsString()
	{
		return BNpcAggroInfoFile.GetFileVersionAsString( GetCurrentFileVersion() );
	}

	private static readonly List<BNpcAggroEntity> mKnownAggroEntities = new();
	private static readonly List<BNpcAggroEntity> mFilteredAggroEntities = new();
	private static UInt32 mCurrentFilteredTerritoryType = 0;

	private static BNpcAggroInfoFile mLoadedInfoFile = null;

	private static readonly Object mLockObj = new();
}
