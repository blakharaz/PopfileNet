# PopfileNet

An IMAP-based email classification system inspired by [POPFile](https://getpopfile.org). Uses machine learning to automatically categorize emails into folders.

## Features

- **IMAP Integration**: Connect to any IMAP-compatible email server
- **ML Classification**: Naive Bayes classifier for email categorization
- **CLI Tool**: Command-line interface for testing and training
- **Parallel Fetching**: Efficiently download emails using connection pooling

## Projects

| Project | Description |
|---------|-------------|
| `PopfileNet.Cli` | Main CLI application entry point |
| `PopfileNet.Imap` | IMAP client services for email operations |
| `PopfileNet.Classifier` | ML.NET-based email classification |
| `PopfileNet.Common` | Shared models and interfaces |

## Quick Start

```bash
# Build the solution
dotnet build

# Configure IMAP settings in PopfileNet.Cli/appsettings.json

# Test IMAP connection and fetch emails
dotnet run --project PopfileNet.Cli -- test fetch-mails --limit 20
```

## Usage

```bash
# Fetch emails from IMAP server
dotnet run --project PopfileNet.Cli -- test fetch-mails -l 50

# Test classifier with sample data
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
