using System;
using System.IO;

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

			var ext = inputFile.Substring(inputFile.Length - 3);
			switch (ext)
			{
				case "drs": new DrsFile(inputFile); break;
				default: Console.WriteLine("Filetype `{0}` is not supported.", ext); break;
			}
		}
	}
}
