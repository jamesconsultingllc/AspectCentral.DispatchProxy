//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="ShortCircuitAspectTests.cs" company="James Consulting LLC">
//    Copyright (c) 2026 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  ----------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Coverage tests for the short-circuit branches of
/// <c>BaseAspect&lt;T&gt;.DispatchIntercepted</c> and
/// <c>BaseAspect&lt;T&gt;.HandleAsyncShortCircuit</c>:
/// (1) sync method with <c>InvokeMethod=false</c> and no Task supplied (fallback PostInvoke), and
/// (2) async method with <c>InvokeMethod=false</c> returning a non-generic
/// <see cref="Task" /> (Task continuation path).
/// </summary>
public class ShortCircuitAspectTests
{
    private static readonly Type ITestInterfaceType = typeof(ITestInterface);

    private ITestInterface CreateProxy()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(typeof(MyTestInterface).FullName!).Returns(new RecordingLogger());
        var provider = Substitute.For<IAspectConfigurationProvider>();
        provider.ShouldIntercept(Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<MethodInfo>())
            .Returns(true);
        var config = new AspectConfiguration(
            new ServiceDescriptor(ITestInterfaceType, MyTestInterface.Type, ServiceLifetime.Transient));
        config.AddEntry(TestAspectFactory.Type, methodsToIntercept: ITestInterfaceType.GetMethods());
        provider.AddEntry(config);
        return ShortCircuitAspect<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface),
            loggerFactory, provider);
    }

    /// <summary>
    /// Sync method, InvokeMethod=false, no Task supplied — exercises the fallback
    /// <c>PostInvoke(aspectContext)</c> branch in <c>DispatchIntercepted</c>.
    /// </summary>
    [Fact]
    public void SyncShortCircuit_RunsPostInvoke()
    {
        ShortCircuitAspect<ITestInterface>.SupplyNonGenericTask = false;
        ShortCircuitAspect<ITestInterface>.PostInvokeRan = false;

        var proxy = CreateProxy();
        proxy.Test(1, "x", new MyUnitTestClass(1, "x"));

        Assert.True(ShortCircuitAspect<ITestInterface>.PostInvokeRan);
    }

    /// <summary>
    /// Async method returning <see cref="Task" /> (non-generic), InvokeMethod=false,
    /// non-generic Task supplied — exercises the <c>ContinueWith</c> branch of
    /// <c>HandleAsyncShortCircuit</c>.
    /// </summary>
    [Fact]
    public async Task NonGenericTaskShortCircuit_RunsPostInvoke()
    {
        ShortCircuitAspect<ITestInterface>.SupplyNonGenericTask = true;
        ShortCircuitAspect<ITestInterface>.PostInvokeRan = false;

        var proxy = CreateProxy();
        await proxy.TestAsync(1, "x", new MyUnitTestClass(1, "x"));

        Assert.True(ShortCircuitAspect<ITestInterface>.PostInvokeRan);
    }
}
