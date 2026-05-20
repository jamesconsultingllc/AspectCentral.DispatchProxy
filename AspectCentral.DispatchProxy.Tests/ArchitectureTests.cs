using System.Reflection;
using AspectCentral.DispatchProxy.Telemetry;
using NetArchTest.Rules;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Library_ShouldNot_DependOn_AspNetCore()
    {
        var result = Types.InAssembly(typeof(BaseAspect<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.Hosting")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void TelemetrySources_ShouldFollow_NamingConvention()
    {
        foreach (var source in LibraryActivitySources.All)
            Assert.StartsWith("AspectCentral.DispatchProxy.", source);

        foreach (var meter in LibraryMeters.All)
            Assert.StartsWith("AspectCentral.DispatchProxy.", meter);
    }

    [Fact]
    public void AspectFactories_Should_Inherit_From_BaseAspectFactory()
    {
        var result = Types.InAssembly(typeof(BaseAspect<>).Assembly)
            .That().HaveNameEndingWith("AspectFactory")
            .And().DoNotHaveName("BaseAspectFactory")
            .And().AreNotAbstract()
            .Should().Inherit(typeof(BaseAspectFactory))
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void BaseAspect_ShouldNot_DeclareStaticFields()
    {
        var staticFields = typeof(BaseAspect<>)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(field => !field.IsLiteral);

        Assert.Empty(staticFields);
    }
}