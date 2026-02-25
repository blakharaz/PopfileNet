# Incremental Development Guidelines

Build code in small, testable increments.

## Workflow

1. **Write minimal code** - Implement the smallest useful piece
2. **Compile immediately** - Run `dotnet build` after each change
3. **Test the change** - Verify it works before adding more
4. **Commit or continue** - If working, commit; otherwise fix

## Steps for Each Change

### Before Writing Code

- Understand the requirement fully
- Identify the smallest increment that adds value

### While Writing Code

- Write only what's needed
- Avoid speculative code ("I'll need this later")
- Keep methods under 30 lines

### After Writing Code

1. Run `dotnet build` to verify compilation
2. Fix any build errors immediately
3. Run tests if available
4. Verify the change works as expected

## When Stuck

- If build fails: fix errors before continuing
- If tests fail: fix tests or code, don't ignore failures
- If unsure: ask the user before proceeding

## Benefits

- Faster feedback loop
- Easier to identify problems
- Smaller diffs for reviews
- More stable codebase
