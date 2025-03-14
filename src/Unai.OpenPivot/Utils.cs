using System;
using System.IO;
using System.Linq;

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
}
