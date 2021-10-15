//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="IAspectRegistrationBuilderExtensionsTests.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using AspectCentral.Abstractions;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests
{
    public class IAspectRegistrationBuilderExtensionsTests
    {
        private readonly Mock<IAspectRegistrationBuilder> mockIAspectRegistrationBuilder;
        
        public IAspectRegistrationBuilderExtensionsTests()
        {
            mockIAspectRegistrationBuilder = new Mock<IAspectRegistrationBuilder>();
        }
        
        [Fact]
        public void AddAspectThrowsArgumentNullExceptionWhenAspectRegistrationBuilderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => default(IAspectRegistrationBuilder).AddAspectViaFactory<TestAspectFactory>());
        }

        [Fact]
        public void AddAspectCallsAddAspectWhenArgumentsAreValid()
        {
            mockIAspectRegistrationBuilder.Object.AddAspectViaFactory<TestAspectFactory>();
            mockIAspectRegistrationBuilder.Verify(x => x.AddAspect(TestAspectFactory.Type, null, new MethodInfo[0]), Times.Once);
        }

    }
}