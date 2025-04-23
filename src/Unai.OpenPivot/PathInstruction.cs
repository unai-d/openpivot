namespace Unai.OpenPivot;

public enum PathInstruction
{
	Unknown = -1,
	MoveTo = 0,
	LineTo,
	CurveTo,
	Close,
	VerticalMoveTo,
	HorizontallyMoveTo,
}
