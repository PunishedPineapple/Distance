using System;
using System.Collections.Generic;

using Lumina.Excel.GeneratedSheets;

namespace Distance;

internal class ClassJobUtils
{
	internal static SortedDictionary<UInt32, ClassJobData> ClassJobDict
	{
		get
		{
			if( mClassJobDict == null )
			{
				mClassJobDict = new();
				Lumina.Excel.ExcelSheet<ClassJob> classJobSheet = Service.DataManager.GetExcelSheet<ClassJob>();
				for( uint i = 0; i < classJobSheet.RowCount; ++i )
				{
					var row = classJobSheet.GetRow( i );
					if( row != null )
					{
						ClassJobSortCategory sortCategory;
						if( row.UIPriority == 0 ) sortCategory = ClassJobSortCategory.Other;
						else if( row.UIPriority <= 10 ) sortCategory = row.JobIndex > 0 ? ClassJobSortCategory.Job_Tank : ClassJobSortCategory.Class;
						else if( row.UIPriority <= 20 ) sortCategory = row.JobIndex > 0 ? ClassJobSortCategory.Job_Healer : ClassJobSortCategory.Class;
						else if( row.UIPriority <= 30 ) sortCategory = row.JobIndex > 0 ? ClassJobSortCategory.Job_Melee : ClassJobSortCategory.Class;
						else if( row.UIPriority <= 40 ) sortCategory = row.JobIndex > 0 ? ClassJobSortCategory.Job_Ranged : ClassJobSortCategory.Class;
						else if( row.UIPriority <= 50 ) sortCategory = row.JobIndex > 0 ? ClassJobSortCategory.Job_Caster : ClassJobSortCategory.Class;
						else sortCategory = row.JobIndex > 0 ? ClassJobSortCategory.HandLand : ClassJobSortCategory.HandLand;

						mClassJobDict.TryAdd( i,
							new ClassJobData
							{
								Abbreviation = row.Abbreviation,
								DefaultSelected = row.DohDolJobIndex < 0,
								SortCategory = sortCategory,
							} );
					}
				}
			}

			return mClassJobDict;
		}
	}

	private static SortedDictionary<UInt32, ClassJobData> mClassJobDict = null;
}
