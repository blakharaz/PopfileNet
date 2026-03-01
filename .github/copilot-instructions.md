# GitHub Copilot Instructions

## Project Context

PopfileNet is a .NET 10 solution for IMAP-based email classification using ML. It consists of:

- **PopfileNet.Cli** - CLI application using System.CommandLine
- **PopfileNet.Imap** - IMAP client using MailKit
- **PopfileNet.Classifier** - ML.NET Naive Bayes classifier
- **PopfileNet.Common** - Shared domain models
- **PopfileNet.Backend** - ASP.NET Core Web API backend
- **PopfileNet.Ui** - Blazor UI application
- **PopfileNet.Ui.Tests** - UI component tests
- **PopfileNet.Database** - Database access layer

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

## Coding Conventions

- Use C# 12+ features (primary constructors, collection expressions)
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Follow existing code style in the codebase
- Use interfaces from `PopfileNet.Common` for abstractions
- Configuration via `appsettings.json` with Microsoft.Extensions.Configuration
- Logging via Microsoft.Extensions.Logging

## Patterns

### IMAP Operations
- Services follow `I*Service` interface pattern
- Use `IOptions<T>` for configuration injection
- Connection pooling for parallel operations
- Custom exceptions in `PopfileNet.Imap.Exceptions`

### ML Classification
- Use ML.NET for ML operations
- Training data uses `[LoadColumn]` attributes
- Prediction returns confidence scores
- Model trained with Naive Bayes trainer

### CLI Commands
- Use System.CommandLine library
- Commands created via factory methods
- Subcommands organized by feature area

## Important Files

- `PopfileNet.Ui/Program.cs` - Web UI entry point
- `PopfileNet.Backend/Program.cs` - Web API entry point
- `PopfileNet.Imap/Services/ImapClientService.cs` - Core IMAP logic
- `PopfileNet.Classifier/NaiveBayesianClassifier.cs` - ML model
- `PopfileNet.Common/Email.cs` - Core domain model
- `PopfileNet.Cli/Program.cs` - CLI entry point (development/testing only)

## Testing Guidelines

- Unit tests should cover classifier predictions
- IMAP tests require mock server or test account
- Use xUnit or NUnit for testing framework

## Don't

- Don't add comments unless necessary
- Don't use magic numbers - extract to constants
- Don't commit secrets to repository
