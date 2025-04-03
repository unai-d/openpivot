using System.Numerics;

namespace Unai.OpenPivot;

public enum PivBackroundType
{
	PNG,
	JPEG,
	SolidColor,
	Gradient,
}

public class PivBackground
{
	public PivBackroundType Type { get; set; } = PivBackroundType.SolidColor;
	public Vector4 Color { get; set; } = Vector4.One;
	public Vector4 SecondColor { get; set; } = Vector4.Zero;
	public Vector2 GradientStart { get; set; } = Vector2.Zero;
	public Vector2 GradientEnd { get; set; } = Vector2.UnitY;
	public byte[] ImageData { get; set; } = null;
}
