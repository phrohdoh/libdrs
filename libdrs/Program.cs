using System;
using System.IO;
using System.Collections.Generic;

namespace libdrs
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("No paramaters given!");
				return;
			}

			if (!Directory.Exists("output"))
				Directory.CreateDirectory("output");

			var inputFile = args[0];

			if (inputFile.EndsWith(".drs"))
				new DrsFile(inputFile);
			else
				Console.WriteLine("Filetype unsupported.");
		}
	}

	public class DrsFile
	{
		public string Name { get; private set; }

		readonly Dictionary<string, DrsEmbeddedFile> files = new Dictionary<string, DrsEmbeddedFile>();
		readonly Stream s;

		public DrsFile(string filename)
		{
			Name = filename;

			s = new FileStream(filename, FileMode.Open, FileAccess.Read);

			var header = new DrsHeader(s, Name);
			if (header.TableCount < 1)
				throw new InvalidOperationException("Bogus header in {0}.".F(filename));

			var tables = new DrsTable[header.TableCount];

			// Table headers are sequential.
			// They are not like so: [header][data], [header][data]
			// Instead they are: [header][header], [data][data]
			// So we read all of the header info before diving into data.
			for (var t = 0; t < tables.Length; t++)
				tables[t] = new DrsTable(s);

			foreach (var t in tables)
			{
				s.Position = t.Offset;
				for (var f = 0; f < t.FileCount; f++)
				{
					var ef = new DrsEmbeddedFile(s, t);
					if (!files.ContainsKey(ef.GeneratedFilename))
						files.Add(ef.GeneratedFilename, ef);
				}
			}

			foreach (var file in files.Values)
			{
				var output = "output/{0}".F(file.GeneratedFilename);
				File.WriteAllBytes(output, file.GetData());
			}
		}

		public byte[] GetData(string filename)
		{
			if (!files.ContainsKey(filename))
				throw new KeyNotFoundException("Embedded file `{0}` does not exist.".F(filename));

			return files[filename].GetData();
		}

		class DrsHeader
		{
			public readonly string CopyrightInfo;
			public readonly string Version;
			public readonly string ArchiveType;
			public readonly int TableCount;
			public readonly int FirstFileOffset;

			public DrsHeader(Stream s, string name)
			{
				if (s.Position != 0)
					throw new InvalidOperationException("Trying to read DRS header from somewhere other than the beginning of the archive.");

				CopyrightInfo = s.ReadASCII(40);
				Version = s.ReadASCII(4);
				ArchiveType = s.ReadASCII(12);
				if (ArchiveType != "tribe\0\0\0\0\0\0\0")
					throw new InvalidOperationException("Archive `{0}` is not a valid `tribe` archive.".F(name));

				TableCount = s.ReadInt32();
				FirstFileOffset = s.ReadInt32();
			}
		}

		class DrsTable
		{
			public readonly string FileType;
			public readonly string Extension;
			public readonly int Offset;
			public readonly int FileCount;

			public DrsTable(Stream s)
			{
				FileType = s.ReadASCII(1);
				Extension = s.ReadASCII(3).Reversed();
				Offset = s.ReadInt32();
				FileCount = s.ReadInt32();
			}
		}

		class DrsEmbeddedFile
		{
			public readonly int FileID;
			public readonly int Offset;
			public readonly int Length;
			public readonly string GeneratedFilename;

			Stream s;

			public DrsEmbeddedFile(Stream s, DrsTable t)
			{
				this.s = s;

				if (s.Length - s.Position < 12)
					throw new InvalidOperationException("Going to read past end of stream. Something went wrong reading this DRS archive.");

				FileID = s.ReadInt32();
				Offset = s.ReadInt32();
				Length = s.ReadInt32();

				GeneratedFilename = "{0}.{1}".F(FileID.ToString(), t.Extension);
			}

			public byte[] GetData()
			{
				s.Position = Offset;
				return s.ReadBytes(Length);
			}
		}
	}
}
