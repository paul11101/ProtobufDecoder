using System.Collections.Generic;

namespace ProtobufDecoder.App.Model
{
	public sealed class EnumValueDescription
	{
		public string Name { get; set; } = string.Empty;
		public long Number { get; set; }
		public string? Comment { get; set; }
	}

	public sealed class EnumDescription : ItemDescription
	{
		public List<EnumValueDescription> Values { get; } = new List<EnumValueDescription>();
	}
}




