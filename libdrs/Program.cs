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

			// Starting to read table headers now
			for (var i = 0; i < 4; i++)
			{
				Console.WriteLine();
				Console.WriteLine("Table {0}:", i + 1);

				var filetype = s.ReadASCII(1);
				if (!string.IsNullOrWhiteSpace(filetype))
					Console.WriteLine("Filetype: {0}", filetype);

				var rExt = s.ReadASCII(3);
				Console.WriteLine("Extension: {0} ({1})", rExt, rExt.Reversed());

				var tableOffset = s.ReadInt32();
				Console.WriteLine("Table offset: {0}", tableOffset);
				tableOffsets[i] = tableOffset;

				var numberOfFiles = s.ReadInt32();
				Console.WriteLine("Number of files: {0}", numberOfFiles);
				filesInTable[i] = numberOfFiles;
			}

			for (var i = 0; i < 4; i++)
			{
				Console.WriteLine();

				// Seek to the actual table
				s.Position = (long)tableOffsets[i];

				Console.WriteLine("Table {0}'s position: {1}", i + 1, s.Position);
				Console.WriteLine("Files in table {0}: {1}", i + 1, filesInTable[i]);

				// Read the actual files
				for (var file = 0; file < filesInTable[i]; file++)
				{
					Console.WriteLine();

					var fileID = BitConverter.ToInt32(s.ReadBytes(4), 0);
					Console.WriteLine("\tFile ID: {0}", fileID);
					
					var fileOffset = s.ReadInt32();
					Console.WriteLine("\tFile Offset: {0}", fileOffset);

					var fileLength = s.ReadInt32();
					Console.WriteLine("\tFile Length (bytes): {0}", fileLength);

					Console.WriteLine("\tSeeking to {0}", fileOffset);
					s.Position = (long)fileOffset;
					File.WriteAllBytes(fileID.ToString(), s.ReadBytes(fileLength));

					Console.WriteLine("\tWrote {0} ({1})", fileID, file);

					Console.WriteLine("\tGoing to table header @ {0}", tableOffsets[i]);
					s.Position = (long)tableOffsets[i];
				}
			}
		}
	}
}
