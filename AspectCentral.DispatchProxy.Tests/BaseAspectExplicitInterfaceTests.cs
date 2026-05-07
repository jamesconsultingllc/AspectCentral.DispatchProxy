using System.Reflection;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Regression tests for <see cref="BaseAspect{T}.GenerateMethodNameWithArguments" />
/// covering implementation-method resolution scenarios that the legacy
/// <c>GetMethods()</c> + <c>ToString()</c> lookup could not handle:
/// explicit interface implementations and overloaded interface methods.
/// </summary>
public class BaseAspectExplicitInterfaceTests
{
    private static BaseAspectTestClass<IExplicit> CreateAspect()
    {
        var providerMock = new Mock<IAspectConfigurationProvider>();
        providerMock
            .Setup(x => x.ShouldIntercept(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<MethodInfo>()))
            .Returns(false);
        var provider = providerMock.Object;
        var config =
            new AspectConfiguration(new ServiceDescriptor(typeof(IExplicit), typeof(ExplicitImpl),
                ServiceLifetime.Transient));
        provider.AddEntry(config);

        var loggerFactory = NullLoggerFactory.Instance;
        return (BaseAspectTestClass<IExplicit>)BaseAspectTestClass<IExplicit>.Create(
            new ExplicitImpl(), typeof(ExplicitImpl), loggerFactory, provider);
    }

    [Fact]
    public void Resolves_OverloadedMethod_TwoArgs()
    {
        var aspect = CreateAspect();
        var target = typeof(IExplicit).GetMethod(nameof(IExplicit.Add), new[] { typeof(int), typeof(int) })!;

        aspect.GenerateMethodNameWithArguments(target, new object[] { 1, 2 }, out var impl);

        Assert.Equal(2, impl.GetParameters().Length);
        Assert.Equal(typeof(ExplicitImpl), impl.DeclaringType);
    }

    [Fact]
    public void Resolves_OverloadedMethod_ThreeArgs()
    {
        var aspect = CreateAspect();
        var target =
            typeof(IExplicit).GetMethod(nameof(IExplicit.Add), new[] { typeof(int), typeof(int), typeof(int) })!;

        aspect.GenerateMethodNameWithArguments(target, new object[] { 1, 2, 3 }, out var impl);

        Assert.Equal(3, impl.GetParameters().Length);
        Assert.Equal(typeof(ExplicitImpl), impl.DeclaringType);
    }

    [Fact]
    public void Resolves_ExplicitInterfaceImplementation()
    {
        // Hidden() is implemented as `string IExplicit.Hidden()` — this method does NOT
        // appear in ExplicitImpl.GetMethods() (it is private). The legacy code path
        // would throw InvalidOperationException from .Single(...) here.
        var aspect = CreateAspect();
        var target = typeof(IExplicit).GetMethod(nameof(IExplicit.Hidden))!;

        aspect.GenerateMethodNameWithArguments(target, Array.Empty<object>(), out var impl);

        Assert.Equal(typeof(ExplicitImpl), impl.DeclaringType);
        Assert.Equal("Hidden", impl.Name.Split('.')[^1]);
    }

    public interface IExplicit
    {
        int Add(int x, int y);
        int Add(int x, int y, int z);
        string Hidden();
    }

    public class ExplicitImpl : IExplicit
    {
        public int Add(int x, int y)
        {
            return x + y;
        }

        public int Add(int x, int y, int z)
        {
            return x + y + z;
        }

        // Explicit interface implementation — not visible via GetMethods()
        string IExplicit.Hidden()
        {
            return "hidden";
        }
    }
}