using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtobufDecoder.App.Model;

namespace ProtobufDecoder.App.Writers
{
	public sealed class ProtobufWriter
	{
		public void Write(string outputDirectory, IEnumerable<ObjectDescription> objects, IEnumerable<EnumDescription> enums, string? package)
		{
			Directory.CreateDirectory(outputDirectory);

			// 简单策略：按命名空间拆分文件
			var group = objects.GroupBy(o => o.Namespace ?? string.Empty).ToList();

			foreach (var g in group)
			{
				string fileName = string.IsNullOrEmpty(g.Key) ? "types.proto" : g.Key.Replace('.', '_') + ".proto";
				string path = Path.Combine(outputDirectory, fileName);
				using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
				using var sw = new StreamWriter(fs, new UTF8Encoding(false));
				WriteFile(sw, g.Key, g.ToList(), enums.Where(e => e.Namespace == g.Key).ToList(), package);
			}
		}

		void WriteFile(StreamWriter sw, string ns, List<ObjectDescription> objects, List<EnumDescription> enums, string? package)
		{
			sw.WriteLine("syntax = \"proto3\";");
			if (!string.IsNullOrEmpty(package))
			{
				sw.WriteLine($"package {package};");
			}
			else if (!string.IsNullOrEmpty(ns))
			{
				sw.WriteLine($"package {ns.Replace('.', '_')};");
			}
			// 根据类型用到 google.protobuf 增加 import
			if (UsesGoogleTypes(objects))
			{
				sw.WriteLine("import \"google/protobuf/timestamp.proto\";");
				sw.WriteLine("import \"google/protobuf/duration.proto\";");
			}
			sw.WriteLine();

			foreach (var e in enums)
			{
				WriteEnum(sw, e);
				sw.WriteLine();
			}

			foreach (var o in objects)
			{
				WriteObject(sw, o);
				sw.WriteLine();
			}
		}

		static bool UsesGoogleTypes(IEnumerable<ObjectDescription> objects)
		{
			foreach (var o in objects)
			{
				foreach (var f in o.Fields)
				{
					if (f.TypeName == "google.protobuf.Timestamp" || f.TypeName == "google.protobuf.Duration") return true;
				}
				foreach (var no in o.NestedObjects)
				{
					if (UsesGoogleTypes(new[] { no })) return true;
				}
			}
			return false;
		}

		void WriteEnum(StreamWriter sw, EnumDescription e)
		{
			sw.WriteLine($"enum {e.Name} {{");
			var ordered = e.Values.OrderBy(v => v.Number).ToList();
			bool hasZero = ordered.Count > 0 && ordered[0].Number == 0;
			if (!hasZero)
			{
				sw.WriteLine("  Unspecified = 0;");
			}
			foreach (var v in ordered)
			{
				sw.WriteLine($"  {v.Name} = {v.Number};");
			}
			sw.WriteLine("}");
		}

		void WriteObject(StreamWriter sw, ObjectDescription o)
		{
			sw.WriteLine($"message {o.Name} {{");

			foreach (var one in o.Oneofs)
			{
				sw.WriteLine($"  oneof {one.Name} {{");
				foreach (var f in one.Fields)
				{
					string typeName = QualifyTypeIfNeeded(o, f.TypeName, f.IsMessageType || f.IsEnumType);
					sw.WriteLine($"    {typeName} {f.Name} = {f.Tag};");
				}
				sw.WriteLine("  }");
			}

			foreach (var f in o.Fields)
			{
				if (!string.IsNullOrEmpty(f.OneofGroup)) continue; // oneof 已经写过
				if (f.IsMap && !string.IsNullOrEmpty(f.MapKeyTypeName) && !string.IsNullOrEmpty(f.MapValueTypeName))
				{
					var keyT = f.MapKeyTypeName;
					var valT = f.MapValueTypeName;
					sw.WriteLine($"  map<{keyT}, {valT}> {f.Name} = {f.Tag};");
				}
				else
				{
					string label = f.Cardinality == FieldCardinality.Repeated ? "repeated " : string.Empty;
					string typeName = QualifyTypeIfNeeded(o, f.TypeName, f.IsMessageType || f.IsEnumType);
					sw.WriteLine($"  {label}{typeName} {f.Name} = {f.Tag};");
				}
			}

			// 嵌套枚举
			foreach (var e in o.Enums)
			{
				WriteEnum(sw, e);
			}

			// 嵌套消息
			foreach (var no in o.NestedObjects)
			{
				WriteObject(sw, no);
			}

			sw.WriteLine("}");
		}

		static string QualifyTypeIfNeeded(ObjectDescription scope, string typeName, bool isComposite)
		{
			if (!isComposite) return typeName; // 基元类型
			// 简化：不做复杂限定，保持短名
			return typeName;
		}
	}
}


