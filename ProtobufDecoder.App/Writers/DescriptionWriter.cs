using System.Collections.Generic;
using System.IO;
using ProtobufDecoder.App.Model;

namespace ProtobufDecoder.App.Writers
{
	public sealed class DescriptionWriter
	{
		private readonly ProtobufWriter _protoWriter = new ProtobufWriter();
		private readonly PythonWriter _pythonWriter = new PythonWriter();

		public void WriteAll(string outputDirectory, IEnumerable<ObjectDescription> objects, IEnumerable<EnumDescription> enums, string? package, string? pythonOutputDirectory)
		{
			Directory.CreateDirectory(outputDirectory);
			_protoWriter.Write(outputDirectory, objects, enums, package);
			if (!string.IsNullOrEmpty(pythonOutputDirectory))
			{
				_pythonWriter.Write(pythonOutputDirectory!, objects);
			}
		}
	}
}




