using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unai.OpenPivot;

public class PivFigure
{
	public static PivFigure DefaultFigure => new()
	{
		Segments =
		[
			new(),
			new(0, 32, Utils.ToRadians(-90), 14),
			new(1, 32, Utils.ToRadians(-90), 14),
			new(2, 33, Utils.ToRadians(-90), 14),
			new(3, 20, Utils.ToRadians(90), 20) { SegmentType = PivSegmentType.CircleWhiteFill, Static = true }, // head
			new(2, 38, Utils.ToRadians(135), 14),
			new(2, 38, Utils.ToRadians(45), 14),
			new(5, 40, Utils.ToRadians(120), 14),
			new(6, 40, Utils.ToRadians(60), 14),
			new(0, 50, Utils.ToRadians(112.5), 14),
			new(0, 50, Utils.ToRadians(67.5), 14),
			new(9, 50, Utils.ToRadians(112.5), 14),
			new(10, 50, Utils.ToRadians(67.5), 14),
		]
	};

	public List<PivSegment> Segments { get; private set; } =
	[
		new() // Root
	];

	public string Name { get; set; } = null;
	public Dictionary<int, double> BendAngles { get; set; } = null;

	public List<object> UnknownList { get; private set; } = [];

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
		Logger.Debug($"  [fig] type=0x{kind:x}({kind:b8}) {kindFlags} seg#={segmentCount}");
		Logger.Debug($"    [seg0] root");

		PivSegmentType firstSegType = 0;

		for (int segIdx = 1; segIdx <= segmentCount; segIdx++)
		{
			Logger.Trace(Utils.GetBufferHexString(br, 32));

			PivSegment pivSeg = new();
			Segments.Add(pivSeg);

			pivSeg.ParentIndex = br.ReadUInt16();
			pivSeg.Index = (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipMeshFill)) ? br.ReadUInt16() : segIdx;
			pivSeg.Length = br.ReadSingle();
			pivSeg.Angle = br.ReadDouble();
			pivSeg.Thickness = br.ReadSingle();

