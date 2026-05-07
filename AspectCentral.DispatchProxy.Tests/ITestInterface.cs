// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITestInterface.cs" company="James Consulting LLC">
//   
// </copyright>
// // <summary>
//   The Interface interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Test service contract used to verify sync, async, and generic method interception.
/// </summary>
public interface ITestInterface
{
    /// <summary>
    /// Returns a test value object for the supplied identifier.
    /// </summary>
    /// <param name="id">
    /// Identifier copied into the returned test value.
    /// </param>
    /// <returns>
    /// A task that produces a <see cref="MyUnitTestClass" />.
    /// </returns>
    Task<MyUnitTestClass> GetClassByIdAsync(int id);

    /// <summary>
    /// Executes a synchronous method used to exercise aspect interception.
    /// </summary>
    /// <param name="x">
    /// Numeric argument captured in invocation-string assertions.
    /// </param>
    /// <param name="y">
    /// String argument captured in invocation-string assertions.
    /// </param>
    /// <param name="myUnitTestClass">
    /// Complex argument captured in invocation-string assertions.
    /// </param>
    void Test(int x, string y, MyUnitTestClass myUnitTestClass);

    /// <summary>
    /// Executes an asynchronous method used to exercise aspect interception.
    /// </summary>
    /// <param name="x">
    /// Numeric argument captured in invocation-string assertions.
    /// </param>
    /// <param name="y">
    /// String argument captured in invocation-string assertions.
    /// </param>
    /// <param name="myUnitTestClass">
    /// Complex argument captured in invocation-string assertions.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous test operation.
    /// </returns>
    Task TestAsync(int x, string y, MyUnitTestClass myUnitTestClass);

    /// <summary>
    /// Executes a generic method used to verify generic method mapping through the proxy.
    /// </summary>
    /// <param name="x">Numeric argument captured in invocation-string assertions.</param>
    /// <param name="entity">Generic argument captured in invocation-string assertions.</param>
    /// <param name="enable">Boolean argument captured in invocation-string assertions.</param>
    /// <typeparam name="T">The generic entity type supplied to the method.</typeparam>
    void GenericTest<T>(int x, T entity, bool enable);
}
