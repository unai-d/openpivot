using System.IO;
using System.Linq;

namespace Unai.OpenPivot;

public static class Utils
{
	public static string GetBufferHexString(BinaryReader br, int count = 4)
	{
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
}
