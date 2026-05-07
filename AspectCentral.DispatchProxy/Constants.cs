//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="Constants.cs" company="James Consulting LLC">
//    Copyright (c) 2019 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

namespace AspectCentral.DispatchProxy;

/// <summary>
/// Shared cached <see cref="Type" /> tokens used by the registration and proxy-construction pipeline.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Cached <see cref="Type" /> reference for <see cref="IAspectFactory" />. Used by
    /// <see cref="DispatchProxyAspectRegistrationBuilder" /> and the assembly-scanning code in
    /// <see cref="ServiceCollectionExtensions" /> to identify aspect factory implementations
    /// without paying the cost of <c>typeof</c> lookups in hot paths.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly Type IAspectFactoryType = typeof(IAspectFactory);
}