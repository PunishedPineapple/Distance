using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Distance
{
	internal class BNpcAggroInfoFile
	{
		public bool ReadFile( string filePath )
		{
			if( !File.Exists( filePath ) ) return false;
			string[] lines = File.ReadAllLines( filePath );

			mAggroInfoList.Clear();

			for( int i = 0; i < lines.Length; ++i )
			{
				string[] tokens = lines[i].Split( '=' );
				if( tokens.Length != 4 ) throw new InvalidDataException( $"Line {i} contained an unexpected number of entries." );
				for( int j = 0; j < tokens.Length; ++j ) tokens[j] = tokens[j].Trim();

				if( !UInt32.TryParse( tokens[0], out UInt32 territoryType ) ) throw new InvalidDataException( $"Unparsable TerritoryType at line {i}" );
				if( !UInt32.TryParse( tokens[1], out UInt32 BNpcNameID ) ) throw new InvalidDataException( $"Unparsable BNpcNameID at line {i}" );
				if( !float.TryParse( tokens[2], out float aggroRange_Yalms ) ) throw new InvalidDataException( $"Unparsable aggro range at line {i}" );

				mAggroInfoList.Add( new BNpcAggroEntity()
				{
					TerritoryType = territoryType,
					NameID = BNpcNameID,
					AggroDistance_Yalms = aggroRange_Yalms,
					EnglishName = tokens[3],
				} );
			}

			return true;
		}

		public List<BNpcAggroEntity> GetEntries()
		{
			return new( mAggroInfoList );
		}

		protected readonly List<BNpcAggroEntity> mAggroInfoList = new();
	}
}
