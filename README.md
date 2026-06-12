# Keel

A foundational project template designed to support the development of microservices using .NET.

## 📦 Publishing Packages

Each project within Keel has its own GitHub Actions workflow configured for creating and publishing NuGet packages to both **GitHub Packages** and **NuGet.org**.

### 🚀 How to Publish

You can trigger the publishing process in two ways:

1. **Manual Publish:** Trigger the workflow manually for the specific project via the `Actions` tab in GitHub.
2. **Publish by Tag:** Create and push a Git tag using the format `<PackageId>-v<version>`. 
   * ⚠️ *Important:* The `<version>` value must exactly match the `Version` property evaluated by MSBuild in the project file.
   * ⚠️ *Note:* Push a maximum of three tags at a time. GitHub does not generate tag push events if more than three tags are pushed simultaneously.

#### Tag Examples
- `Keel.Domain.CleanCode-v1.1.0`
- `Keel.Infra.Db-v1.1.0`
- `Keel.Infra.WebApi-v1.1.0`

### 🌐 Package Destinations

Upon successful execution of the workflow, packages are published to the following registries:
- **GitHub Packages:** `https://nuget.pkg.github.com/evul/index.json`
- **NuGet.org:** `https://www.nuget.org/packages`

### 🔑 Prerequisites for GitHub Actions

To enable publishing to NuGet.org, the following repository secret must be configured:
- `NUGET_API_KEY`: An API key generated on NuGet.org with permissions to push packages.
