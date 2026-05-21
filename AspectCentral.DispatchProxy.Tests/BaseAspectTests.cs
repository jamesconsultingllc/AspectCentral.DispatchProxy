//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspectTests.cs" company="James Consulting LLC">
//    Copyright (c) 2019 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Tests core <see cref="BaseAspect{T}" /> proxy behavior.
/// </summary>
public class BaseAspectTests
{
    // ReSharper disable once InconsistentNaming
    private static readonly Type ITestInterfaceType = typeof(ITestInterface);

    /// <summary>
    /// Proxied test service used by each test.
    /// </summary>
    private readonly ITestInterface _instance;

    /// <summary>
    /// Recording logger used to verify aspect hook execution.
    /// </summary>
    private readonly RecordingLogger _logger;

    /// <summary>
    /// Initializes a proxy whose aspect short-circuits asynchronous method invocation.
    /// </summary>
    public BaseAspectTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _logger = new RecordingLogger();
        loggerFactory.CreateLogger(typeof(MyTestInterface).FullName!).Returns(_logger);
        var aspectConfigurationProvider = Substitute.For<IAspectConfigurationProvider>();
        aspectConfigurationProvider
            .ShouldIntercept(Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<MethodInfo>())
            .Returns(true);
        var aspectConfiguration =
            new AspectConfiguration(new ServiceDescriptor(ITestInterfaceType, MyTestInterface.Type,
                ServiceLifetime.Transient));
        aspectConfiguration.AddEntry(TestAspectFactory.Type, methodsToIntercept: ITestInterfaceType.GetMethods());
        aspectConfiguration.AddEntry(TestAspectFactory2.Type, methodsToIntercept: ITestInterfaceType.GetMethods());
        aspectConfigurationProvider.AddEntry(aspectConfiguration);
        _instance = BaseAspectTestClass<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface),
            loggerFactory, aspectConfigurationProvider);
    }

    /// <summary>
    /// Verifies that an aspect can provide a task result without invoking the target method.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous test.
    /// </returns>
    [Fact]
    public async Task TestCreatingTaskResultWhenMethodNotInvoked()
    {
        // PreInvoke synthesizes a Task<MyUnitTestClass> result and sets InvokeMethod=false, so the
        // real method is never called. Async + InvokeMethod=false: PreInvoke runs (1 log "Setting
        // result"); the supplied Task<TResult> short-circuit continuation unwraps the awaited
        // value into AspectContext.ReturnValue (matching the normal Task<TResult> path) and runs
        // PostInvoke (1 log "PostInvoke ran after short-circuited async completion"). Total: 2 log calls.
        BaseAspectTestClass<ITestInterface>.LastObservedReturnValueType = null;
        var result = await _instance.GetClassByIdAsync(12);
        Assert.Equal(2, _logger.CountAt(LogLevel.Information));
        Assert.Equal(new MyUnitTestClass(12, "testing 123"), result);
        // Verify PostInvoke saw the unwrapped TResult (not the Task<TResult> wrapper).
        Assert.Equal(typeof(MyUnitTestClass), BaseAspectTestClass<ITestInterface>.LastObservedReturnValueType);
    }
}
