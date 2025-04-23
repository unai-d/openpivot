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
				if (hasSecondColor > 1) throw new InvalidDataException($"Invalid boolean value (0x{hasSecondColor:X2}).");
				// if (hasSecondColor > 0) br.ReadBytes((!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipAlphaChannel)) ? 4 : 3);
				if (hasSecondColor > 0) br.ReadBytes(4);
			}

			if (pivSeg.SegmentType == PivSegmentType.Image || pivSeg.SegmentType == PivSegmentType.Text)
			{
				var imageIndex = br.ReadUInt16();
				var backwards = br.ReadByte() > 0;
				var mirror = br.ReadByte() > 0;
			}

			if (segIdx == 1) firstSegType = pivSeg.SegmentType;

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
				Logger.Debug($"      [bend{edIdx}] segIdx={bendSegIdx} bend={bendAngle}");
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

			// if (!kindFlags.HasFlag(PivSegmentLayoutFlags.SkipSecondColor)) // why?
			// {
			// 	br.ReadByte();
			// }

			Logger.Trace(Utils.GetBufferHexString(br, 32));
			
			var fontName = br.ReadPivLEString();
			var boldItaFlags = br.ReadByte(); // bold = 1, italics = 2
			var unk3 = br.ReadUInt32();
			Logger.Debug($"    font='{fontName}' bif=0x{boldItaFlags:x2} {unk3:x4}");

			Logger.Trace(Utils.GetBufferHexString(br, 32));
			
			var text = br.ReadPivLEString();
			Logger.Debug($"    text='{text}'");

			var uniqueChars = br.ReadByte();
			Logger.Debug($"    path#={uniqueChars}");
			for (int c = 0; c < uniqueChars; c++)
			{
				Logger.Trace(Utils.GetBufferHexString(br, 32));

				// stuff like newlines, spaces and tabs don't render anything and thus don't have path data (instructionCount = 0).
				var instructionCount = br.ReadUInt32();
				if (instructionCount > 1000) throw new InvalidDataException("Instruction count is too big.");

				Logger.Debug($"      [path{c}] instr#={instructionCount}");

				List<byte> pathOpCodes = Enumerable.Repeat((byte)255, (int)instructionCount * 4).ToList();
				
				int k = 0;
				while (k < instructionCount)
				{
					var opCodePair = br.ReadByte();
					for (int j = 0; k < instructionCount && (j < 8); j += 4)
					{
						byte pathOpCode = (byte)((0xf << ((byte)j & 0x1f) & (uint)opCodePair) >> ((byte)j & 0x1f));
						pathOpCodes[k] = pathOpCode;
						k += pathOpCode == 2 ? 3 : 1;
						Logger.Debug($"        [instr{pathOpCodes.Count - 1}] arg_off={k} op={(PathInstruction)pathOpCode}");
						if (pathOpCode > 5)
						{
							Logger.Error($"Font path instruction overrun.");
							br.BaseStream.Position++;
							k = (int)instructionCount;
							break;
						}
					}
				}

				// TODO: store path data somewhere
				for (int i = 0; i < instructionCount; i++)
				{
					float[] args = new float[6];

					switch (pathOpCodes[i])
					{
						case 0: // moveto x y
							args[0] = br.ReadSingle();
							args[1] = br.ReadSingle();
							Logger.Debug($"        [instr{i}] moveto x={args[0]} y={args[1]}");
							break;

						case 1: // lineto x y
							args[0] = br.ReadSingle();
							args[1] = br.ReadSingle();
							Logger.Debug($"        [instr{i}] lineto x={args[0]} y={args[1]}");
							break;

						case 2: // curveto c1x c1y c2x c2y x y
							args[0] = br.ReadSingle();
							args[1] = br.ReadSingle();
							args[2] = br.ReadSingle();
							args[3] = br.ReadSingle();
							args[4] = br.ReadSingle();
							args[5] = br.ReadSingle();
							i += 2;
							Logger.Debug($"        [instr{i}] curveto c1x={args[0]} c1y={args[1]} c2x={args[2]} c2y={args[3]} x={args[4]} y={args[5]}");
							break;

						case 3: // close
							Logger.Debug($"        [instr{i}] close");
							break;

						case 4: // vmoveto
							args[0] = br.ReadSingle();
							Logger.Debug($"        [instr{i}] vmoveto y={args[0]}");
							break;

						case 5: // hmoveto
							args[0] = br.ReadSingle();
							Logger.Debug($"        [instr{i}] hmoveto x={args[0]}");
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
		var absPolyVertexCount = 0;
		Logger.Debug($"    poly#={polygonCount}");
		if (polygonCount > 0)
		{
			for (int polyIdx = 0; polyIdx < polygonCount; polyIdx++)
			{
				Logger.Trace(Utils.GetBufferHexString(br, 32));
				var numVertices = br.ReadUInt16();
				var shapeColor = br.ReadUInt32(); // rgba
				for (int i = 0; i < numVertices; i++)
				{
					var vertexValue = br.ReadUInt16();
					Logger.Debug($"      [poly{polyIdx}] [vert{i}] {vertexValue}");
					absPolyVertexCount++;
				}
			}

			Logger.Debug($"      total vertex count = {absPolyVertexCount}");
		}

		if (kindFlags.HasFlag(PivSegmentLayoutFlags.SkipThickness))
		{
			var outlineWidth = br.ReadSingle() * 200;
			var eUnk4 = br.ReadUInt32();
			Logger.Debug($"    outline={outlineWidth} ?={eUnk4:x8}");
		}

		Logger.Trace(Utils.GetBufferHexString(br, 32));

		if (!kindFlags.HasFlag(PivSegmentLayoutFlags.Unknown16))
		{
			// for (int i = 0; i < segmentCount; i++) // ← works with archer.piv, run_demo.piv, tween_demo.piv, tween_pendulum.piv, etc.
			// // works with
			// //   type=0xa8(10101000) SkipAlphaChannel, SkipMeshFill, SkipSecondColor
			// //   type=0xa0(10100000) SkipMeshFill, SkipSecondColor
			// // for (int i = 0; i < segmentCount + 1; i++)
			// // for (int i = 0; i < segmentCount + 6; i++) // ← works with tween_football_bounce.piv, ~52 segments
			// {
			// 	var unk = br.ReadUInt16();
			// 	Logger.Debug($"      [seg{i}] ?={unk}");
			// 	UnknownList.Add(unk);
			// }

			// heuristic-based skipping of unknown values.
			for (int i = 0; i < segmentCount * 2; i++) // limit is based on nothing.
			{
				var unk = br.ReadUInt16();
				if (unk >= 0x0200)
				{
					br.BaseStream.Position -= 2;
					break;
				}
				Logger.Debug($"    [unk{i}] {unk}");
				UnknownList.Add(unk);
			}
		}

		Logger.Trace(Utils.GetBufferHexString(br, 32));

		Name = br.ReadPivString();
		Logger.Debug($"    name='{Name}'");
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