			pivSeg.SegmentType = (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipSegmentType)) ? (PivSegmentType)br.ReadByte() : 0;
			pivSeg.Static = (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipStatic)) && br.ReadByte() > 0;
			
			byte red = 0, green = 0, blue = 0, invAlpha = 0;
			if (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipColor))
			{
				red = br.ReadByte();
				green = br.ReadByte();
				blue = br.ReadByte();
				invAlpha = (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipAlphaChannel)) ? br.ReadByte() : (byte)0;
				pivSeg.Color = new(red / 256f, green / 256f, blue / 256f, 1 - (invAlpha / 256f));
			}

			if (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipSecondColor))
			{
				var hasSecondColor = br.ReadByte();
				if (hasSecondColor > 1) throw new InvalidDataException("Invalid boolean value.");
				// if (hasSecondColor > 0) br.ReadBytes((!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipAlphaChannel)) ? 4 : 3);
				if (hasSecondColor > 0) br.ReadBytes(4);
			}

			if (pivSeg.SegmentType == PivSegmentType.Image || pivSeg.SegmentType == PivSegmentType.Text)
			{
				var imageIndex = br.ReadUInt16();
				var backwards = br.ReadByte() > 0;
				var mirror = br.ReadByte() > 0;
			}

			if (segIdx == 0) firstSegType = pivSeg.SegmentType;

			Logger.Debug($"    [seg{segIdx}] idx={pivSeg.Index} parent={pivSeg.ParentIndex} len={pivSeg.Length:N2} angle={Utils.ToDegrees(pivSeg.Angle):N2} thick={pivSeg.Thickness:N2} type={pivSeg.SegmentType} static={pivSeg.Static} col=rgba({red:x2}{green:x2}{blue:x2}{invAlpha:x2})");
		}

		Logger.Trace(Utils.GetBufferHexString(br, 32));
		
		// bends
		var bendCount = br.ReadUInt16();
		Logger.Debug($"    bend#={bendCount}");
		if (bendCount > segmentCount)
		{
			throw new InvalidDataException("Bend value count is bigger than the number of segments.");
		}
		if (bendCount > 0)
		{
			BendAngles = [];
			for (int edIdx = 0; edIdx < bendCount; edIdx++)
			{
				var bendSegIdx = br.ReadUInt16();
				var bendAngle = Utils.ToDegrees(br.ReadDouble());
				Logger.Debug($"      {bendSegIdx} bend={bendAngle}");
				BendAngles.Add(bendSegIdx, bendAngle);
			}
		}

		// image data
		if (firstSegType == PivSegmentType.Image)
		{
			var imageCount = br.ReadUInt16();
			Logger.Debug($"    img#={imageCount}");
			if (imageCount > 0)
			{
				throw new NotImplementedException("Sprites are not implemented yet.");
			}
		}

		// text data
		if (firstSegType == PivSegmentType.Text)
		{
			Logger.Trace(Utils.GetBufferHexString(br, 32));
			var unk1 = br.ReadByte();
			var unk2 = br.ReadByte();
			Logger.Debug($"    {unk1:x2} {unk2:x2}");

			if (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipSecondColor)) // why?
			{
				br.ReadByte();
			}
			var fontName = br.ReadPivLEString();
			var boldItaFlags = br.ReadByte(); // bold = 1, italics = 2
			var unk3 = br.ReadUInt32();
			var text = br.ReadPivLEString();
			Logger.Debug($"    font='{fontName}' text='{text}' bif=0x{boldItaFlags:x2} {unk3:x4}");

			var uniqueChars = br.ReadByte();
			List<uint> charPaths = [];
			for (int c = 0; c < uniqueChars; c++)
			{
				Logger.Trace(Utils.GetBufferHexString(br, 32));

				// stuff like newlines, spaces and tabs don't render anything and thus don't have path data (instructionCount = 0).
				var instructionCount = br.ReadUInt32();
				charPaths.Add(instructionCount);

				List<byte> pathOpCodes = Enumerable.Repeat((byte)0, (int)instructionCount * 4).ToList();
				
				for (int i = 0; i < instructionCount; i++)
				{
					var opCodePair = br.ReadByte();
					for (int j = 0; i < instructionCount && (j < 8); j += 4)
					{
						byte pathOpCode = (byte)((0xf << ((byte)j & 0x1f) & (uint)opCodePair) >> ((byte)j & 0x1f));
						pathOpCodes[i] = pathOpCode;
						if (pathOpCode == 2) i += 2;
					}
				}

				// TODO: store path data somewhere
				for (int i = 0; i < instructionCount; i++)
				{
					switch (pathOpCodes[i])
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
				}
			}

			for (int i = 0; i < text.Length; i++)
			{
				Logger.Trace(Utils.GetBufferHexString(br, 32));
				Logger.Debug($"    {i}/{text.Length}");
				if (!char.IsWhiteSpace(text[i]))
				{
					var v1 = br.ReadSingle();
					var v2 = br.ReadSingle();
					Logger.Debug($"    {v1} {v2}");
				}
				else
				{
					Logger.Debug($"    skip");
				}
			}
		}

		// polygon data
		var polygonCount = br.ReadUInt16();
		Logger.Debug($"    poly#={polygonCount}");
		if (polygonCount > 0)
		{
			for (int polyIdx = 0; polyIdx < polygonCount; polyIdx++)
			{
				Logger.Trace(Utils.GetBufferHexString(br, 32));
				var numVertices = br.ReadUInt16();
				br.ReadUInt32(); // rgba
				for (int i = 0; i < numVertices; i++)
				{
					var vertexValue = br.ReadUInt16();
					Logger.Debug($"      [poly{polyIdx}] [vert{i}] {vertexValue}");
				}
			}
		}

		if (kindFlags.HasFlag(PivSegmentLayoutFlags.SkipThickness))
		{
			var outlineWidth = br.ReadSingle() * 200;
			br.ReadUInt32();
		}

		Logger.Trace(Utils.GetBufferHexString(br, 32));

		if (!kindFlags.HasFlag(PivSegmentLayoutFlags.Unknown16))
		{
			for (int i = 1; i <= segmentCount; i++)
			{
				var unk = br.ReadUInt16();
				Logger.Debug($"    [seg{i}] ?={unk}");
				UnknownList.Add(unk);
			}
		}

		Name = br.ReadPivString();
		Logger.Debug($"    figName='{Name}'");
	}

	public PivSegment GetSegment(int index)
	{
		return Segments.FirstOrDefault(s => s.Index == index) ?? Segments[index];
	}

	public int GetArrayIndexOfSegmentIndex(int index)
	{
		return Segments.FindIndex(s => s.Index == index);
	}
}
