# Setup Guide

## Prerequisites

- .NET 10.0 SDK or later
- Access to an IMAP email account
- IDE (Visual Studio, Rider, or VS Code recommended)

## Installation

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd PopfileNet
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

## Configuration

### IMAP Settings

Edit `PopfileNet.Cli/appsettings.json` with your IMAP server details:

| Setting | Description | Example |
|---------|-------------|---------|
| Server | IMAP hostname | `imap.gmail.com` |
| Username | Email address | `user@gmail.com` |
| Password | App password or account password | - |
| Port | IMAP port (usually 993 for SSL) | `993` |
| UseSsl | Enable SSL/TLS | `true` |
| MaxParallelConnections | Connection pool size | `5` |

### Gmail Specific

If using Gmail:
1. Enable 2-Factor Authentication
2. Generate an App Password at https://myaccount.google.com/security
3. Use the App Password as `Password`

## Running

### CLI Commands

```bash
# Test IMAP connection
dotnet run --project PopfileNet.Cli -- test

# Fetch emails (default limit: 40)
dotnet run --project PopfileNet.Cli -- test fetch-mails
dotnet run --project PopfileNet.Cli -- test fetch-mails --limit 100

# Test classifier
dotnet run --project PopfileNet.Cli -- test classifier
```

## Development

### Project Dependencies

```
PopfileNet.Cli
    ├── PopfileNet.Imap
    ├── PopfileNet.Classifier
    └── PopfileNet.Common

PopfileNet.Imap
    └── PopfileNet.Common

PopfileNet.Classifier
    └── PopfileNet.Common
```

### Running Tests

```bash
dotnet test
```

### Code Style

- Follows .NET conventions
- Uses C# 12+ features (primary constructors, collection expressions)
- Nullable reference types enabled
- StyleCop rules configured in `.editorconfig`

## Troubleshooting

### Connection Issues

- Verify IMAP server hostname and port
- Check firewall/network settings
- Ensure SSL/TLS settings match server requirements
- For Gmail/Outlook, use App Passwords

### Build Errors

- Ensure .NET 10.0 SDK is installed
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Restore packages: `dotnet restore`
