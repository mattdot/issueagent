using Xunit;

namespace IssueAgent.IntegrationTests.Docker;

/// <summary>
/// Docker tests collection to ensure they run sequentially and don't interfere with each other.
/// </summary>
[CollectionDefinition("Docker", DisableParallelization = true)]
public class DockerTestCollection
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}