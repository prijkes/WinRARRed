---
description: C# style guide, naming conventions, and patterns to use/avoid
paths:
  - "**/*.cs"
---

# Coding Conventions

## C# Style

- **File-scoped namespaces** - all files use `namespace X;` not `namespace X { }`
- **Primary constructors** for dependency injection where applicable
- **Nullable reference types** enabled - use `?` for nullable, handle null cases
- **Collection expressions** - use `[]` syntax for empty collections
- **Target-typed new** - use `new()` when type is clear from context

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `RARHeaderReader` |
| Methods | PascalCase | `ParseBlock()` |
| Properties | PascalCase | `CompressionMethod` |
| Private fields | camelCase | `commentFilePath` |
| Constants | PascalCase | `OffsetHostOS` |
| Enums | PascalCase, singular | `RAR4BlockType.FileHeader` |
| Event handlers | On + EventName | `OnBruteForceProgress` |
| Async methods | Suffix with Async | `BruteForceRARVersionAsync` |

## Pattern Rules

### DO
- Use `CancellationToken` on all async methods that may be long-running
- Use events for progress/status callbacks (not delegates or callbacks)
- Use options classes for configuration (RAROptions, BruteForceOptions)
- Use XML doc comments on public APIs
- Use `StringComparer.OrdinalIgnoreCase` for file path dictionaries
- Log at appropriate levels: `Debug` for details, `Information` for milestones, `Warning` for recoverable issues

### DON'T
- Don't use `Task.Run` for I/O-bound operations
- Don't catch `Exception` without re-throwing or logging
- Don't use magic numbers - define constants
- Don't use `async void` except for event handlers
- Don't block on async code (no `.Result` or `.Wait()`)

## Error Handling

```csharp
// Preferred: Result pattern for expected failures
public bool TryParseVersion(string name, out int version)

// Use exceptions only for unexpected/unrecoverable errors
if (headerSize < 7)
    throw new InvalidDataException("Invalid RAR header size");

// Log and continue for recoverable issues
catch (Exception ex)
{
    Log.Warning(this, $"Failed to patch {filePath}: {ex.Message}");
}
```

## Binary Data Handling

```csharp
// Use BinaryReader for structured binary data
using var reader = new BinaryReader(fs);
ushort headerSize = reader.ReadUInt16();

// Use BitConverter for inline conversions
uint value = BitConverter.ToUInt32(buffer, offset);

// Use struct packing for performance-critical parsing
byte[] fullHeader = reader.ReadBytes(headerSize);
```

## Logging

Uses Serilog. Log source is `this` for instance methods:

```csharp
Log.Information(this, $"Starting brute force: {releaseDir}");
Log.Debug(this, $"Testing version {version} with args {args}");
Log.Warning(this, $"Skipped {count} invalid entries");
Log.Error(this, $"CRC mismatch: expected {expected}, got {actual}");
```
