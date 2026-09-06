namespace Transpiler.Core.Tests;

public class NameRegistryTests
{
    [Fact]
    public void Register_ProducesSafeCamelCaseIdentifier()
    {
        var registry = new NameRegistry();

        var name = registry.Register("node-1", "Send Slack Message");

        Assert.Equal("sendSlackMessage", name);
    }

    [Fact]
    public void Register_IsIdempotentForTheSameNodeId()
    {
        var registry = new NameRegistry();

        var first = registry.Register("node-1", "Set Name");
        var second = registry.Register("node-1", "Set Name");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Register_DisambiguatesCollidingNames()
    {
        var registry = new NameRegistry();

        var first = registry.Register("node-1", "Set");
        var second = registry.Register("node-2", "Set");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Register_PrefixesIdentifiersThatWouldStartWithADigit()
    {
        var registry = new NameRegistry();

        var name = registry.Register("node-1", "2nd Request");

        Assert.StartsWith("n_", name);
    }

    [Fact]
    public void CurrentVariable_DefaultsUntilAdvanced()
    {
        var registry = new NameRegistry();

        Assert.Equal("initialItem", registry.CurrentVariable);

        registry.AdvanceCurrent("myVar");

        Assert.Equal("myVar", registry.CurrentVariable);
    }
}
