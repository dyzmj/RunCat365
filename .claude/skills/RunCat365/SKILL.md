```markdown
# RunCat365 Development Patterns

> Auto-generated skill from repository analysis

## Overview
This skill teaches you the core development patterns, coding conventions, and common workflows for contributing to the RunCat365 C# codebase. RunCat365 is a C# application (no specific framework detected) with features such as CPU/GPU thermal zone monitoring and multi-language localization. The repository uses structured commit messages, clear file organization, and supports contributions through well-defined workflows and commands.

## Coding Conventions

- **File Naming:**  
  Use PascalCase for all file names.  
  _Example:_  
  ```
  TemperatureRepository.cs
  Program.cs
  Strings.Designer.cs
  ```

- **Import Style:**  
  Use relative imports within the project.  
  _Example:_  
  ```csharp
  using RunCat365.Properties;
  ```

- **Export Style:**  
  Use named exports for classes and methods.  
  _Example:_  
  ```csharp
  public class TemperatureRepository
  {
      public double GetCpuTemperature() { ... }
  }
  ```

- **Commit Messages:**  
  - Use prefixes: `fix:`, `feat:`, `refactor:`
  - Keep messages concise (~51 chars on average)
  _Example:_  
  ```
  feat: add GPU thermal zone support for AMD chips
  fix: correct temperature rounding in UI
  refactor: move localization logic to separate class
  ```

## Workflows

### Add or Update Thermal Zone Feature
**Trigger:** When you want to add or update the CPU/GPU thermal zone monitoring functionality.  
**Command:** `/update-thermal-zone`

1. Edit or create `RunCat365/TemperatureRepository.cs` to implement or update thermal zone logic.
2. Edit `RunCat365/Program.cs` to integrate or adjust how the feature is used in the application.
3. Update resource files for localization support:
    - `RunCat365/Properties/Strings.*.resx` (for each supported language)
    - `RunCat365/Properties/Strings.Designer.cs` (auto-generated, ensure it's updated)
4. Commit your changes with a descriptive message, e.g.:
    ```
    feat: add support for new thermal zones in TemperatureRepository
    ```
5. Submit a pull request for review.

_Code Example:_
```csharp
// In TemperatureRepository.cs
public double GetGpuTemperature()
{
    // Implementation for GPU temperature retrieval
}
```

### Update Contribution Guidelines and Templates
**Trigger:** When you want to change how contributors interact with the project (guidelines, templates, review process).  
**Command:** `/update-contributing`

1. Edit `CONTRIBUTING.md` to update the contribution guidelines.
2. Edit or add files in `.github/ISSUE_TEMPLATE/` for issue templates:
    - `bug_report.yml`
    - `feature_request.yml`
3. Edit `.github/pull_request_template.md` to update the pull request template.
4. Commit your changes with a message like:
    ```
    refactor: update contribution guidelines and templates
    ```
5. Submit a pull request for review.

## Testing Patterns

- **Test File Naming:**  
  Test files follow the pattern `*.test.*` (e.g., `TemperatureRepository.test.cs`).
- **Testing Framework:**  
  The specific testing framework is not detected. Check existing test files for conventions.
- **Test Example:**  
  ```csharp
  // TemperatureRepository.test.cs
  [TestMethod]
  public void TestGetCpuTemperature()
  {
      var repo = new TemperatureRepository();
      Assert.IsTrue(repo.GetCpuTemperature() > 0);
  }
  ```

## Commands

| Command                | Purpose                                                      |
|------------------------|--------------------------------------------------------------|
| /update-thermal-zone   | Add or update CPU/GPU thermal zone monitoring functionality  |
| /update-contributing   | Update contribution guidelines and GitHub templates           |
```
