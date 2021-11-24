using Hackney.Core.Testing.Shared;
using Xunit;

namespace PersonListener.Tests
{
    [CollectionDefinition("LogCall collection")]
    public class LogCallAspectFixtureCollection : ICollectionFixture<LogCallAspectFixture>
    { }
}
