using System.Diagnostics;
using System.Reflection;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using AspectCentral.DispatchProxy.Profiling;
using AspectCentral.DispatchProxy.Telemetry;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
///     Targeted tests for branches in <see cref="BaseAspect{T}"/>,
///     <see cref="DispatchProxyAspectRegistrationBuilder"/>, and
///     <see cref="ServiceCollectionExtensions"/> that the rest of the suite does not exercise:
///     activity-tag emission, the no-intercept pass-through path, sync exception propagation,
///     async faulted/canceled task telemetry, the empty virtual <c>PreInvoke</c>/<c>PostInvoke</c>
///     defaults, the static <c>Type</c> tokens on the built-in aspects, the params-Assembly[]
///     overload of <c>AddAspectSupport</c>, and the public <c>InvokeCreateFactory</c> override.
/// </summary>
public class CoverageTests
{
    private static readonly Type ITestInterfaceType = typeof(ITestInterface);
    private static readonly Type IThrowingType = typeof(IThrowingTestInterface);

    private static (ILoggerFactory loggerFactory, Mock<ILogger> logger, IAspectConfigurationProvider provider)
        CreateInfrastructure(Type targetType, bool shouldIntercept = true)
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var logger = new Mock<ILogger>();
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        loggerFactoryMock.Setup(x => x.CreateLogger(targetType.FullName!)).Returns(logger.Object);

        var providerMock = new Mock<IAspectConfigurationProvider>();
        providerMock
            .Setup(x => x.ShouldIntercept(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<MethodInfo>()))
            .Returns(shouldIntercept);

