using System;
using System.Numerics;
using SkiaSharp;

namespace Unai.OpenPivot.Gui.Uno;

public static class Utils
{
	public static SKColor Vector4ToSKColor(Vector4 v)
	{
		return new(
			(byte)Math.Clamp(v.X * 256, 0, 255),
			(byte)Math.Clamp(v.Y * 256, 0, 255),
			(byte)Math.Clamp(v.Z * 256, 0, 255),
			(byte)Math.Clamp(v.W * 256, 0, 255)
		);
	}
}
