# C# Coding Standards

Follow [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) and [.NET naming guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines).

## General Principles

- Write small increments of code
- Compile and test frequently after each change
- Prefer readability over cleverness
- Keep methods short and focused

## Code Style

### Naming Conventions

- PascalCase for classes, methods, properties, public fields
- camelCase for private fields, local variables, parameters
- _camelCase for backing fields (optional)
- `I` prefix for interfaces (e.g., `IEmailService`)
- Avoid abbreviations unless well-known (ID, URL, API)

### Formatting

- Use default Visual Studio/Rider formatting (Ctrl+K, Ctrl+D)
- 4 spaces for indentation
- One namespace per file
- Opening brace on new line for namespaces/classes
- **One class/record per file** - each type should be in its own file with matching filename

### Language Features

- Use C# 12+ features:
  - Primary constructors for classes/records
  - Collection expressions `[]` instead of `new List<T>()`
  - File-scoped namespaces
  - Pattern matching with `is` and `switch`
  - Null-coalescing `??` and `?.` operators
- Enable and respect nullable reference types
- Use `var` when type is obvious from right side

### Properties vs Fields

- Use properties (with `{ get; set; }`) for public API
- Use private fields for internal state
- Consider init-only properties for immutability

## Async/Await

- Use `async/await` for I/O-bound operations
- Name async methods with `Async` suffix
- Return `Task` or `Task<T>`, not `void` (except events)

## Error Handling

- Use exceptions for exceptional cases
- Don't use exceptions for control flow
- Catch specific exceptions, not base `Exception`
- Log exceptions with appropriate context

## Testing

- Test one thing per test method
- Use descriptive test names: `MethodName_Scenario_ExpectedResult`
- Follow Arrange-Act-Assert pattern
- Keep tests independent and isolated

## Dependencies

- Depend on abstractions (interfaces), not concrete implementations
- Use dependency injection via constructors
- Register dependencies in composition root

## Documentation

- Add XML doc comments for public APIs
- Don't add comments for obvious code
- Write self-documenting code with clear names
