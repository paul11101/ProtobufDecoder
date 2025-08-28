using System;
using System.Collections.Generic;
using System.IO;
using ProtobufDecoder.App.Parsing;
using ProtobufDecoder.App.Writers;

namespace ProtobufDecoder.App
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			string input = FindArg(args, "-i") ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "dummydlls");
			string output = FindArg(args, "-o") ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "proto_out");
			string? package = FindArg(args, "-pkg");
			string? pythonOut = FindArg(args, "-pythonOut");
			string? nsInclude = FindArg(args, "-nsInclude");

			IEnumerable<string>? includes = null;
			if (!string.IsNullOrEmpty(nsInclude))
			{
				includes = nsInclude.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			}

			Console.WriteLine($"Input: {Path.GetFullPath(input)}");
			Console.WriteLine($"Output: {Path.GetFullPath(output)}");
			if (!string.IsNullOrEmpty(pythonOut)) Console.WriteLine($"PythonOut: {Path.GetFullPath(pythonOut)}");

			var parser = new AssemblyParser();
			var result = parser.ParseDirectory(input, includes);

			var writer = new DescriptionWriter();
			writer.WriteAll(output, result.Objects, result.Enums, package, pythonOut);

			Console.WriteLine("Done.");
			return 0;
		}

		private static string? FindArg(string[] args, string key)
		{
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
				{
					return args[i + 1];
				}
			}
			return null;
		}
	}
}

