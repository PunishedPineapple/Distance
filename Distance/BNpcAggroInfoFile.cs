using System;
using System.Collections.Generic;
using System.IO;

namespace Distance;

internal class BNpcAggroInfoFile
{
	internal bool ReadFromFile( string filePath )
	{
		if( !File.Exists( filePath ) ) return false;
		string fileText = File.ReadAllText( filePath );
		return ReadFromString( fileText );
	}

	internal bool ReadFromString( string data )
	{
		FileLoaded = false;
		mAggroInfoList.Clear();

		string[] lines = data.Split( new[]{"\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );

		for( int i = 0; i < lines.Length; ++i )
		{
			string[] tokens = lines[i].Split( '=', StringSplitOptions.TrimEntries );

			//	The first line will be the version of the data.  The following lines should all be data entries.
			if( tokens.Length == 2 && tokens[0].ToLowerInvariant() == "version" && TryParseVersionString( tokens[1], out mFileVersion ) ) continue;
			else if( tokens.Length != 4 ) throw new InvalidDataException( $"Line {i} contained an unexpected number of entries." );

			if( !UInt32.TryParse( tokens[0], out UInt32 territoryType ) ) throw new InvalidDataException( $"Unparsable TerritoryType at line {i}" );
			if( !UInt32.TryParse( tokens[1], out UInt32 BNpcID ) ) throw new InvalidDataException( $"Unparsable BNpc ID at line {i}" );
			if( !float.TryParse( tokens[2], out float aggroRange_Yalms ) ) throw new InvalidDataException( $"Unparsable aggro range at line {i}" );

			mAggroInfoList.Add( new BNpcAggroEntity()
			{
				TerritoryType = territoryType,
				BNpcID = BNpcID,
				AggroDistance_Yalms = aggroRange_Yalms,
				EnglishName = tokens[3],
			} );
		}

		FileLoaded = true;
		return true;
	}

	internal void WriteFile( string filePath )
	{
		List<string> lines = new();
		
		if( VersionNumberIsFormattable( FileVersion ) )
		{
			lines.Add( $"Version = {GetFileVersionAsString()}" );
		}
		
		foreach( var entry in mAggroInfoList)
		{
			lines.Add( $"{entry.TerritoryType}={entry.BNpcID}={entry.AggroDistance_Yalms}={entry.EnglishName}" );
		}

		File.WriteAllLines( filePath, lines );
	}

	internal List<BNpcAggroEntity> GetEntries()
	{
		return new( mAggroInfoList );
	}

	internal bool FileLoaded { get; private set; } = false;
	protected readonly List<BNpcAggroEntity> mAggroInfoList = new();

	protected UInt64 mFileVersion = 0;
	internal UInt64 FileVersion
	{
		get { return mFileVersion; }
		private set { mFileVersion = value; }
	}

	internal string GetFileVersionAsString()
	{
		return GetFileVersionAsString( FileVersion );
	}

	//	The version number is formatted like "2022.01.25.0000.0000-000" on disk, where the decimal-separated
	//	digits are the gamever, and the final three numbers are the revision of our data for that gamever.
	internal static string GetFileVersionAsString( UInt64 version )
	{
		if( version == 0 )
		{
			return "Unknown Version";
		}
		else if( !VersionNumberIsFormattable( version ) )
		{
			return "Invalid Version Format";
		}
		else
		{
			string str = version.ToString();
			str = str.Insert( 16, "-" );
			str = str.Insert( 12, "." );
			str = str.Insert( 8, "." );
			str = str.Insert( 6, "." );
			str = str.Insert( 4, "." );
			return str;
		}
	}

	private static bool VersionNumberIsFormattable( UInt64 version )
	{
		return	version >= 1000_00_00_0000_0000_000 &&
				version <= 9999_99_99_9999_9999_999;
	}

	private static bool TryParseVersionString( string str, out UInt64 versionNumber )
	{
		return UInt64.TryParse( str.Trim().Replace( ".", "" ).Replace( "-", "" ), out versionNumber );
	}
}
