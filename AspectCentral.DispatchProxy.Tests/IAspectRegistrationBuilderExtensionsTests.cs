//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="IAspectRegistrationBuilderExtensionsTests.cs" company="James Consulting LLC">
//    Copyright (c) 2019 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using AspectCentral.Abstractions;
using NSubstitute;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

public class AspectRegistrationBuilderExtensionsTests
{
    private readonly IAspectRegistrationBuilder _aspectRegistrationBuilder;

    public AspectRegistrationBuilderExtensionsTests()
    {
        _aspectRegistrationBuilder = Substitute.For<IAspectRegistrationBuilder>();
    }

    [Fact]
    public void AddAspectThrowsArgumentNullExceptionWhenAspectRegistrationBuilderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            default(IAspectRegistrationBuilder)!.AddAspectViaFactory<TestAspectFactory>());
    }

    [Fact]
    public void AddAspectCallsAddAspectWhenArgumentsAreValid()
    {
        _aspectRegistrationBuilder.AddAspectViaFactory<TestAspectFactory>();
        _aspectRegistrationBuilder.Received(1)
            .AddAspect(TestAspectFactory.Type, null, Arg.Is<MethodInfo[]>(m => m.Length == 0));
    }
}