using System.Collections.Generic;
using System.IO;
using System.Text;
using ProtobufDecoder.App.Model;

namespace ProtobufDecoder.App.Writers
{
	public sealed class PythonWriter
	{
		public void Write(string outputDirectory, IEnumerable<ObjectDescription> objects)
		{
			Directory.CreateDirectory(outputDirectory);
			string path = Path.Combine(outputDirectory, "models.py");
			using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
			using var sw = new StreamWriter(fs, new UTF8Encoding(false));
			sw.WriteLine("from dataclasses import dataclass, field");
			sw.WriteLine("from typing import List, Optional");
			sw.WriteLine();

			foreach (var o in objects)
			{
				WriteObject(sw, o);
				sw.WriteLine();
			}
		}

		void WriteObject(StreamWriter sw, ObjectDescription o)
		{
			sw.WriteLine($"@dataclass\nclass {o.Name}:");
			if (o.Fields.Count == 0)
			{
				sw.WriteLine("    pass");
				return;
			}
			foreach (var f in o.Fields)
			{
				string pyType = MapToPythonType(f);
				sw.WriteLine($"    {f.Name}: {pyType} = None");
			}
		}

		static string MapToPythonType(FieldDescription f)
		{
			string baseType = f.TypeName switch
			{
				"bool" => "bool",
				"string" => "str",
				"float" => "float",
				"double" => "float",
				"int32" => "int",
				"uint32" => "int",
				"int64" => "int",
				"uint64" => "int",
				"bytes" => "bytes",
				_ => f.TypeName
			};
			if (f.Cardinality == FieldCardinality.Repeated)
			{
				return $"List[{baseType}]";
			}
			return baseType;
		}
	}
}




