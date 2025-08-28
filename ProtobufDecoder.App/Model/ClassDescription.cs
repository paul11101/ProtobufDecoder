using System.Collections.Generic;

namespace ProtobufDecoder.App.Model
{
	public sealed class ClassDescription
	{
		public List<ObjectDescription> Objects { get; } = new List<ObjectDescription>();
		public List<EnumDescription> Enums { get; } = new List<EnumDescription>();
	}
}




