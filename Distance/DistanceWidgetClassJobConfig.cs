using System;
using System.Collections.Generic;
using System.Numerics;

using Distance.Services;

using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using System.Linq;
using System.Runtime.Serialization;
using Dalamud.Utility;

namespace Distance;

public class DistanceWidgetClassJobConfig
{
	public bool ShowDistanceForClassJob( UInt32 classJob )
	{
		return	classJob > 0 &&
				classJob < mApplicableClassJobsArray.Length &&
				mApplicableClassJobsArray[classJob] == true;
	}

	internal bool[] ApplicableClassJobsArray => mApplicableClassJobsArray;
	
	internal static SortedDictionary<UInt32, ClassJobData> ClassJobDict
	{
		get
		{
			if( _mClassJobDict == null )
			{
				_mClassJobDict = new();
				Lumina.Excel.ExcelSheet<ClassJob> classJobSheet = Service.DataManager.GetExcelSheet<ClassJob>();
				for( uint i = 0; i < classJobSheet.RowCount; ++i )
				{
					var row = classJobSheet.GetRow( i );
					if( row != null )
					{
						ClassJobData.ClassJobSortCategory sortCategory;
						if( row.UIPriority == 0 ) sortCategory = ClassJobData.ClassJobSortCategory.Other;
						else if( row.UIPriority <= 10 ) sortCategory = row.JobIndex > 0 ? ClassJobData.ClassJobSortCategory.Job_Tank : ClassJobData.ClassJobSortCategory.Class;
						else if( row.UIPriority <= 20 ) sortCategory = row.JobIndex > 0 ? ClassJobData.ClassJobSortCategory.Job_Healer : ClassJobData.ClassJobSortCategory.Class;
						else if( row.UIPriority <= 30 ) sortCategory = row.JobIndex > 0 ? ClassJobData.ClassJobSortCategory.Job_Melee : ClassJobData.ClassJobSortCategory.Class;
						else if( row.UIPriority <= 40 ) sortCategory = row.JobIndex > 0 ? ClassJobData.ClassJobSortCategory.Job_Ranged : ClassJobData.ClassJobSortCategory.Class;
						else if( row.UIPriority <= 50 ) sortCategory = row.JobIndex > 0 ? ClassJobData.ClassJobSortCategory.Job_Caster : ClassJobData.ClassJobSortCategory.Class;
						else sortCategory = row.JobIndex > 0 ? ClassJobData.ClassJobSortCategory.HandLand : ClassJobData.ClassJobSortCategory.HandLand;

						_mClassJobDict.TryAdd( i,
							new ClassJobData
							{
								Abbreviation = row.Abbreviation,
								DefaultSelected = row.DohDolJobIndex < 0,
								SortCategory = sortCategory,
							} );
					}
				}
			}

			return _mClassJobDict;
		}
	}

	public DistanceWidgetClassJobConfig()
	{
		mApplicableClassJobsArray = new bool[ClassJobDict.Count];
		for( int i = 0; i < mApplicableClassJobsArray.Length; ++i ) mApplicableClassJobsArray[i] = ClassJobDict[(uint)i].DefaultSelected;
	}

	[OnDeserialized]
	internal void ValidateClassJobData( StreamingContext s )
	{
		if( mApplicableClassJobsArray?.Length != ClassJobDict.Count )
		{
			bool[] newArray = new bool[ClassJobDict.Count];
			try
			{
				mApplicableClassJobsArray?.CopyTo( newArray, 0 );
			}
			catch( Exception e )
			{
				for( int i = 0; i < mApplicableClassJobsArray.Length; ++i ) newArray[i] = ClassJobDict[(uint)i].DefaultSelected;
				Service.PluginLog.Warning( $"Exception while validating ClassJob filters; using defaults for all ClassJobs:\r\n{e}" );
			}
			mApplicableClassJobsArray = newArray;
		}
	}

	[NonSerialized]
	private static SortedDictionary<UInt32, ClassJobData> _mClassJobDict = null;

	public bool[] mApplicableClassJobsArray = null;

	internal struct ClassJobData
	{
		internal string Abbreviation;
		internal bool DefaultSelected;
		internal ClassJobSortCategory SortCategory;

		internal enum ClassJobSortCategory
		{
			Job_Tank,
			Job_Healer,
			Job_Melee,
			Job_Ranged,
			Job_Caster,
			Class,
			HandLand,
			Other,
		};
	}
}
