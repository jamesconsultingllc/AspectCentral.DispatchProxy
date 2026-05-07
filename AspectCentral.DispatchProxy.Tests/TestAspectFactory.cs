//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="TestAspectFactory.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Pass-through test aspect factory used as the first aspect in configuration-order assertions.
/// </summary>
public class TestAspectFactory : BaseAspectFactory
{
    /// <summary>
    /// Cached <see cref="Type" /> token for this test factory.
    /// </summary>
    public static readonly Type Type = typeof(TestAspectFactory);

    /// <inheritdoc />
    public TestAspectFactory(ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider) :
        base(loggerFactory, aspectConfigurationProvider)
    {
    }

    /// <inheritdoc />
    public override T Create<T>(T instance, Type implementationType)
    {
        return instance;
    }
}

/// <summary>
/// Pass-through test aspect factory used as the second aspect in configuration-order assertions.
/// </summary>
public class TestAspectFactory2 : BaseAspectFactory
{
    /// <summary>
    /// Cached <see cref="Type" /> token for this test factory.
    /// </summary>
    public static readonly Type Type = typeof(TestAspectFactory2);

    /// <inheritdoc />
    public TestAspectFactory2(ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider) :
        base(loggerFactory, aspectConfigurationProvider)
    {
    }

    /// <inheritdoc />
    public override T Create<T>(T instance, Type implementationType)
    {
        return instance;
    }
}
