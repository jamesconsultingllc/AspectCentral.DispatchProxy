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
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

public class AspectRegistrationBuilderExtensionsTests
{
    private readonly Mock<IAspectRegistrationBuilder> _mockIAspectRegistrationBuilder;

    public AspectRegistrationBuilderExtensionsTests()
    {
        _mockIAspectRegistrationBuilder = new Mock<IAspectRegistrationBuilder>();
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
        _mockIAspectRegistrationBuilder.Object.AddAspectViaFactory<TestAspectFactory>();
        _mockIAspectRegistrationBuilder.Verify(
            x => x.AddAspect(TestAspectFactory.Type, null, It.Is<MethodInfo[]>(m => m.Length == 0)),
            Times.Once);
    }
}