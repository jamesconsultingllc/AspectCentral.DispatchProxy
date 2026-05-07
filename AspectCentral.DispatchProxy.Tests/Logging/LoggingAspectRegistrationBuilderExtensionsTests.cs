// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingAspectRegistrationBuilderExtensionsTests.cs" company="James Consulting LLC">
//   
// </copyright>
//  <summary>
//   The logging aspect registration builder extensions tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions;
using AspectCentral.DispatchProxy.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests.Logging;

/// <summary>
/// Tests logging aspect registration extension methods.
/// </summary>
public class LoggingAspectRegistrationBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that registering logging with a null builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [Fact]
    public void AddLoggingAspectNullBuilderThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => default(IAspectRegistrationBuilder)!.AddLoggingAspect());
    }

    /// <summary>
    /// Verifies that registering logging without a method filter applies to all methods.
    /// </summary>
    [Fact]
    public void AddLoggingAspectRegistersAllMethodsWhenNoMethodsAreGiven()
    {
        var builder = new ServiceCollection().AddAspectSupport().AddTransient<ITestInterface, MyTestInterface>()
            .AddLoggingAspect();

        var aspects = builder.AspectConfigurationProvider.ConfigurationEntries[0].GetAspects().ToArray();
        Assert.Equal(typeof(MyTestInterface),
            builder.AspectConfigurationProvider.ConfigurationEntries[0].ServiceDescriptor.ImplementationType);
        Assert.Single(aspects);
        Assert.Equal(LoggingAspectFactory.LoggingAspectFactoryType, aspects[0].AspectType);
    }
}
