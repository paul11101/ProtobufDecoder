using System;

namespace ProtobufDecoder.App.Model
{
	public enum FieldCardinality
	{
		Optional,
		Required,
		Repeated
	}

	public sealed class FieldDescription
	{
		public string Name { get; set; } = string.Empty;
		public int Tag { get; set; }
		public string TypeName { get; set; } = string.Empty;
		public bool IsMessageType { get; set; }
		public bool IsEnumType { get; set; }
		public FieldCardinality Cardinality { get; set; } = FieldCardinality.Optional;
		public string? OneofGroup { get; set; }
		public string? Comment { get; set; }
		public bool UseOptionalPresence { get; set; }

		// map 支持
		public bool IsMap { get; set; }
		public string? MapKeyTypeName { get; set; }
		public string? MapValueTypeName { get; set; }
	}
}


