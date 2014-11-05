using System;
using System.IO;

namespace libdrs
{
	class MainClass
	{
		static FileStream s;

		public static void Main(string[] args)
		{
			if (args.Length < 1)
				return;

			s = new FileStream(args[0], FileMode.Open, FileAccess.Read);

			var copyright = s.ReadASCII(36);
			s.ReadBytes(4); // \032.
			Console.WriteLine("Copyright: {0}", copyright.Replace('\0', '0'));

			var version = s.ReadASCII(4);
			Console.WriteLine(version);

			var tribe = s.ReadASCII(12);
			Console.WriteLine("{0}", tribe.Replace('\0', '0'));

			var numTables = s.ReadInt32();
			Console.WriteLine("Number of tables: {0}", numTables);

			var firstFileOffset = s.ReadInt32();

			Console.WriteLine("Position should be 64: {0}", s.Position);

			var tableOffsets = new int[4];
			var filesInTable = new int[4];
			var typePerTable = new string[4];

			if (!Directory.Exists("output"))
				Directory.CreateDirectory("output");

			// Starting to read table headers now
			for (var i = 0; i < numTables; i++)
			{
				Console.WriteLine();
				Console.WriteLine("Table {0}:", i + 1);

				var filetype = s.ReadASCII(1);
				if (!string.IsNullOrWhiteSpace(filetype))
					Console.WriteLine("Filetype: {0}", filetype);

				var ext = s.ReadASCII(3).Reversed();
				Console.WriteLine("Extension: {0} ({1})", ext, ext.Reversed());
				typePerTable[i] = ".{0}".F(ext);

				var tableOffset = s.ReadInt32();
				Console.WriteLine("Table offset: {0}", tableOffset);
				tableOffsets[i] = tableOffset;

				var numberOfFiles = s.ReadInt32();
				Console.WriteLine("Number of files: {0}", numberOfFiles);
				filesInTable[i] = numberOfFiles;
			}

			for (var i = 0; i < numTables; i++)
			{
				Console.WriteLine();

				// Seek to the actual table
				s.Position = (long)tableOffsets[i];

				Console.WriteLine("Table {0}'s position: {1}", i + 1, s.Position);
				Console.WriteLine("Files in table {0}: {1}", i + 1, filesInTable[i]);

				var lastPos = 0L;

				// Read the actual files
				for (var file = 0; file < filesInTable[i]; file++)
				{
					if (lastPos != 0)
					{
						Console.WriteLine("Going to lastPos at {0}", lastPos);
						s.Position = lastPos;
					}

					var fileID = s.ReadInt32();
					Console.WriteLine("\tFile ID: {0}", fileID);
					
					var fileOffset = s.ReadInt32();
					Console.WriteLine("\tFile Offset: {0}", fileOffset);

					var fileLength = s.ReadInt32();
					Console.WriteLine("\tFile Length (bytes): {0}", fileLength);

					lastPos = s.Position;
					Console.WriteLine("\tSeeking to {0}", fileOffset);
					s.Position = (long)fileOffset;

					var output = "{0}/{1}{2}".F("output", fileID.ToString(), typePerTable[i]);

					File.WriteAllBytes(output, s.ReadBytes(fileLength));

					Console.WriteLine("\tWrote {0} ({1})", fileID, file);

					Console.WriteLine("\tEnd of file {0} at {1}", fileID, s.Position);
				}

				Console.WriteLine("\tGoing to table header @ {0}", tableOffsets[i]);
				s.Position = (long)tableOffsets[i];
			}
		}
	}
}
