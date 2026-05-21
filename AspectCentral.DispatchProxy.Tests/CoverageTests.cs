using System.Diagnostics;
using System.Reflection;
using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using AspectCentral.DispatchProxy.Profiling;
using AspectCentral.DispatchProxy.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Targeted tests for branches in <see cref="BaseAspect{T}" />,
/// <see cref="DispatchProxyAspectRegistrationBuilder" />, and
/// <see cref="ServiceCollectionExtensions" /> that the rest of the suite does not exercise:
/// activity-tag emission, the no-intercept pass-through path, sync exception propagation,
/// async faulted/canceled task telemetry, the empty virtual <c>PreInvoke</c>/<c>PostInvoke</c>
/// defaults, the static <c>Type</c> tokens on the built-in aspects, the params-Assembly[]
/// overload of <c>AddAspectSupport</c>, and the public <c>InvokeCreateFactory</c> override.
/// </summary>
public class CoverageTests
{
    private static readonly Type TestInterfaceType = typeof(ITestInterface);
    private static readonly Type ThrowingType = typeof(IThrowingTestInterface);

    private static (ILoggerFactory loggerFactory, RecordingLogger logger, IAspectConfigurationProvider provider)
        CreateInfrastructure(Type targetType, bool shouldIntercept = true)
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var logger = new RecordingLogger();
        loggerFactory.CreateLogger(targetType.FullName!).Returns(logger);

        var provider = Substitute.For<IAspectConfigurationProvider>();
        provider
            .ShouldIntercept(Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<MethodInfo>())
            .Returns(shouldIntercept);

