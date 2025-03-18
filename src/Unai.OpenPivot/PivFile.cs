using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;

namespace Unai.OpenPivot;

public class PivFile
{
	public List<PivFigure> Figures { get; } =
	[
		null,
		PivFigure.DefaultFigure,
	];

	public List<PivFrame> Frames { get; } = [];

	public void Load(Stream stream)
	{
		using var br = new BinaryReader(stream);

		var pivAniVersion = br.ReadByte();

		if (pivAniVersion == 'x')
		{
			stream.Position = 0;
			using var zlibStr = new ZLibStream(stream, CompressionMode.Decompress);
			using var memStr = new MemoryStream();
			zlibStr.CopyTo(memStr);
			memStr.Position = 0;
			Load(memStr);
			return;
		}

		var canvasWidth = br.ReadUInt32();
		var canvasHeight = br.ReadUInt32();
		Console.Error.WriteLine($"v{pivAniVersion} {canvasWidth}×{canvasHeight}");

		var backgroundCount = br.ReadUInt16(); // 1, 2 or 3
		Console.Error.WriteLine($"bg#={backgroundCount}");
		Console.Error.WriteLine($"  [bg0] default");
		for (int bgIdx = 1; bgIdx < backgroundCount; bgIdx++)
		{
			// 0 = PNG, 1 = JPEG, 2 = solid, 3 = gradient
			var kind = br.ReadByte();

			switch (kind)
			{
				case 0:
				case 1:
					{
						// Console.Error.WriteLine(Utils.GetBufferHexString(br, 32));

						var imageDataOff = br.BaseStream.Position + sizeof(uint);
						var imageDataEndOff = br.ReadUInt32();
						var size = imageDataEndOff - imageDataOff;

						br.BaseStream.Position = imageDataEndOff;

						// Console.Error.WriteLine(Utils.GetBufferHexString(br, 32));

						var bgName = br.ReadPivString();

						Console.Error.WriteLine($"  [bg{bgIdx}] '{bgName}' size={size}");
					}
					break;

				case 2:
				case 3:
					{
						var blue0 = br.ReadByte();
						var green0 = br.ReadByte();
						var red0 = br.ReadByte();
						var alpha0 = br.ReadByte();
						byte blue1 = 0;
						byte green1 = 0;
						byte red1 = 0;
						byte alpha1 = 0;
						float x0 = 0;
						float y0 = 0;
						float x1 = 0;
						float y1 = 0;
						if (kind == 3)
						{
							blue1 = br.ReadByte();
							green1 = br.ReadByte();
							red1 = br.ReadByte();
							alpha1 = br.ReadByte();
							x0 = br.ReadSingle();
							y0 = br.ReadSingle();
							x1 = br.ReadSingle();
							y1 = br.ReadSingle();
						}
						var bgName = br.ReadPivString();
						Console.Error.WriteLine($"  [bg{bgIdx}] type={kind} '{bgName}'");
						if (kind == 3)
						{
							Console.Error.WriteLine($"    pulls {x0}:{y0} {x1}:{y1}");
						}
					}
					break;
			}
		}

		var figureCount = br.ReadUInt16();

		Console.Error.WriteLine($"fig#={figureCount}");
		Console.Error.WriteLine("  [fig0] null?");
		Console.Error.WriteLine("  [fig1] default?");

		for (int figIdx = 2; figIdx < figureCount; figIdx++)
		{
			Figures.Add(new(br));
		}

		var frameCount = br.ReadUInt32();
		Console.Error.WriteLine($"fr#={frameCount}");

		for (int f = 0; f < frameCount; f++)
		{
			var frame = new PivFrame();
			Frames.Add(frame);

			var bgIdx = br.ReadUInt16();
			var unk1 = br.ReadUInt16();
			var unk2 = br.ReadByte();
			var elementCount = br.ReadUInt16();

			Console.Error.WriteLine($"  [fr{f}] bg={bgIdx} {unk1:x4} {unk2:x2} elem#={elementCount}");

			for (int eIdx = 0; eIdx < elementCount; eIdx++)
			{
				var figInst = new PivFigureInstance();
				frame.FigureInstances.Add(figInst);

				Console.Error.WriteLine(Utils.GetBufferHexString(br, 32));

				var eUnk0 = br.ReadUInt32();
				figInst.FigureIndex = br.ReadUInt16();
				figInst.Scale = br.ReadSingle(); // untested
				var rotation = br.ReadSingle(); // untested
				var eUnk1 = br.ReadByte();

				Console.Error.WriteLine($"    [e{eIdx}] {eUnk0:x8} fig={figInst.FigureIndex:x4} scale={figInst.Scale} rot={rotation} {eUnk1:x2}");
				
				var figure = Figures[figInst.FigureIndex];
				for (int segIdx = 1; segIdx < figure.Segments.Count; segIdx++)
				{
					var segAngle = br.ReadDouble();
					figInst.SegmentOverrides.Add(new(segAngle));
					Console.Error.WriteLine($"        [seg{segIdx}] angle={Utils.ToDegrees(segAngle)}");
				}

				if (figure.Segments[1].SegmentType == PivSegmentType.Text)
				{
					_ = br.ReadByte();
				}

				var x = br.ReadSingle();
				var y = br.ReadSingle();
				figInst.Position = new(x, y);
				var eUnk2 = br.ReadBytes(5); // always 0

				Console.Error.WriteLine($"      x={x} y={y} {Utils.ToHex(eUnk2)}");
			}

			br.ReadBytes(1 + elementCount * 2);
		}

		Console.Error.WriteLine("tail (must be 5 bytes):\n" + Utils.GetBufferHexString(br, 16));

		var framerate = br.ReadUInt32();
		Console.Error.WriteLine($"fps={framerate}");
	}
}