using System.Collections.Generic;

namespace Unai.OpenPivot;

public class PivFrame
{
	public int BackgroundIndex { get; set; } = 0;
	public List<PivFigureInstance> FigureInstances { get; set; } = [];
}
