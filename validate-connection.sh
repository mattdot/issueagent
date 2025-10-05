#!/bin/bash

# Azure AI Foundry Connection Validation Script
# This script validates the Azure AI Foundry connection using the bootstrap integration

set -e

echo "========================================"
echo "Azure AI Foundry Connection Validator"
echo "========================================"
echo ""

# Check environment variables
if [ -z "$ISSUE_AGENT_ENDPOINT" ]; then
    echo "❌ ERROR: ISSUE_AGENT_ENDPOINT environment variable is not set"
    exit 1
fi

if [ -z "$ISSUE_AGENT_KEY" ]; then
    echo "❌ ERROR: ISSUE_AGENT_KEY environment variable is not set"
    exit 1
fi

echo "✓ Environment variables configured:"
echo "  - ISSUE_AGENT_ENDPOINT: ${ISSUE_AGENT_ENDPOINT:0:40}..."
echo "  - ISSUE_AGENT_KEY: ${ISSUE_AGENT_KEY:0:20}... (84 chars total)"
echo ""

# Create a temporary C# test program
TEMP_DIR=$(mktemp -d)
TEMP_PROJECT="$TEMP_DIR/validator.csproj"
TEMP_PROGRAM="$TEMP_DIR/Program.cs"

echo "Creating temporary validation program..."

# Create the project file
cat > "$TEMP_PROJECT" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="/workspaces/issueagent/src/IssueAgent.Agent/IssueAgent.Agent.csproj" />
    <ProjectReference Include="/workspaces/issueagent/src/IssueAgent.Shared/IssueAgent.Shared.csproj" />
  </ItemGroup>
</Project>
EOF

# Create the program
cat > "$TEMP_PROGRAM" << 'EOF'
using System.Diagnostics;
using IssueAgent.Agent.Runtime;
using IssueAgent.Shared.Models;

var endpoint = Environment.GetEnvironmentVariable("ISSUE_AGENT_ENDPOINT");
var apiKey = Environment.GetEnvironmentVariable("ISSUE_AGENT_KEY");

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("❌ ERROR: Required environment variables not set");
    return 1;
}

// Create configuration
var config = new AzureFoundryConfiguration
{
    Endpoint = endpoint,
    ApiKey = apiKey,
    ModelDeploymentName = "gpt-4o-mini",
    ApiVersion = "2024-05-01-preview",
    ConnectionTimeout = TimeSpan.FromSeconds(30)
};

Console.WriteLine("Validating configuration...");
try
{
    config.Validate();
    Console.WriteLine("✓ Configuration is valid");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Configuration validation failed: {ex.Message}");
    return 1;
}

Console.WriteLine("");
Console.WriteLine("Attempting connection to Azure AI Foundry...");
var sw = Stopwatch.StartNew();

try
{
    await AgentBootstrap.InitializeAzureFoundryAsync(config, CancellationToken.None);
    sw.Stop();
    
    var endpointSuffix = new Uri(endpoint).Host.Split('.')[0];
    Console.WriteLine($"✅ SUCCESS: Connected to Azure AI Foundry");
    Console.WriteLine($"   Endpoint: ...{endpointSuffix}");
    Console.WriteLine($"   Duration: {sw.ElapsedMilliseconds}ms");
    
    if (sw.ElapsedMilliseconds > 3000)
    {
        Console.WriteLine($"⚠️  WARNING: Connection took longer than target (3000ms)");
    }
    
    return 0;
}
catch (Exception ex)
{
    sw.Stop();
    Console.WriteLine($"❌ FAILED: Connection failed after {sw.ElapsedMilliseconds}ms");
    Console.WriteLine($"   Error: {ex.GetType().Name}");
    Console.WriteLine($"   Message: {ex.Message}");
    
    // Can't use ConnectionErrorCategory.FromException since it's just an enum
    Console.WriteLine($"   Stack: {ex.StackTrace?.Split('\n')[0] ?? "N/A"}");
    
    return 1;
}
EOF

echo "Running validation..."
echo ""

# Run the program
cd "$TEMP_DIR"
dotnet run --verbosity quiet

EXIT_CODE=$?

# Cleanup
rm -rf "$TEMP_DIR"

echo ""
if [ $EXIT_CODE -eq 0 ]; then
    echo "========================================"
    echo "✅ Validation completed successfully"
    echo "========================================"
else
    echo "========================================"
    echo "❌ Validation failed"
    echo "========================================"
fi

exit $EXIT_CODE
