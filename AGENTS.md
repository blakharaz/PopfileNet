# Agents

This file helps OpenCode understand the project structure and coding patterns.

## Project Overview

PopfileNet is a .NET 10 solution for IMAP-based email classification using ML.NET.

## Projects

| Project | Description |
|---------|-------------|
| PopfileNet.Cli | CLI application using System.CommandLine |
| PopfileNet.Imap | IMAP client using MailKit |
| PopfileNet.Classifier | ML.NET Naive Bayes classifier |
| PopfileNet.Common | Shared domain models and interfaces |
| PopfileNet.Backend | ASP.NET Core Web API backend |
| PopfileNet.Ui | Blazor UI application |
| PopfileNet.Ui.Tests | UI component tests |
| PopfileNet.Database | Database access layer |

## Key Files

- `PopfileNet.Ui/Program.cs` - Web UI entry point
- `PopfileNet.Backend/Program.cs` - Web API entry point
- `PopfileNet.Imap/Services/ImapClientService.cs` - IMAP operations with connection pooling
- `PopfileNet.Classifier/NaiveBayesianClassifier.cs` - ML model training/prediction
- `PopfileNet.Common/Email.cs` - Core email domain model
- `PopfileNet.Cli/Program.cs` - CLI entry point (development/testing only)

## Agents

Use @dotnet-builder for build/test operations.
Use @ml-classifier for ML-related questions.
Use @imap-expert for IMAP/email operations.

## Skills

Use /build to build the solution.
Use /test to run tests.
Use /run to execute CLI.
Use /clean for clean rebuild.

## Coding Guidelines

Follow these external guidelines for all code changes:

- @docs/coding-standards.md - C# coding standards and conventions
- @docs/incremental-development.md - Build in small, testable increments

## Coding Conventions

- C# 12+ with primary constructors
- Nullable reference types enabled
- Collection expressions `[]` instead of `new List<T>()`
- Interfaces in PopfileNet.Common for abstractions
- Configuration via appsettings.json
