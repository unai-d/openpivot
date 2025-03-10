using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unai.OpenPivot;

[Flags]
public enum PivSegmentLayoutFlags
{
	SkipSegmentType = 1, // 1
	SkipStatic = 0b10, // 2
	SkipColor = 0b100, // 4
	SkipAlphaChannel = 0b1000, // 8
	Unknown16 = 0b0001_0000, // 16
	SkipMeshFill = 0b0010_0000, // 32
	SkipThickness = 0b0100_0000, // 64
	SkipSecondColor = 0b1000_0000, // 128
}

public enum PivSegmentType
{
	Line = 0,
	CircleWhiteFill = 1,
	Image = 2,
	CircleFill = 3,
	Circle = 4,
	Text = 5,
	SquareLine = 6,
}

public class PivFigure
{
	public int SegmentCount { get; private set; } = 0;

	public static PivFigure DefaultFigure => new()
	{
		SegmentCount = 12,
	};

	public PivFigure()
	{

	}

	public PivFigure(BinaryReader br)
	{
		Load(br);
	}

	public void Load(BinaryReader br)
	{
		var kind = br.ReadByte();
		var kindFlags = (PivSegmentLayoutFlags)kind;
		var segmentCount = br.ReadUInt16();
		SegmentCount = segmentCount;
		Console.Error.WriteLine($"  [fig] type=0x{kind:x}({kind:b8}) {kindFlags} seg#={segmentCount}");

		// var firstSegmentOff = br.BaseStream.Position;
		// var segmentSize = kind switch
		// {
		// 	0x7a => 27,
		// 	// 0xa0 => 26, // overreads 1
		// 	0xb2 => 23,
		// 	0xb8 => 23,
		// 	0xb9 => 22,
		// 	0xba => 22, // untested
		// 	0xbb => 21,
		// 	0xbc => 20,
		// 	0xbe => 19, // overreads 1
		// 	0xbf => 18, // overreads 1
		// 	0xfe => 27,
		// 	_ => 0,
		// 	// _ => throw new NotImplementedException($"Figure data layout type 0x{kind:x2} not implemented."),
		// };

		PivSegmentType firstSegType = 0;

		for (int segIdx = 0; segIdx < segmentCount; segIdx++)
		{
			// if (segmentSize != 0) br.BaseStream.Position = firstSegmentOff + (segmentSize * segIdx);

			Console.Error.WriteLine(Utils.GetBufferHexString(br, 32));

			var parent = br.ReadUInt16();
			var child = (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipMeshFill)) ? br.ReadUInt16() : 0; // unclear
			var segLength = br.ReadSingle();
			var angle = (br.ReadDouble() / Math.PI) * 180;
			var thickness = br.ReadSingle();

			var segType = (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipSegmentType)) ? (PivSegmentType)br.ReadByte() : 0;
			var segStatic = (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipStatic)) ? br.ReadByte() > 0 : false;
			byte red = 0, green = 0, blue = 0, invAlpha = 0;
			if (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipColor))
			{
				red = br.ReadByte();
				green = br.ReadByte();
				blue = br.ReadByte();
				invAlpha = (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipAlphaChannel)) ? br.ReadByte() : (byte)0;
				if (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipSecondColor))
				{
					var hasSecondColor = br.ReadByte() > 0;
					if (hasSecondColor) br.ReadBytes((!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipAlphaChannel)) ? 4 : 3);
				}
			}

			if (segType == PivSegmentType.Image || segType == PivSegmentType.Text)
			{
				var imageIndex = br.ReadUInt16();
				var backwards = br.ReadByte() > 0;
				var mirror = br.ReadByte() > 0;
			}

			if (segIdx == 0) firstSegType = segType;

			Console.Error.WriteLine($"    [seg{segIdx}] parent={parent} len={segLength:N2} angle={angle:N2} thick={thickness:N2} type={segType} static={segStatic} col=rgba({red:x2}{green:x2}{blue:x2}{invAlpha:x2})");
		}

		Console.Error.WriteLine(Utils.GetBufferHexString(br, 32));
		
		// bends
		var bendCount = br.ReadUInt16();
		Console.Error.WriteLine($"    bend#={bendCount}");
		if (bendCount > 0)
		{
			for (int edIdx = 0; edIdx < bendCount; edIdx++)
			{
				var bendSegIdx = br.ReadUInt16();
				var bendAngle = (br.ReadDouble() / Math.PI) * 180;
				Console.Error.WriteLine($"    {bendSegIdx} bend={bendAngle}");
			}
		}

		// image data
		if (firstSegType == PivSegmentType.Image)
		{
			var imageCount = br.ReadUInt16();
			Console.Error.WriteLine($"    img#={imageCount}");
		}

		// text data
		if (firstSegType == PivSegmentType.Text)
		{
			Console.Error.WriteLine(Utils.GetBufferHexString(br, 32));
			var unk1 = br.ReadByte();
			var unk2 = br.ReadByte();
			Console.Error.WriteLine($"    {unk1:x2} {unk2:x2}");

			if (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipSecondColor)) // why?
			{
				br.ReadByte();
			}
			var fontName = br.ReadPivLEString();
			var boldItaFlags = br.ReadByte(); // bold = 1, italics = 2
			var unk3 = br.ReadUInt32();
			var text = br.ReadPivLEString();
			Console.Error.WriteLine($"    font='{fontName}' text='{text}' bif=0x{boldItaFlags:x2} {unk3:x4}");

			// TODO: some parts contain unknown data. expect errors.
			var uniqueChars = br.ReadByte();
			List<uint> charPaths = new();
			for (int c = 0; c < uniqueChars; c++)
			{
				Console.Error.WriteLine(Utils.GetBufferHexString(br, 32));

				// stuff like newlines, spaces and tabs don't render anything and thus don't have path data (runlen = 0).
				var runlen = br.ReadUInt32();
				charPaths.Add(runlen);

				byte local16 = 0;
				List<byte> local8 = Enumerable.Repeat((byte)0, (int)(runlen * 4)).ToList();
				int i = 0;
				if (runlen > 0)
				{
					do
					{
						var local15 = br.ReadByte();
						for (int j = 0; (i < runlen && (j < 8)); j += 4)
						{
							local16 = (byte)((0xf << ((byte)j & 0x1f) & (uint)local15) >> ((byte)j & 0x1f));
							local8[i] = local16;
							if (local16 == 2)
							{
								i += 3;
							}
							else
							{
								i += 1;
							}
						}
					}
					while (i < runlen);
				}
				i = 0;
				if (runlen > 0)
				{
					do
					{
						// Console.WriteLine($"      {i}/{runlen}");
						switch (local8[i])
						{
							case 0: // moveto x y
								br.ReadDouble();
								break;

							case 1: // lineto x y
								br.ReadDouble();
								break;

							case 2: // curveto c1x c1y c2x c2y x y
								br.ReadBytes(8 * 3);
								i += 2;
								break;

							case 3: // close
								break;

							case 4: // vmoveto
							case 5: // hmoveto
								br.ReadSingle();
								break;
						}
						i++;
					}
					while (i < runlen);
				}
			}

			for (int i = 0; i < text.Length; i++)
			{
				Console.Error.WriteLine(Utils.GetBufferHexString(br, 32));
				Console.Error.WriteLine($"    {i}/{text.Length}");
				if (!char.IsWhiteSpace(text[i]))
				{
					var v1 = br.ReadSingle();
					var v2 = br.ReadSingle();
					Console.Error.WriteLine($"    {v1} {v2}");
				}
				else
				{
					Console.Error.WriteLine($"    skip");
				}
			}

			br.ReadBytes(2);

			if (kindFlags.HasFlag(PivSegmentLayoutFlags.SkipThickness))
			{
				var outlineWidth = br.ReadSingle() * 200;
				br.ReadUInt32();
			}
		}
		else
		{
			br.ReadBytes(2);
		}

		Console.Error.WriteLine(Utils.GetBufferHexString(br, 32));

		var figName = br.ReadPivString();
		Console.Error.WriteLine($"    figName='{figName}'");
	}
}