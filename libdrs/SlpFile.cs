using System;
using System.IO;
using System.Collections.Generic;

namespace libdrs
{
	public class SlpFile
	{
		public string Name { get; private set; }
		public readonly SlpHeader Header;
		public readonly List<SlpFrame> Frames;

		readonly Stream stream;

		static void Assert(bool test, string errorMessage)
		{
			if (!test)
				throw new Exception(errorMessage);
		}

		public List<byte> ReadRowCommands(Stream stream, SlpFrame frame, SlpFrameRow row)
		{
			var ret = new List<byte>();
			for (var i = 0; i < frame.Width; i++)
				ret.Add(0);

			if (row.ShouldSkip)
			{
				stream.ReadBytes(1);
				return ret;
			}

			var command = DrawCommand.EndOfRow;

			byte currentByte;
//			byte previousByte; // Used by the extended 'flip' commands so it isn't necessary until those are implemented

			var currentPixelPosition = row.Left;

			do
			{
//				previousByte = currentByte;

				uint commandLength = 0;
				currentByte = stream.ReadBytes(1)[0];
				command = CommandFromByte(currentByte);

				switch (command)
				{
					case DrawCommand.LesserBlockCopy:
					case DrawCommand.LesserBlockCopy2:
					case DrawCommand.LesserBlockCopy3:
					case DrawCommand.LesserBlockCopy4:
						commandLength = GetTop6BitsOf(currentByte);

						for (var i = 0; i < commandLength; i++)
							ret[currentPixelPosition++] = stream.ReadBytes(1)[0];

						break;

					case DrawCommand.LesserSkip:
					case DrawCommand.LesserSkip2:
					case DrawCommand.LesserSkip3:
					case DrawCommand.LesserSkip4:
						commandLength = GetTop6BitsOf(currentByte);
						currentPixelPosition += (ushort)commandLength;
						break;

					case DrawCommand.GreaterBlockCopy:
						commandLength = Get4BitsAndNext(currentByte, stream.ReadBytes(1)[0]);

						for (var i = 0; i < commandLength; i++)
							ret[currentPixelPosition++] = stream.ReadBytes(1)[0];

						break;

					case DrawCommand.GreaterSkip:
						commandLength = Get4BitsAndNext(currentByte, stream.ReadBytes(1)[0]);
						currentPixelPosition += (ushort)commandLength;
						break;

					case DrawCommand.PlayerColorCopy:
						commandLength = GetTopNibbleOrNext(currentByte, stream);
						break;

					case DrawCommand.FillColor:
						commandLength = GetTopNibbleOrNext(currentByte, stream);

						var fillColor = stream.ReadBytes(1)[0];
						for (var i = 0; i < commandLength; i++)
							ret[currentPixelPosition++] = fillColor;

						break;

					case DrawCommand.FillPlayerColor:
						commandLength = GetTopNibbleOrNext(currentByte, stream);

						var playerColor = (byte)(stream.ReadBytes(1)[0] + ((1 + 1) * 16));
						for (var i = 0; i < commandLength; i++)
							ret[currentPixelPosition++] = playerColor;

						break;

					case DrawCommand.ObscuredColor:
						commandLength = GetTopNibbleOrNext(currentByte, stream);
						for (var i = 0; i < commandLength; i++)
							ret[currentPixelPosition++] = 56;

						break;

					case DrawCommand.ExtendedCommand:
						Console.WriteLine("TODO");
						break;

					case DrawCommand.EndOfRow:
						break;

					default:
						Console.WriteLine("Error parsing command from `{0}`!", currentByte.ToString());
						Environment.Exit(1);
						break;
				}

			} while (command != DrawCommand.EndOfRow);

			Assert(currentPixelPosition + row.Right == (uint)frame.Width, "Current Pixel Position + Right skip != Frame's width!");

			return ret;
		}

		static DrawCommand CommandFromByte(byte b)
		{
			return (DrawCommand)(b & 0x0F);
		}

		static uint GetTop6BitsOf(byte b)
		{
			return (uint)(b & 0xFC) >> 2;
		}

		static uint Get4BitsAndNext(byte b1, byte b2)
		{
			return (uint)((b1 & 0xF0) << 4) + b2;
		}

		static uint GetTopNibbleOrNext(byte b, Stream stream)
		{
			var length = (uint)(b & 0xF0) >> 4;

			if (length == 0)
				length = stream.ReadBytes(1)[0];

			return length;
		}

