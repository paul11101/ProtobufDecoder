using System.Collections.Generic;

namespace ProtobufDecoder.App.Model
{
	public sealed class OneofDescription
	{
		public string Name { get; set; } = string.Empty;
		public List<FieldDescription> Fields { get; } = new List<FieldDescription>();
	}
}




