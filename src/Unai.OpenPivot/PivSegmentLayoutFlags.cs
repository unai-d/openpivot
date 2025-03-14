using System;

namespace Unai.OpenPivot;

[Flags]
public enum PivSegmentLayoutFlags
{
	SkipSegmentType = 1, // 1
	SkipStatic = 0b10, // 2
	SkipColor = 0b100, // 4
	SkipAlphaChannel = 0b1000, // 8
	Unknown16 = 0b0001_0000, // 16
	SkipMeshFill = 0b0010_0000, // 32
	SkipThickness = 0b0100_0000, // 64
	SkipSecondColor = 0b1000_0000, // 128
}
