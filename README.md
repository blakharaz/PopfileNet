# PopfileNet

<!-- coverage-table-start -->
<!-- coverage-table-end -->

An IMAP-based email classification system inspired by [POPFile](https://getpopfile.org). Uses machine learning to automatically categorize emails into folders.

## Features

- **IMAP Integration**: Connect to any IMAP-compatible email server
- **ML Classification**: Naive Bayes classifier for email categorization
- **Web UI**: Blazor-based user interface for configuration and management
- **CLI Tool**: Command-line interface for development testing only
- **Parallel Fetching**: Efficiently download emails using connection pooling

## Projects

| Project | Description |
|---------|-------------|
| `PopfileNet.Ui` | Blazor UI application (user interface) |
| `PopfileNet.Backend` | ASP.NET Core Web API backend |
| `PopfileNet.Imap` | IMAP client services for email operations |
| `PopfileNet.Classifier` | ML.NET-based email classification |
| `PopfileNet.Database` | Database access layer |
| `PopfileNet.Common` | Shared models and interfaces |
| `PopfileNet.Cli` | CLI application (development/testing only) |
| `PopfileNet.Ui.Tests` | UI component tests |

## Quick Start

```bash
# Build the solution
dotnet build

# Run the Web UI
dotnet run --project PopfileNet.Ui
```

Open http://localhost:5000 in your browser.

## Development Testing

The CLI is available for development testing only:

```bash
# Test IMAP connection
dotnet run --project PopfileNet.Cli -- test fetch-mails --limit 20

# Test classifier
dotnet run --project PopfileNet.Cli -- test classifier
```

## Configuration

Edit `PopfileNet.Cli/appsettings.json`:

```json
{
  "ImapSettings": {
    "Server": "imap.example.com",
    "Username": "your@email.com",
    "Password": "your-password",
    "Port": 993,
    "UseSsl": true,
    "MaxParallelConnections": 5
  },
  "Classifications": {
    "Category": "Folder"
  }
}
```

## Requirements

- .NET 10.0+
- IMAP access to email server
