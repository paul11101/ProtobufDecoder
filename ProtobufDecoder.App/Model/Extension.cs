namespace ProtobufDecoder.App.Model
{
	public sealed class Extension
	{
		public string TargetFullName { get; set; } = string.Empty;
		public FieldDescription Field { get; set; } = new FieldDescription();
	}
}




