---
name: run
description: Run the PopfileNet CLI application
---

Run the PopfileNet CLI to test IMAP connections and fetch emails.

Available commands:
- `dotnet run --project PopfileNet.Cli -- --help` - Show CLI help
- `dotnet run --project PopfileNet.Cli -- test fetch-mails` - Fetch emails (default 40)
- `dotnet run --project PopfileNet.Cli -- test fetch-mails --limit 100` - Fetch with custom limit
- `dotnet run --project PopfileNet.Cli -- test classifier` - Test ML classifier

Use this skill when the user wants to run the CLI application.
