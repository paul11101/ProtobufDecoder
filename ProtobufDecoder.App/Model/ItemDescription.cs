using System;

namespace ProtobufDecoder.App.Model
{
	public abstract class ItemDescription
	{
		public string Namespace { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string FullName => string.IsNullOrEmpty(Namespace) ? Name : Namespace + "." + Name;
		public string? Comment { get; set; }
	}
}




