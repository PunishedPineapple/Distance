using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Distance
{
	public struct TextNodeDrawData
	{
		public short PositionX;
		public short PositionY;

		public bool UseDepth;

		public byte Alpha;

		public byte TextColorA;
		public byte TextColorR;
		public byte TextColorG;
		public byte TextColorB;

		public byte EdgeColorA;
		public byte EdgeColorR;
		public byte EdgeColorG;
		public byte EdgeColorB;

		public byte FontSize;
		public byte AlignmentFontType;
		public byte LineSpacing;
		public byte CharSpacing;

		public static readonly TextNodeDrawData Default = new TextNodeDrawData()
		{
			PositionX = 1,
			PositionY = 1,
			UseDepth = false,
			Alpha = 255,
			TextColorA = 255,
			TextColorR = 255,
			TextColorG = 255,
			TextColorB = 255,
			EdgeColorA = 255,
			EdgeColorR = 255,
			EdgeColorG = 255,
			EdgeColorB = 255,
			FontSize = 12,
			AlignmentFontType = (byte)AlignmentType.TopLeft,
			LineSpacing = 24,
			CharSpacing = 1
		};
	}
}
