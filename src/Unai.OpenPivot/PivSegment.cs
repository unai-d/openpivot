using System;
using System.Numerics;

namespace Unai.OpenPivot;

public class PivSegment
{
	public int ParentIndex { get; set; } = -1;
	public double Length { get; set; } = 1;
	public double Angle { get; set; } = 0;
	public double Thickness { get; set; } = 8;
	public PivSegmentType SegmentType { get; set; } = PivSegmentType.Line;
	public bool Static { get; set; } = false;

	public PivSegment()
	{

	}

	public PivSegment(int parent, double length, double angle, double thickness)
	{
		ParentIndex = parent;
		Length = length;
		Angle = angle;
		Thickness = thickness;
	}

	public Vector2 GetAbsoluteStartPoint(PivFigure figure)
	{
		Vector2 ret = Vector2.Zero;

		if (ParentIndex >= 0)
		{
			ret += figure.Segments[ParentIndex].GetAbsoluteEndPoint(figure);
		}

		return ret;
	}

	public Vector2 EndPoint => Utils.VectorFromLengthAngle(Length, Angle);

	public Vector2 GetAbsoluteEndPoint(PivFigure figure)
	{
		return GetAbsoluteStartPoint(figure) + EndPoint;
	}
}
