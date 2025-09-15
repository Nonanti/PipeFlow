# Contributing to PipeFlow

First off, thanks for taking the time to contribute!

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When you create a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples to demonstrate the steps**
- **Describe the behavior you observed and what you expected**
- **Include code samples and stack traces if applicable**

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:

- **Use a clear and descriptive title**
- **Provide a detailed description of the suggested enhancement**
- **Provide specific examples to demonstrate how it would work**
- **Explain why this enhancement would be useful**

### Pull Requests

1. Fork the repo and create your branch from `main`
2. If you've added code that should be tested, add tests
3. Ensure the test suite passes (`dotnet test`)
4. Make sure your code follows the existing style
5. Issue that pull request!

## Development Setup

```bash
# Clone your fork
git clone https://github.com/your-username/PipeFlow.git
cd PipeFlow

# Build the project
dotnet build

# Run tests
dotnet test

# Run benchmarks
dotnet run --project PipeFlow.Benchmarks -c Release
```

## Code Style

- Use 4 spaces for indentation (no tabs)
- Keep lines under 120 characters when possible
- Follow C# naming conventions
- Write self-documenting code, avoid unnecessary comments
- LINQ-style for simple operations, loops for complex logic

## Testing

- Write unit tests for new functionality
- Ensure all tests pass before submitting PR
- Include both positive and negative test cases
- Test edge cases and boundary conditions

## Commit Messages

- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters or less
- Reference issues and pull requests liberally after the first line

## Questions?

Feel free to open an issue with your question or reach out to the maintainers.

Thanks for contributing!