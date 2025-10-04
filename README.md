# issueagent

## Development Environment

This repository includes a devcontainer configuration for a consistent development environment with:

- **.NET 8.0 SDK** - Full .NET development tools
- **uv/uvx** - Fast Python package installer and runner
- **specify-cli** - GitHub spec-kit CLI tool
- **VS Code Extensions** - Pre-configured extensions for .NET and Python development

### Using the Devcontainer

1. Open this repository in VS Code
2. Install the "Dev Containers" extension if not already installed
3. Click "Reopen in Container" when prompted, or use Command Palette: `Dev Containers: Reopen in Container`
4. The container will build and install all required tools automatically

The devcontainer automatically verifies the installation by running `dotnet --version` and `uv --version`, and installs the specify-cli tool from the GitHub spec-kit repository after creation.