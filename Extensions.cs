using System.IO;
using System.Text;

namespace Unai.OpenPivot;

public static class Extensions
{
	public static string ReadPivString(this BinaryReader br)
	{
		var length = br.ReadByte();
		return Encoding.UTF8.GetString(br.ReadBytes(length));
	}

	public static string ReadPivLEString(this BinaryReader br)
	{
		var length = br.ReadUInt32();
		if (length > 512) throw new InvalidDataException($"Sanity check failed. String length too long ({length}).");
		return Encoding.Unicode.GetString(br.ReadBytes((int)(length * 2)));
	}
}