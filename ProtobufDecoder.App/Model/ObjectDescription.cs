using System.Collections.Generic;

namespace ProtobufDecoder.App.Model
{
	public sealed class ObjectDescription : ItemDescription
	{
		public List<FieldDescription> Fields { get; } = new List<FieldDescription>();
		public List<OneofDescription> Oneofs { get; } = new List<OneofDescription>();
		public List<EnumDescription> Enums { get; } = new List<EnumDescription>();
		public List<ObjectDescription> NestedObjects { get; } = new List<ObjectDescription>();
	}
}