		public SlpFile(string filename)
		{
			Name = filename;
			stream = new FileStream(filename, FileMode.Open, FileAccess.Read);

			Header = SlpHeader.FromStream(stream);

			Frames = new List<SlpFrame>();
			for (var i = 0; i < Header.FrameCount; i++)
				Frames.Add(SlpFrame.FromStream(stream));

			var currFrame = 0;
			foreach (var frame in Frames)
			{
				var pos = stream.Position;

				// Read the outline color data offset
				Assert(stream.Position == frame.OutlineColorDataOffset, "Invalid outline offset: {0} in frame {1}.".F(pos, currFrame));
				for (var i = 0; i < frame.Height; i++)
					frame.Rows.Add(SlpFrameRow.FromStream(stream));

				// Read the row data offset
				Assert(stream.Position == frame.CommandDataOffset, "Invalid command offset: {0} in frame {1}.".F(pos, currFrame));
				for (var i = 0; i < frame.Height; i++)
					frame.Rows[i].RowDataOffset = stream.ReadUInt32();

				// Read each row's data
				for (var i = 0; i < frame.Height; i++)
				{
					var row = frame.Rows[i];
					Assert(stream.Position == row.RowDataOffset, "Invalid row data offset: {0} in frame {1}.".F(pos, currFrame));

					row.PixelData = ReadRowCommands(stream, frame, row);
				}

				currFrame++;
			}
		}
	}

	public class SlpHeader
	{
		public string Version;
		public int FrameCount;
		public string Comment;

		public static SlpHeader FromStream(Stream stream)
		{
			return new SlpHeader
			{
				Version = stream.ReadASCII(4),
				FrameCount = stream.ReadInt32(),
				Comment = stream.ReadASCII(24),
			};
		}
	}

	public class SlpFrame
	{
		public uint CommandDataOffset;
		public uint OutlineColorDataOffset;
		public uint PaletteOffset;
		public uint Properties;
		public int Width;
		public int Height;
		public int HotspotX;
		public int HotspotY;

		public List<SlpFrameRow> Rows = new List<SlpFrameRow>();

		public static SlpFrame FromStream(Stream stream)
		{
			return new SlpFrame
			{
				CommandDataOffset = stream.ReadUInt32(),
				OutlineColorDataOffset = stream.ReadUInt32(),
				PaletteOffset = stream.ReadUInt32(),
				Properties = stream.ReadUInt32(),
				Width = stream.ReadInt32(),
				Height = stream.ReadInt32(),
				HotspotX = stream.ReadInt32(),
				HotspotY = stream.ReadInt32()
			};
		}
	}

	enum DrawCommand : byte
	{
		LesserBlockCopy  = 0x00,
		LesserSkip       = 0x01,
		GreaterBlockCopy = 0x02,
		GreaterSkip      = 0x03,
		LesserBlockCopy2 = 0x04,
		LesserSkip2      = 0x05,
		PlayerColorCopy  = 0x06,
		FillColor        = 0x07,
		LesserBlockCopy3 = 0x08,
		LesserSkip3      = 0x09,
		FillPlayerColor  = 0x0A,
		ObscuredColor    = 0x0B,
		LesserBlockCopy4 = 0x0C,
		LesserSkip4      = 0x0D,
		ExtendedCommand  = 0x0E,
		EndOfRow         = 0x0F
	}

	enum DrawCommandExtended : byte
	{
		Invalid             = 0x00,
		RenderHintFlipX     = 0x0E,
		RenderHintNotFlipX  = 0x1E,
		TableColorNormal    = 0x2E,
		TableColorAlt       = 0x3E,
		PlayerOrTransparent = 0x4E,
		OutlineSpan1        = 0x5E,
		BlackOutline        = 0x6E,
		OutlineSpan2        = 0x7E
	}

	public class SlpFrameRow
	{
		public ushort Left;
		public ushort Right;
		public uint RowDataOffset;
		public List<byte> PixelData = new List<byte>();

		public bool ShouldSkip { get { return Left == empty; } }

		const ushort empty = 0x8000;

		public static SlpFrameRow FromStream(Stream stream)
		{
			return new SlpFrameRow
			{
				Left = stream.ReadUInt16(),
				Right = stream.ReadUInt16()
			};
		}
	}
}
