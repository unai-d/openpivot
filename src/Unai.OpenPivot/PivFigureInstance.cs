using System.Collections.Generic;
using System.Numerics;

namespace Unai.OpenPivot;

public class PivFigureInstance
{
	public int FigureIndex { get; set; } = 0;
	public Vector2 Position { get; set; } = Vector2.Zero;
	public double Scale { get; set; } = 1;
	public List<PivSegmentOverrides> SegmentOverrides { get; } = [ null ];
}
