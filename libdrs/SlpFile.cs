using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace libdrs
{
	public class SlpFile
	{
		public string Name { get; private set; }

		readonly Stream s;

		public SlpFile(string filename)
		{
			Name = filename;

			s = new FileStream(filename, FileMode.Open, FileAccess.Read);

			var header = new SlpHeader(s);
			if (header.FrameCount < 1)
				throw new InvalidOperationException("Bogus header in {0}.".F(filename));

			var frames = new SlpFrame[header.FrameCount];

			for (var f = 0; f < frames.Length; f++)
				frames[f] = new SlpFrame(s);
		}

		class SlpHeader
		{
			public readonly string Version;
			public readonly int FrameCount;
			public readonly string Comment;

			public SlpHeader(Stream s)
			{
				Version = s.ReadASCII(4);
				FrameCount = s.ReadInt32();
				Comment = s.ReadASCII(24);
			}
		}

		class SlpFrame
		{
			public readonly uint CommandTableOffset;
			public readonly uint OutlineTableOffset;
			public readonly uint PaletteOffset;
			public readonly uint Properties;
			public readonly int Width;
			public readonly int Height;
			public readonly int HotspotX;
			public readonly int HotspotY;
			public readonly ReadOnlyCollection<SlpFrameRowEdge> RowEdges;
			public readonly ReadOnlyCollection<uint> CommandOffsets;

			readonly List<SlpFrameRowEdge> rowEdges;
			readonly List<uint> commandOffsets;

			public SlpFrame(Stream s)
			{
				CommandTableOffset = s.ReadUInt32();
				OutlineTableOffset = s.ReadUInt32();
				PaletteOffset = s.ReadUInt32();
				Properties = s.ReadUInt32();
				Width = s.ReadInt32();
				Height = s.ReadInt32();
				HotspotX = s.ReadInt32();
				HotspotY = s.ReadInt32();

				rowEdges = new List<SlpFrameRowEdge>();
				s.Position = OutlineTableOffset;
				for (var i = 0; i < Height; i++)
					rowEdges.Add(new SlpFrameRowEdge(s));

				RowEdges = rowEdges.AsReadOnly();

				commandOffsets = new List<uint>();
				s.Position = CommandTableOffset;
				for (var i = 0; i < Height; i++)
					commandOffsets.Add(s.ReadUInt32());

				CommandOffsets = commandOffsets.AsReadOnly();

				Console.WriteLine("Should be at {0} -- Currently at {1}", CommandTableOffset + 4 * Height, s.Position);
			}

		}

		class SlpFrameRowEdge
		{
			public readonly short Left;
			public readonly short Right;
			public readonly bool ShouldSkip;
			
			static ushort empty = 0x8000; // 1000 0000 0000 0000 (32768 -- max value of unsigned short)
			
			public SlpFrameRowEdge(Stream s)
			{
				Left = s.ReadInt16();
				Right = s.ReadInt16();
				ShouldSkip = (Left == empty || Right == empty);
			}
		}
	}
}