        return (loggerFactoryMock.Object, logger, providerMock.Object);
    }

    [Fact]
    public void BaseAspect_EmitsActivityTagsWhenListenerIsAttached()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(MyTestInterface));
        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibraryActivitySources.Aspects,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => { lock (captured) captured.Add(a); }
        };
        ActivitySource.AddActivityListener(listener);

        var instance = PassThroughAspect<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface), loggerFactory, provider);
        instance.Test(1, "abc", new MyUnitTestClass(1, "x"));

        var activity = captured.Should().Contain(a => a.DisplayName == $"{nameof(ITestInterface)}.{nameof(ITestInterface.Test)}").Which;
        activity.GetTagItem("code.namespace").Should().Be(typeof(MyTestInterface).Namespace);
        activity.GetTagItem("code.function").Should().Be(nameof(ITestInterface.Test));
        activity.GetTagItem("aspect.target_type").Should().Be(typeof(MyTestInterface).FullName);
        activity.GetTagItem("aspect.interface_type").Should().Be(typeof(ITestInterface).FullName);
        activity.GetTagItem("aspect.factory").Should().Be(PassThroughAspect<ITestInterface>.Type.FullName);
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Fact]
    public void BaseAspect_PassesThroughWhenShouldInterceptReturnsFalse()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(MyTestInterface), shouldIntercept: false);
        var instance = PassThroughAspect<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface), loggerFactory, provider);

        var act = () => instance.Test(1, "abc", new MyUnitTestClass(1, "x"));
        act.Should().NotThrow();
    }

    [Fact]
    public void BaseAspect_PropagatesSyncExceptionAndRecordsErrorTelemetry()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(ThrowingTestInterface));
        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibraryActivitySources.Aspects,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => { lock (captured) captured.Add(a); }
        };
        ActivitySource.AddActivityListener(listener);

        var instance = PassThroughAspect<IThrowingTestInterface>.Create(
            new ThrowingTestInterface(), typeof(ThrowingTestInterface), loggerFactory, provider);

        var thrown = Assert.Throws<TargetInvocationException>(() => instance.ThrowSync());
        thrown.InnerException.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("sync boom");

        var activity = captured.Should().Contain(a =>
            a.DisplayName == $"{nameof(IThrowingTestInterface)}.{nameof(IThrowingTestInterface.ThrowSync)}").Which;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
    }

    [Fact]
    public async Task BaseAspect_RecordsErrorTelemetryWhenAsyncTaskFaults()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(ThrowingTestInterface));
        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibraryActivitySources.Aspects,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => { lock (captured) captured.Add(a); }
        };
        ActivitySource.AddActivityListener(listener);

        var instance = PassThroughAspect<IThrowingTestInterface>.Create(
            new ThrowingTestInterface(), typeof(ThrowingTestInterface), loggerFactory, provider);

        var task = instance.ThrowAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => task);

        var activity = captured.Should().Contain(a =>
            a.DisplayName == $"{nameof(IThrowingTestInterface)}.{nameof(IThrowingTestInterface.ThrowAsync)}").Which;
        activity.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task BaseAspect_RecordsErrorTelemetryWhenAsyncTaskIsCanceled()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(ThrowingTestInterface));
        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibraryActivitySources.Aspects,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => { lock (captured) captured.Add(a); }
        };
        ActivitySource.AddActivityListener(listener);

        var instance = PassThroughAspect<IThrowingTestInterface>.Create(
            new ThrowingTestInterface(), typeof(ThrowingTestInterface), loggerFactory, provider);

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await instance.ReturnCanceledTask());

        var activity = captured.Should().Contain(a =>
            a.DisplayName == $"{nameof(IThrowingTestInterface)}.{nameof(IThrowingTestInterface.ReturnCanceledTask)}").Which;
        activity.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
    public void BaseAspect_VirtualPreAndPostInvokeDefaultsAreNoOps()
    {
        var (loggerFactory, _, provider) = CreateInfrastructure(typeof(MyTestInterface));

        // PassThroughAspect does not override PreInvoke/PostInvoke — exercising it through a sync
        // call drives the empty virtual defaults declared on BaseAspect<T>.
        var instance = PassThroughAspect<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface), loggerFactory, provider);

        var act = () => instance.Test(7, "ok", new MyUnitTestClass(7, "ok"));
        act.Should().NotThrow();
    }

    [Fact]
    public void LoggingAspect_ExposesOpenGenericType()
    {
        LoggingAspect<ITestInterface>.Type.Should().Be(typeof(LoggingAspect<>));
    }

    [Fact]
    public void ProfilingAspect_ExposesOpenGenericType()
    {
        ProfilingAspect<ITestInterface>.Type.Should().Be(typeof(ProfilingAspect<>));
    }

    [Fact]
    public void AddAspectSupport_WithExplicitAssemblies_RegistersFactoriesFromOnlyThoseAssemblies()
    {
        var services = new ServiceCollection();
        var builder = services.AddAspectSupport(typeof(LoggingAspectFactory).Assembly);

        builder.Should().BeOfType<DispatchProxyAspectRegistrationBuilder>();
        services.Count(x => x.ServiceType == typeof(LoggingAspectFactory)).Should().Be(1);
        services.Count(x => x.ServiceType == typeof(ProfilingAspectFactory)).Should().Be(1);
        // Test-project factories live in a different assembly and therefore must not be picked up.
        services.Count(x => x.ServiceType == typeof(TestAspectFactory)).Should().Be(0);
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

        proxy.Should().NotBeNull().And.BeAssignableTo<ITestInterface>();
    }

    [Fact]
    public void DispatchProxyAspectRegistrationBuilder_InvokeCreateFactory_HandlesImplementationFactoryDescriptors()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var sd = ServiceDescriptor.Describe(typeof(ITestInterface), _ => new MyTestInterface(), ServiceLifetime.Transient);
        var configuration = new AspectConfiguration(sd);
        configuration.AddEntry(LoggingAspectFactory.LoggingAspectFactoryType);

        var provider = new InMemoryAspectConfigurationProvider();
        provider.AddEntry(configuration);

        services.AddAspectSupport(provider);
        var dpBuilder = new DispatchProxyAspectRegistrationBuilder(services, provider);

        using var sp = services.BuildServiceProvider();
        var proxy = dpBuilder.InvokeCreateFactory(sp, configuration);

        proxy.Should().NotBeNull().And.BeAssignableTo<ITestInterface>();
    }
}