        return (loggerFactory, logger, provider);
    }

    [Fact]
    public void BaseAspect_EmitsActivityTagsWhenListenerIsAttached()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(MyTestInterface));
        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibraryActivitySources.Aspects,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = a =>
            {
                lock (captured)
                {
                    captured.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var instance = PassThroughAspect<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface),
            loggerFactory, provider);
        instance.Test(1, "abc", new MyUnitTestClass(1, "x"));

        var activity = Assert.Single(captured,
            a => a.DisplayName == $"{nameof(ITestInterface)}.{nameof(ITestInterface.Test)}");
        Assert.Equal(typeof(MyTestInterface).Namespace, activity.GetTagItem("code.namespace"));
        Assert.Equal(nameof(ITestInterface.Test), activity.GetTagItem("code.function"));
        Assert.Equal(typeof(MyTestInterface).FullName, activity.GetTagItem("aspect.target_type"));
        Assert.Equal(typeof(ITestInterface).FullName, activity.GetTagItem("aspect.interface_type"));
        Assert.Equal(PassThroughAspect<ITestInterface>.Type.FullName, activity.GetTagItem("aspect.factory"));
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public void BaseAspect_PassesThroughWhenShouldInterceptReturnsFalse()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(MyTestInterface), false);
        var instance = PassThroughAspect<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface),
            loggerFactory, provider);

        // No exception is the assertion — if Test throws the test fails.
        instance.Test(1, "abc", new MyUnitTestClass(1, "x"));
    }

    [Fact]
    public void BaseAspect_PropagatesSyncExceptionAndRecordsErrorTelemetry()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(ThrowingTestInterface));
        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibraryActivitySources.Aspects,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = a =>
            {
                lock (captured)
                {
                    captured.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var instance = PassThroughAspect<IThrowingTestInterface>.Create(
            new ThrowingTestInterface(), typeof(ThrowingTestInterface), loggerFactory, provider);

        var thrown = Assert.Throws<TargetInvocationException>(() => instance.ThrowSync());
        var inner = Assert.IsType<InvalidOperationException>(thrown.InnerException);
        Assert.Equal("sync boom", inner.Message);

        var activity = Assert.Single(captured, a =>
            a.DisplayName == $"{nameof(IThrowingTestInterface)}.{nameof(IThrowingTestInterface.ThrowSync)}");
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Single(activity.Events, e => e.Name == "exception");
    }

    [Fact]
    public async Task BaseAspect_RecordsErrorTelemetryWhenAsyncTaskFaults()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(ThrowingTestInterface));
        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibraryActivitySources.Aspects,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = a =>
            {
                lock (captured)
                {
                    captured.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var instance = PassThroughAspect<IThrowingTestInterface>.Create(
            new ThrowingTestInterface(), typeof(ThrowingTestInterface), loggerFactory, provider);

        var task = instance.ThrowAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => task);

        var activity = Assert.Single(captured, a =>
            a.DisplayName == $"{nameof(IThrowingTestInterface)}.{nameof(IThrowingTestInterface.ThrowAsync)}");
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public async Task BaseAspect_RecordsErrorTelemetryWhenAsyncTaskIsCanceled()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(ThrowingTestInterface));
        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibraryActivitySources.Aspects,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = a =>
            {
                lock (captured)
                {
                    captured.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var instance = PassThroughAspect<IThrowingTestInterface>.Create(
            new ThrowingTestInterface(), typeof(ThrowingTestInterface), loggerFactory, provider);

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await instance.ReturnCanceledTask());

        var activity = Assert.Single(captured, a =>
            a.DisplayName ==
            $"{nameof(IThrowingTestInterface)}.{nameof(IThrowingTestInterface.ReturnCanceledTask)}");
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void BaseAspect_VirtualPreAndPostInvokeDefaultsAreNoOps()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(MyTestInterface));

        // PassThroughAspect does not override PreInvoke/PostInvoke — exercising it through a sync
        // call drives the empty virtual defaults declared on BaseAspect<T>.
        var instance = PassThroughAspect<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface),
            loggerFactory, provider);

        // No exception is the assertion — if Test throws the test fails.
        instance.Test(7, "ok", new MyUnitTestClass(7, "ok"));
    }

    [Fact]
    public void LoggingAspect_ExposesOpenGenericType()
    {
        Assert.Equal(typeof(LoggingAspect<>), LoggingAspect<ITestInterface>.Type);
    }

    [Fact]
    public void ProfilingAspect_ExposesOpenGenericType()
    {
        Assert.Equal(typeof(ProfilingAspect<>), ProfilingAspect<ITestInterface>.Type);
    }

    [Fact]
    public void AddAspectSupport_WithExplicitAssemblies_RegistersFactoriesFromOnlyThoseAssemblies()
    {
        var services = new ServiceCollection();
        var builder = services.AddAspectSupport(typeof(LoggingAspectFactory).Assembly);

        Assert.IsType<DispatchProxyAspectRegistrationBuilder>(builder);
        Assert.Equal(1, services.Count(x => x.ServiceType == typeof(LoggingAspectFactory)));
        Assert.Equal(1, services.Count(x => x.ServiceType == typeof(ProfilingAspectFactory)));
        // Test-project factories live in a different assembly and therefore must not be picked up.
        Assert.Equal(0, services.Count(x => x.ServiceType == typeof(TestAspectFactory)));
    }

    [Fact]
    public void AddAspectSupport_WithNullServiceCollection_Throws()
    {
        Assert.Throws<ArgumentNullException>("serviceCollection",
            () => default(IServiceCollection)!.AddAspectSupport(typeof(LoggingAspectFactory).Assembly));
    }

    [Fact]
    public void AddAspectSupport_WithNullAssemblyArray_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>("assembliesToScan",
            () => services.AddAspectSupport(default(Assembly[])!));
    }

    [Fact]
    public void DispatchProxyAspectRegistrationBuilder_InvokeCreateFactory_BuildsProxyForImplementationType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransient<MyTestInterface>();

        var configuration = new AspectConfiguration(
            ServiceDescriptor.Describe(typeof(ITestInterface), typeof(MyTestInterface), ServiceLifetime.Transient));
        configuration.AddEntry(LoggingAspectFactory.LoggingAspectFactoryType);

        var provider = new InMemoryAspectConfigurationProvider();
        provider.AddEntry(configuration);

        var builder = services.AddAspectSupport(provider);
        var dpBuilder = new DispatchProxyAspectRegistrationBuilder(services, provider);

        using var sp = services.BuildServiceProvider();
        var proxy = dpBuilder.InvokeCreateFactory(sp, configuration);

        Assert.NotNull(proxy);
        Assert.IsType<ITestInterface>(proxy, exactMatch: false);
    }

    [Fact]
    public void DispatchProxyAspectRegistrationBuilder_InvokeCreateFactory_HandlesImplementationFactoryDescriptors()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var sd = ServiceDescriptor.Describe(typeof(ITestInterface), _ => new MyTestInterface(),
            ServiceLifetime.Transient);
        var configuration = new AspectConfiguration(sd);
        configuration.AddEntry(LoggingAspectFactory.LoggingAspectFactoryType);

        var provider = new InMemoryAspectConfigurationProvider();
        provider.AddEntry(configuration);

        services.AddAspectSupport(provider);
        var dpBuilder = new DispatchProxyAspectRegistrationBuilder(services, provider);

        using var sp = services.BuildServiceProvider();
        var proxy = dpBuilder.InvokeCreateFactory(sp, configuration);

        Assert.NotNull(proxy);
        Assert.IsType<ITestInterface>(proxy, exactMatch: false);
    }
}
