# Continue.dev Instructions

## Project Context

PopfileNet - .NET 10 email classification system using IMAP and ML.NET Naive Bayes classifier.

## Project Structure

```
PopfileNet.sln
├── PopfileNet.Cli/           # CLI entry (System.CommandLine)
├── PopfileNet.Imap/          # MailKit IMAP client
├── PopfileNet.Classifier/   # ML.NET classification
├── PopfileNet.Common/        # Domain models & interfaces
```

## Code Guidelines

IMPORTANT: Follow these guidelines for all code changes.

### Coding Standards

See `docs/coding-standards.md` for detailed C# standards:
- C# 12+ with primary constructors
- Nullable reference types enabled
- Collection expressions `[]` instead of `new List<T>()`
- PascalCase for public API, camelCase for private
- Interfaces with `I` prefix

### Incremental Development

See `docs/incremental-development.md`:
- Write minimal code, compile immediately
- Run `dotnet build` after each change
- Test before adding more code

## Key Libraries

- **MailKit** - IMAP/SMTP protocols
- **Microsoft.ML** - Machine learning
- **Microsoft.Extensions** - Configuration, logging, DI
- **System.CommandLine** - CLI framework
- **MimeKit** - MIME parsing

## Important Classes

| File | Purpose |
|------|---------|
| `PopfileNet.Cli/Program.cs` | CLI entry, command routing |
| `PopfileNet.Imap/Services/ImapClientService.cs` | IMAP operations, connection pool |
| `PopfileNet.Classifier/NaiveBayesianClassifier.cs` | ML training & prediction |
| `PopfileNet.Common/Email.cs` | Core email model |

## Configuration

IMAP settings in `PopfileNet.Cli/appsettings.json`:
- Server, Username, Password, Port, UseSsl
- MaxParallelConnections for connection pooling
