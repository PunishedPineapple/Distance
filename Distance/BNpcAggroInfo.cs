using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Logging;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Distance
{
	internal static class BNpcAggroInfo
	{
		public static void Init( DataManager dataManager, string filePath  )
		{
			Task.Run( () =>
			{
				//	Read in the file.
				var file = new BNpcAggroInfoFile();
				try
				{
					PluginLog.LogDebug( $"Trying to read aggro info file at {filePath}" );
					if( file.ReadFromFile( filePath ) )
					{
						Init( dataManager, file );
					}
				}
				catch( Exception e )
				{
					PluginLog.LogWarning( $"Unable to read BNpc aggro file: {e}" );
				}
			} );
		}

		public static void Init( DataManager dataManager, BNpcAggroInfoFile file )
		{
			if( !file.FileLoaded ) return;

			lock( mLockObj )
			{
				mKnownAggroEntities.Clear();
				mKnownAggroEntities.InsertRange( 0, file.GetEntries() );
				mLoadedInfoFile = file;
				PluginLog.LogInformation( $"Loaded BNpc aggro range data version {GetCurrentFileVersionAsString()} ({GetCurrentFileVersion()})" );

				//	Verify entries against the english name in the sheet as a sanity check.  Remove those that no longer match, or have invalid TerritoryType.
				ExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType> territorySheet = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType>();
				ExcelSheet<Lumina.Excel.GeneratedSheets.BNpcName> BNpcNameSheet = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.BNpcName>( Dalamud.ClientLanguage.English );
				for( int i = mKnownAggroEntities.Count - 1; i >= 0; i-- )
				{
					if( mKnownAggroEntities[i].TerritoryType < 1 || territorySheet.GetRow( mKnownAggroEntities[i].TerritoryType ) == null )
					{
						PluginLog.LogDebug( $"Aggro data entry removed because no such TerritoryType ID exists: {mKnownAggroEntities[i].TerritoryType}, {mKnownAggroEntities[i].NameID}, {mKnownAggroEntities[i].EnglishName}" );
						mKnownAggroEntities.RemoveAt( i );
					}
					else if( mKnownAggroEntities[i].NameID < 1 || BNpcNameSheet.GetRow( mKnownAggroEntities[i].NameID ) == null )
					{
						PluginLog.LogDebug( $"Aggro data entry removed because no such BNpcName ID exists: {mKnownAggroEntities[i].TerritoryType}, {mKnownAggroEntities[i].NameID}, {mKnownAggroEntities[i].EnglishName}" );
						mKnownAggroEntities.RemoveAt( i );
					}
					else if( !mKnownAggroEntities[i].EnglishName.Equals( BNpcNameSheet.GetRow( mKnownAggroEntities[i].NameID ).Singular, StringComparison.InvariantCultureIgnoreCase ) )
					{
						PluginLog.LogDebug( $"Aggro data entry removed because BNpcName Mismatch: {mKnownAggroEntities[i].TerritoryType}, {mKnownAggroEntities[i].NameID}, {mKnownAggroEntities[i].EnglishName}" );
						mKnownAggroEntities.RemoveAt( i );
					}
				}
			}

			FilterAggroEntities( mCurrentFilteredTerritoryType, true );
		}

		public static List<BNpcAggroEntity> GetAllAggroEntities()
		{
			lock( mLockObj )
			{
				return new( mKnownAggroEntities );
			}
		}

		public static List<BNpcAggroEntity> GetFilteredAggroEntities( UInt32 territoryType, bool forceRefresh = false )
		{
			FilterAggroEntities( territoryType, forceRefresh );

			lock( mLockObj )
			{
				return new( mFilteredAggroEntities );
			}
		}

		public static void FilterAggroEntities( UInt32 territoryType, bool forceRefresh = false )
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

		public static float? GetAggroRange( UInt32 BNpcNameID, UInt32 territoryType )
		{
			FilterAggroEntities( territoryType );

			lock( mLockObj )
			{
				int index = mFilteredAggroEntities.FindIndex( x => x.NameID == BNpcNameID  );
				return index >= 0 ? mFilteredAggroEntities[index].AggroDistance_Yalms : null;
			}
		}

		public static UInt64 GetCurrentFileVersion()
		{
			return mLoadedInfoFile?.FileVersion ?? 0;
		}

		public static string GetCurrentFileVersionAsString()
		{
			return BNpcAggroInfoFile.GetFileVersionAsString( GetCurrentFileVersion() );
		}

		private static readonly List<BNpcAggroEntity> mKnownAggroEntities = new();
		private static readonly List<BNpcAggroEntity> mFilteredAggroEntities = new();
		private static UInt32 mCurrentFilteredTerritoryType = 0;

		private static BNpcAggroInfoFile mLoadedInfoFile = null;

		private static readonly Object mLockObj = new();
	}
}
