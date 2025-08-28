using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using ProtobufDecoder.App.Model;

namespace ProtobufDecoder.App.Parsing
{
	public sealed class AssemblyParser
	{
		public sealed class ParseResult
		{
			public List<ObjectDescription> Objects { get; } = new List<ObjectDescription>();
			public List<EnumDescription> Enums { get; } = new List<EnumDescription>();
		}

		public ParseResult ParseDirectory(string inputDirectory, IEnumerable<string>? namespaceIncludes)
		{
			var result = new ParseResult();
			var includeList = (namespaceIncludes ?? Array.Empty<string>()).ToList();

			var resolver = new DefaultAssemblyResolver();
			if (Directory.Exists(inputDirectory))
			{
				resolver.AddSearchDirectory(inputDirectory);
			}
			else
			{
				var dir = Path.GetDirectoryName(inputDirectory);
				if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
				{
					resolver.AddSearchDirectory(dir);
				}
			}

			var readerParams = new ReaderParameters
			{
				AssemblyResolver = resolver,
				ReadSymbols = false
			};

			if (File.Exists(inputDirectory))
			{
				var dllPath = inputDirectory;
				var fileName = Path.GetFileName(dllPath);
				if (!ShouldSkipAssembly(fileName))
				{
					try
					{
						var asm = AssemblyDefinition.ReadAssembly(dllPath, readerParams);
						ParseAssembly(asm, includeList, result);
					}
					catch
					{
						// 忽略无法解析的 DLL
					}
				}
			}
			else if (Directory.Exists(inputDirectory))
			{
				foreach (var dllPath in Directory.EnumerateFiles(inputDirectory, "*.dll", SearchOption.TopDirectoryOnly))
				{
					var fileName = Path.GetFileName(dllPath);
					if (ShouldSkipAssembly(fileName))
					{
						continue;
					}

					try
					{
						var asm = AssemblyDefinition.ReadAssembly(dllPath, readerParams);
						ParseAssembly(asm, includeList, result);
					}
					catch
					{
						// 忽略无法解析的 DLL
					}
				}
			}
			else
			{
				// 输入既不是文件也不是目录，直接返回空结果
				return result;
			}

			return result;
		}

		static bool ShouldSkipAssembly(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) return true;
			string lower = fileName.ToLowerInvariant();
			// 跳过常见运行库与 Unity/系统程序集
			return lower.StartsWith("system") || lower.StartsWith("microsoft") || lower.StartsWith("unity") || lower.StartsWith("mono") || lower == "mscorlib.dll" || lower.StartsWith("netstandard") || lower.StartsWith("protobuf-net") || lower.Contains("unityengine");
		}

		void ParseAssembly(AssemblyDefinition asm, List<string> includeList, ParseResult output)
		{
			foreach (var module in asm.Modules)
			{
				foreach (var type in module.Types)
				{
					ParseTypeRecursive(type, includeList, output);
				}
			}
		}

		void ParseTypeRecursive(TypeDefinition type, List<string> includeList, ParseResult output)
		{
			if (type == null) return;

			if (IncludeNamespace(type.Namespace, includeList))
			{
				if (type.IsEnum)
				{
					var enumDesc = TryBuildEnum(type);
					if (enumDesc != null)
					{
						output.Enums.Add(enumDesc);
					}
				}
				else
				{
					var objDesc = TryBuildObject(type);
					if (objDesc != null)
					{
						output.Objects.Add(objDesc);
					}
				}
			}

			if (type.HasNestedTypes)
			{
				foreach (var nested in type.NestedTypes)
				{
					ParseTypeRecursive(nested, includeList, output);
				}
			}
		}

