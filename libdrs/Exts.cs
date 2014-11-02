using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace libdrs
{
	public static class Exts
	{
		public static string F(this string fmt, params object[] args)
		{
			return string.Format(fmt, args);
		}

		public static string Reversed(this string s)
		{
			var arr = s.ToCharArray();
			Array.Reverse(arr);
			return new string(arr);
		}

		public static byte[] ReadBytes(this Stream s, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", "Non-negative number required.");

			var buf = new byte[count];
			if (s.Read(buf, 0, count) < count)
				throw new EndOfStreamException();

			return buf;
		}

		public static int Peek(this Stream s)
		{
			var buf = new byte[1];
			if (s.Read(buf, 0, 1) == 0)
				return -1;

			s.Seek(s.Position - 1, SeekOrigin.Begin);
			return buf[0];
		}

		public static byte ReadUInt8(this Stream s)
		{
			return s.ReadBytes(1)[0];
		}

		public static ushort ReadUInt16(this Stream s)
		{
			return BitConverter.ToUInt16(s.ReadBytes(2), 0);
		}

		public static short ReadInt16(this Stream s)
		{
			return BitConverter.ToInt16(s.ReadBytes(2), 0);
		}

		public static uint ReadUInt32(this Stream s)
		{
			return BitConverter.ToUInt32(s.ReadBytes(4), 0);
		}

		public static int ReadInt32(this Stream s)
		{
			return BitConverter.ToInt32(s.ReadBytes(4), 0);
		}

		public static float ReadFloat(this Stream s)
		{
			return BitConverter.ToSingle(s.ReadBytes(4), 0);
		}

		public static double ReadDouble(this Stream s)
		{
			return BitConverter.ToDouble(s.ReadBytes(8), 0);
		}

		public static string ReadASCII(this Stream s, int length)
		{
			return new string(Encoding.ASCII.GetChars(s.ReadBytes(length)));
		}

		public static string ReadASCIIZ(this Stream s)
		{
			var bytes = new List<byte>();
			var buf = new byte[1];

			for (;;)
			{
				if (s.Read(buf, 0, 1) < 1)
					throw new EndOfStreamException();

				if (buf[0] == 0)
					break;

				bytes.Add(buf[0]);
			}

			return new string(Encoding.ASCII.GetChars(bytes.ToArray()));
		}
	}
}
