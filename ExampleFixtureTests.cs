using NUnit.Framework;

[TestFixture]
public class ExampleFixtureTests
{
    [SetUp]
    public void SetUp()
    {
    }

    [Test]
    public void TestFail()
    {
        Assert.Fail();
    }

    [Test]
    public void TestPass()
    {
        Assert.Pass();
    }
}
