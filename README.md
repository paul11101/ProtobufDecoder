# ProtobufDecoder

A .NET tool for extracting and generating Protocol Buffer definitions from compiled assemblies. This tool analyzes .NET assemblies to reverse-engineer protobuf message structures and generates corresponding `.proto` files and Python classes.

## Features

- **Assembly Analysis**: Parses .NET assemblies to extract protobuf message structures
- **Proto File Generation**: Generates standard Protocol Buffer `.proto` files
- **Python Class Generation**: Creates Python classes with protobuf annotations
- **Namespace Filtering**: Supports filtering by specific namespaces
- **Package Support**: Configurable package names for generated protobuf files

## Requirements

- .NET 9.0 or later
- Mono.Cecil library (automatically included)

## Installation

1. Build the project:
```bash
dotnet build
```

## Usage

### Basic Usage

```bash
dotnet run --project ProtobufDecoder.App
```

This will use default paths:
- Input: `../../../../dummydlls` (relative to executable)
- Output: `../../../../proto_out` (relative to executable)

### Command Line Options

```bash
dotnet run --project ProtobufDecoder.App -- [options]
```

Available options:
- `-i <path>`: Input directory containing assemblies to analyze
- `-o <path>`: Output directory for generated files
- `-pkg <package>`: Package name for generated protobuf files
- `-pythonOut <path>`: Output directory for Python classes
- `-nsInclude <namespaces>`: Semicolon-separated list of namespaces to include

### Examples

```bash
# Analyze specific directory with custom output
dotnet run --project ProtobufDecoder.App -- -i "C:\MyAssemblies" -o "C:\Output"

# Include specific namespaces only
dotnet run --project ProtobufDecoder.App -- -nsInclude "MyNamespace;AnotherNamespace"

# Generate with custom package name
dotnet run --project ProtobufDecoder.App -- -pkg "com.example.protobuf"

# Generate Python classes
dotnet run --project ProtobufDecoder.App -- -pythonOut "C:\PythonOutput"
```

## Project Structure

```
ProtobufDecoder/
├── ProtobufDecoder.App/
│   ├── Program.cs              # Main entry point
│   ├── Model/                  # Data models
│   │   ├── ClassDescription.cs
│   │   ├── EnumDescription.cs
│   │   ├── FieldDescription.cs
│   │   └── ...
│   ├── Parsing/               # Assembly parsing logic
│   │   └── AssemblyParser.cs
│   └── Writers/               # Output generators
│       ├── DescriptionWriter.cs
│       ├── ProtobufWriter.cs
│       └── PythonWriter.cs
└── ProtobufDecoder.sln
```

## How It Works

1. **Assembly Loading**: Uses Mono.Cecil to load and analyze .NET assemblies
2. **Type Analysis**: Scans for types with protobuf attributes and metadata
3. **Structure Extraction**: Extracts field information, types, and relationships
4. **Code Generation**: Generates protobuf definitions and Python classes

## Output

The tool generates:
- **`.proto` files**: Standard Protocol Buffer definitions
- **Python classes**: Python classes with protobuf annotations (optional)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [Mono.Cecil](https://github.com/jbevain/cecil) for assembly analysis
- Inspired by the need to reverse-engineer protobuf structures from compiled assemblies
