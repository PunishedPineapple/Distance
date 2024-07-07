using System;
using System.Collections.Generic;

using Dalamud.Utility;

using Lumina.Excel.GeneratedSheets;

namespace Distance;

internal class ClassJobUtils
{
	internal static SortedDictionary<int, ClassJobData> ClassJobDict
	{
		get
		{
			if( mClassJobDict == null )
			{
				mClassJobDict = new();
				Lumina.Excel.ExcelSheet<ClassJob> classJobSheet_En = Service.DataManager.GetExcelSheet<ClassJob>( Dalamud.Game.ClientLanguage.English );
				Lumina.Excel.ExcelSheet<ClassJob> classJobSheet_Local = Service.DataManager.GetExcelSheet<ClassJob>();
				for( int i = 0; i < classJobSheet_En.RowCount; ++i )
				{
					var row = classJobSheet_En.GetRow( (uint)i );
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

						//	This needs to be unique (for use as a key).
						string abbreviation_En = row.Abbreviation;
						if( abbreviation_En.IsNullOrWhitespace() ) abbreviation_En = $"UNK{i}";

						mClassJobDict.TryAdd( i,
							new ClassJobData
							{
								Abbreviation = classJobSheet_Local.GetRow( (uint)i )?.Abbreviation ?? "",
								Abbreviation_En = abbreviation_En,
								DefaultSelected = row.DohDolJobIndex < 0,
								SortCategory = sortCategory,
							} );
					}
				}
			}

			return mClassJobDict;
		}
	}

	private static SortedDictionary<int, ClassJobData> mClassJobDict = null;
}
