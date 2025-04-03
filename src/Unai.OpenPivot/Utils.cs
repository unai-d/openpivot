using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Unai.OpenPivot;

public static class Utils
{
	public static string GetBufferHexString(BinaryReader br, int count = 4)
	{
		if (!br.BaseStream.CanSeek) return null;

		var ofs = br.BaseStream.Position;

		var buf = br.ReadBytes(count);
		var bufHex = string.Join(' ', buf.Select(x => x.ToString("x2")));
		var bufAscii = string.Join("", buf.Select(x => char.IsBetween((char)x, ' ', '\x7e') ? (char)x : '.'));
		string ret = $"{ofs:x8}  {bufHex}  |{bufAscii}|";

		br.BaseStream.Position = ofs;

		return ret;
	}

	public static string ToHex(byte[] buf)
	{
		return string.Join("", buf.Select(x => x.ToString("x2")));
	}

	public static double ToDegrees(double radians)
	{
		return radians / Math.PI * 180;
	}

	public static double ToRadians(double degrees)
	{
		return degrees / 180 * Math.PI;
	}

	public static Vector2 VectorFromLengthAngle(double length, double angle)
	{
		return new((float)(length * Math.Cos(angle)), (float)(length * Math.Sin(angle)));
	}

	public static Vector4 RgbaToVector4(byte r, byte g, byte b, byte a)
	{
		return new(r / 256f, g / 256f, b / 256f, a / 256f);
	}
}