		static bool IncludeNamespace(string? ns, List<string> includeList)
		{
			if (includeList.Count == 0) return true;
			string candidate = ns ?? string.Empty;
			foreach (var inc in includeList)
			{
				var token = inc.Trim();
				if (string.IsNullOrEmpty(token)) continue;
				// 简易包含匹配：* 通配仅末尾
				if (token.EndsWith("*", StringComparison.Ordinal))
				{
					var prefix = token.Substring(0, token.Length - 1);
					if (candidate.StartsWith(prefix, StringComparison.Ordinal)) return true;
				}
				else if (candidate.Contains(token, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		static bool HasAttribute(ICustomAttributeProvider provider, string fullName)
		{
			if (!provider.HasCustomAttributes) return false;
			foreach (var ca in provider.CustomAttributes)
			{
				if (ca.AttributeType.FullName == fullName) return true;
			}
			return false;
		}

		static CustomAttribute? GetAttribute(ICustomAttributeProvider provider, string fullName)
		{
			if (!provider.HasCustomAttributes) return null;
			foreach (var ca in provider.CustomAttributes)
			{
				if (ca.AttributeType.FullName == fullName) return ca;
			}
			return null;
		}

		static string GetAttributeString(CustomAttribute attr, string name)
		{
			foreach (var na in attr.Properties)
			{
				if (na.Name == name && na.Argument.Value is string s) return s;
			}
			return string.Empty;
		}

		static int? GetAttributeInt(CustomAttribute attr, string name)
		{
			foreach (var na in attr.Properties)
			{
				if (na.Name == name && na.Argument.Value is int i) return i;
			}
			return null;
		}

		static int? GetCtorIntArg(CustomAttribute attr, int index)
		{
			if (index < attr.ConstructorArguments.Count)
			{
				var arg = attr.ConstructorArguments[index];
				if (arg.Value is int i) return i;
			}
			return null;
		}

		EnumDescription? TryBuildEnum(TypeDefinition type)
		{
			// 需要有 ProtoEnum 标注或作为枚举类型直接导出
			bool hasAnyProtoEnum = false;
			foreach (var f in type.Fields)
			{
				if (HasAttribute(f, "ProtoBuf.ProtoEnumAttribute"))
				{
					hasAnyProtoEnum = true;
					break;
				}
			}

			if (!hasAnyProtoEnum && !HasAttribute(type, "ProtoBuf.ProtoContractAttribute"))
			{
				// 没有明确的 protobuf 标记，跳过
				return null;
			}

			var e = new EnumDescription
			{
				Namespace = type.Namespace ?? string.Empty,
				Name = SanitizeTypeName(type)
			};

			foreach (var field in type.Fields)
			{
				if (!field.IsStatic) continue;
				if (!field.HasConstant) continue;
				var ev = new EnumValueDescription
				{
					Name = field.Name,
					Number = Convert.ToInt64(field.Constant)
				};
				e.Values.Add(ev);
			}

			return e;
		}

		ObjectDescription? TryBuildObject(TypeDefinition type)
		{
			if (!HasAttribute(type, "ProtoBuf.ProtoContractAttribute")) return null;

			var obj = new ObjectDescription
			{
				Namespace = type.Namespace ?? string.Empty,
				Name = SanitizeTypeName(type)
			};

			// 处理字段与属性上的 ProtoMember（优先属性，避免重复）
			var usedTags = new HashSet<int>();
			foreach (var prop in type.Properties)
			{
				var fd = BuildFieldFromMember(prop);
				if (fd != null && usedTags.Add(fd.Tag)) obj.Fields.Add(fd);
			}
			foreach (var fld in type.Fields)
			{
				if (fld.Name.EndsWith("k__BackingField", StringComparison.Ordinal)) continue; // 跳过编译器生成
				var fd = BuildFieldFromMember(fld);
				if (fd != null && usedTags.Add(fd.Tag)) obj.Fields.Add(fd);
			}

			// 嵌套类型
			if (type.HasNestedTypes)
			{
				foreach (var nested in type.NestedTypes)
				{
					if (nested.IsEnum)
					{
						var en = TryBuildEnum(nested);
						if (en != null) obj.Enums.Add(en);
					}
					else
					{
						var no = TryBuildObject(nested);
						if (no != null) obj.NestedObjects.Add(no);
					}
				}
			}

			// 排序字段：按 tag
			obj.Fields.Sort((a, b) => a.Tag.CompareTo(b.Tag));
			return obj;
		}

		FieldDescription? BuildFieldFromMember(PropertyDefinition prop)
		{
			if (HasAttribute(prop, "ProtoBuf.ProtoIgnoreAttribute")) return null;
			var attr = GetAttribute(prop, "ProtoBuf.ProtoMemberAttribute");
			if (attr == null) return null;
			return BuildField(prop.PropertyType, prop.Name, attr);
		}

		FieldDescription? BuildFieldFromMember(FieldDefinition fld)
		{
			if (fld.IsStatic) return null;
			if (HasAttribute(fld, "ProtoBuf.ProtoIgnoreAttribute")) return null;
			var attr = GetAttribute(fld, "ProtoBuf.ProtoMemberAttribute");
			if (attr == null) return null;
			return BuildField(fld.FieldType, fld.Name, attr);
		}

		FieldDescription? BuildField(TypeReference typeRef, string memberName, CustomAttribute attr)
		{
			int tag = GetAttributeInt(attr, "Tag") ?? GetCtorIntArg(attr, 0) ?? -1;
			if (tag <= 0) return null;

			string name = GetAttributeString(attr, "Name");
			if (string.IsNullOrEmpty(name)) name = ToCamelCase(memberName);

			bool isRepeated = IsRepeated(typeRef, out var elementType);
			var effective = isRepeated ? elementType : typeRef;

			// bytes 特判：byte[] 或 List<byte> 统一映射为 bytes
			if (IsByteSequence(typeRef))
			{
				return new FieldDescription
				{
					Name = name,
					Tag = tag,
					TypeName = "bytes",
					IsMessageType = false,
					IsEnumType = false,
					Cardinality = FieldCardinality.Optional
				};
			}

			// Nullable<T> => T
			if (IsNullable(effective, out var underlying)) effective = underlying;

			// Dictionary<K,V> => map<k,v>
			if (IsDictionary(effective, out var keyType, out var valueType))
			{
				var keyMapped = MapToProtoType(keyType, out _, out _);
				var valueMapped = MapToProtoType(valueType, out _, out _);
				return new FieldDescription
				{
					Name = name,
					Tag = tag,
					IsMap = true,
					MapKeyTypeName = keyMapped,
					MapValueTypeName = valueMapped,
					Cardinality = FieldCardinality.Optional
				};
			}

			var mapped = MapToProtoType(effective, out bool isMessage, out bool isEnum);

			return new FieldDescription
			{
				Name = name,
				Tag = tag,
				TypeName = mapped,
				IsMessageType = isMessage,
				IsEnumType = isEnum,
				Cardinality = isRepeated ? FieldCardinality.Repeated : FieldCardinality.Optional
			};
		}

		static bool IsRepeated(TypeReference typeRef, out TypeReference elementType)
		{
			// 数组
			if (typeRef is ArrayType at)
			{
				elementType = at.ElementType;
				return true;
			}

			// 泛型集合 List<T>/IList<T>/IEnumerable<T>/ICollection<T>
			if (typeRef is GenericInstanceType git)
			{
				string n = git.ElementType.FullName;
				if (n.StartsWith("System.Collections.Generic.List`1") || n.StartsWith("System.Collections.Generic.IList`1") || n.StartsWith("System.Collections.Generic.IEnumerable`1") || n.StartsWith("System.Collections.Generic.ICollection`1") || n.StartsWith("System.Collections.ObjectModel.Collection`1"))
				{
					elementType = git.GenericArguments[0];
					return true;
				}
			}

			elementType = typeRef;
			return false;
		}

		static bool IsNullable(TypeReference typeRef, out TypeReference underlying)
		{
			underlying = typeRef;
			if (typeRef is GenericInstanceType git)
			{
				if (git.ElementType.FullName == "System.Nullable`1" && git.GenericArguments.Count == 1)
				{
					underlying = git.GenericArguments[0];
					return true;
				}
			}
			return false;
		}

		static bool IsDictionary(TypeReference typeRef, out TypeReference keyType, out TypeReference valueType)
		{
			keyType = typeRef;
			valueType = typeRef;
			if (typeRef is GenericInstanceType git)
			{
				string n = git.ElementType.FullName;
				if ((n == "System.Collections.Generic.Dictionary`2" || n == "System.Collections.Generic.IDictionary`2") && git.GenericArguments.Count == 2)
				{
					keyType = git.GenericArguments[0];
					valueType = git.GenericArguments[1];
					return true;
				}
			}
			return false;
		}

		static bool IsByteSequence(TypeReference typeRef)
		{
			if (typeRef is ArrayType at && at.ElementType.FullName == "System.Byte") return true;
			if (typeRef is GenericInstanceType git)
			{
				string n = git.ElementType.FullName;
				if ((n.StartsWith("System.Collections.Generic.List`1") || n.StartsWith("System.Collections.Generic.IList`1") || n.StartsWith("System.Collections.ObjectModel.Collection`1")) && git.GenericArguments.Count == 1)
				{
					return git.GenericArguments[0].FullName == "System.Byte";
				}
			}
			return false;
		}


		static string MapToProtoType(TypeReference typeRef, out bool isMessage, out bool isEnum)
		{
			isMessage = false;
			isEnum = false;

			string fullName = typeRef.FullName;
			switch (fullName)
			{
				case "System.Boolean": return "bool";
				case "System.String": return "string";
				case "System.Single": return "float";
				case "System.Double": return "double";
				case "System.SByte":
				case "System.Int16":
				case "System.Int32": return "int32";
				case "System.Byte":
				case "System.UInt16":
				case "System.UInt32": return "uint32";
				case "System.Int64": return "int64";
				case "System.UInt64": return "uint64";
				case "System.Byte[]": return "bytes";
				case "System.Guid": return "string"; // 也可考虑 bytes(16)
				case "System.Decimal": return "string"; // 无原生 decimal，默认字符串
				case "System.Uri": return "string";
				case "System.DateTime": return "google.protobuf.Timestamp";
				case "System.DateTimeOffset": return "google.protobuf.Timestamp";
				case "System.TimeSpan": return "google.protobuf.Duration";
			}

			// List<byte> -> bytes（此分支仅做兜底，优先在 BuildField 中识别）
			if (typeRef is GenericInstanceType git2 && git2.ElementType.FullName.StartsWith("System.Collections.Generic.List`1"))
			{
				var t = git2.GenericArguments[0].FullName;
				if (t == "System.Byte") return "bytes";
			}

			// Enum
			if (typeRef.Resolve()?.IsEnum == true)
			{
				isEnum = true;
				return SanitizeTypeName(typeRef.Resolve());
			}

			// 其他引用类型作为 message 处理
			if (!typeRef.IsValueType)
			{
				isMessage = true;
				var td = typeRef.Resolve();
				return td != null ? SanitizeTypeName(td) : typeRef.Name;
			}

			// 兜底
			return "string";
		}

		static string ToCamelCase(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;
			if (name.Length == 1) return name.ToLowerInvariant();
			return char.ToLowerInvariant(name[0]) + name.Substring(1);
		}

		static string SanitizeTypeName(TypeDefinition td)
		{
			string n = td.Name;
			int idx = n.IndexOf('`');
			if (idx >= 0) n = n.Substring(0, idx);
			return n;
		}
	}
}


