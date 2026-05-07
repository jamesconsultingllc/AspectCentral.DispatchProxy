// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfilingAspectRegistrationBuilderExtensionsTests.cs" company="James Consulting LLC">
//   
// </copyright>
//  <summary>
//   The profiling aspect registration builder extensions tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions;
using AspectCentral.DispatchProxy.Profiling;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests.Profiling;

/// <summary>
/// Tests profiling aspect registration extension methods.
/// </summary>
public class ProfilingAspectRegistrationBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that registering profiling with a null builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [Fact]
    public void AddProfilingAspectNullBuilderThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => default(IAspectRegistrationBuilder)!.AddProfilingAspect());
    }

    /// <summary>
    /// Verifies that registering profiling without a method filter applies to all methods.
    /// </summary>
    [Fact]
    public void AddProfilingAspectRegistersAllMethodsWhenNoMethodsAreGiven()
    {
        var builder = new ServiceCollection().AddAspectSupport().AddTransient<ITestInterface, MyTestInterface>()
            .AddProfilingAspect();

        var aspects = builder.AspectConfigurationProvider.ConfigurationEntries.Last().GetAspects().ToArray();
        Assert.Equal(ProfilingAspectFactory.ProfilingAspectFactoryType, aspects[0].AspectType);
    }
}
