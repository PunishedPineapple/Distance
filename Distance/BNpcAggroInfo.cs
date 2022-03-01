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
	public static class BNpcAggroInfo
	{
		public static void Init( DataManager dataManager, string filePath  )
		{
			//	Start fresh.
			mKnownAggroEntities.Clear();

			//Read in the file.
			var file = new BNpcAggroInfoFile();
			try
			{
				PluginLog.LogDebug( $"Trying to read aggro info file at {filePath}" );
				if( file.ReadFile( filePath ) )
				{
					mKnownAggroEntities.InsertRange( 0, file.GetEntries() );
				}
			}
			catch( Exception e )
			{
				PluginLog.LogWarning( $"Unable to read BNpc aggro file: {e}" );
			}

			//	Verify entries against the english name in the sheet as a sanity check.  Remove those that no longer match, or have invalid TerritoryType.
			ExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType> territorySheet = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType>();
			ExcelSheet<Lumina.Excel.GeneratedSheets.BNpcName> BNpcNameSheet = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.BNpcName>( Dalamud.ClientLanguage.English );
			for( int i = mKnownAggroEntities.Count - 1; i >= 0; i-- )
			{
				if( mKnownAggroEntities[i].NameID > 0 && mKnownAggroEntities[i].NameID < BNpcNameSheet.RowCount )
				{
					if( !mKnownAggroEntities[i].EnglishName.Equals( BNpcNameSheet.GetRow( mKnownAggroEntities[i].NameID ).Singular, StringComparison.InvariantCultureIgnoreCase ) ||
						mKnownAggroEntities[i].TerritoryType == 0 ||
						mKnownAggroEntities[i].TerritoryType > territorySheet.RowCount )
					{
						mKnownAggroEntities.RemoveAt( i );
					}
				}
			}
		}

		public static List<BNpcAggroEntity> GetAllAggroEntities()
		{
			return new( mKnownAggroEntities );
		}

		public static List<BNpcAggroEntity> GetFilteredAggroEntities( UInt32 territoryType, bool forceRefresh = false )
		{
			FilterAggroEntities( territoryType, forceRefresh );
			return new( mFilteredAggroEntities );
		}

		private static void FilterAggroEntities( UInt32 territoryType, bool forceRefresh = false )
		{
			//	If we already did this territory, skip doing the work.
			if( territoryType == mCurrentFilteredTerritoryType && !forceRefresh ) return;

			mCurrentFilteredTerritoryType = 0;
			mFilteredAggroEntities.Clear();

			var filteredEntries = mKnownAggroEntities.Where( x => x.TerritoryType == territoryType );
			if( filteredEntries.Count() > 0 )
			{
				mFilteredAggroEntities.InsertRange( 0, filteredEntries );
				mCurrentFilteredTerritoryType = territoryType;
			}
		}

		public static float? GetAggroRange( UInt32 BNpcNameID, UInt32 territoryType )
		{
			FilterAggroEntities( territoryType );

			int index = mFilteredAggroEntities.FindIndex( x => x.NameID == BNpcNameID  );
			return index >= 0 ? mFilteredAggroEntities[index].AggroDistance_Yalms : null;
		}

		private static readonly List<BNpcAggroEntity> mKnownAggroEntities = new List<BNpcAggroEntity>();
		private static readonly List<BNpcAggroEntity> mFilteredAggroEntities = new List<BNpcAggroEntity>();
		private static UInt32 mCurrentFilteredTerritoryType = 0;
	}
}
