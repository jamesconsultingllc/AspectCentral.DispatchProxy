// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AspectRegistrationTests.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The aspect registration tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Represents a series of tests for aspect registration functionality in the dispatch proxy framework.
/// </summary>
public class AspectRegistrationTests
{
    /// <summary>
    /// Represents the type definition of the ITestInterface used within aspect configuration and testing contexts.
    /// </summary>
    public static readonly Type InterfaceType = typeof(ITestInterface);

    /// <summary>
    /// Represents the type definition for the MyTestInterface.
    /// </summary>
    public static readonly Type MyTestInterfaceType = typeof(MyTestInterface);

    private readonly IServiceCollection _services;

    /// <summary>
    /// Represents a test class to validate aspect registration functionality within the dispatch proxy framework.
    /// </summary>
    public AspectRegistrationTests()
    {
        _services = new ServiceCollection();
        _services.AddTransient<ITestInterface, MyTestInterface>();
        _services.AddLogging(x => { x.AddConsole(); });
        IAspectConfigurationProvider aspectConfigurationProvider = new InMemoryAspectConfigurationProvider();
        var aspectConfiguration =
            new AspectConfiguration(
                new ServiceDescriptor(InterfaceType, MyTestInterfaceType, ServiceLifetime.Transient));
        aspectConfiguration.AddEntry(TestAspectFactory.Type, methodsToIntercept: InterfaceType.GetMethods());
        aspectConfiguration.AddEntry(TestAspectFactory2.Type, methodsToIntercept: InterfaceType.GetMethods());
        aspectConfigurationProvider.AddEntry(aspectConfiguration);
        _services.AddAspectSupport(aspectConfigurationProvider);
    }

    /// <summary>
    /// Validates the registration of generic services within the service collection.
    /// </summary>
    [Fact]
    public void TestGenericRegistration()
    {
        AssertServiceRegisteredCorrectly(_services, ServiceLifetime.Transient);
    }

    /// <summary>
    /// Verifies that a service is registered with the correct configuration in the service collection.
    /// </summary>
    /// <param name="services">
    /// Service collection to validate.
    /// </param>
    /// <param name="lifetime">
    /// Expected service lifetime to verify.
    /// </param>
    private static void AssertServiceRegisteredCorrectly(IServiceCollection services, ServiceLifetime lifetime)
    {
        var sp = services.BuildServiceProvider();
        sp.GetService<ITestInterface>()!.Test(1, string.Empty, new MyUnitTestClass(1, string.Empty));
        Assert.Contains(services, x =>
            x.Lifetime == lifetime && x.ServiceType == MyTestInterfaceType &&
            x.ImplementationType == MyTestInterfaceType);
        Assert.Contains(services, x =>
            x.Lifetime == lifetime && x.ServiceType == InterfaceType && x.ImplementationFactory != null);
        Assert.NotNull(sp.GetService<ITestInterface>());
    }
}
