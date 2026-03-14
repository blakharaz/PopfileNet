---
name: test
description: Run .NET tests with dotnet test
---

Run `dotnet test` to execute the test suite.

## Test Categories

### Integration Tests
Integration tests use **Testcontainers** to spin up a real PostgreSQL database in a Docker/Podman container.

```bash
dotnet test Tests/IntegrationTests
```

**Requirements:**
- Docker or Podman must be running
- Testcontainers for .NET must be able to create containers

### Unit Tests
Unit tests don't require external dependencies:
```bash
dotnet test Tests/UnitTests
```

Use this skill when the user wants to run tests or see test results.
