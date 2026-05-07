// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MyTestInterface.cs" company="James Consulting LLC">
//   
// </copyright>
// // <summary>
//   The my interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Concrete implementation of <see cref="ITestInterface" /> used by proxy tests.
/// </summary>
public class MyTestInterface : ITestInterface
{
    /// <summary>
    /// Cached <see cref="Type" /> token for this test implementation.
    /// </summary>
    public static readonly Type Type = typeof(MyTestInterface);

    /// <inheritdoc />
    public async Task<MyUnitTestClass> GetClassByIdAsync(int id)
    {
        await Task.Delay(100);
        return new MyUnitTestClass(id, id.ToString());
    }

    /// <inheritdoc />
    public void Test(int x, string y, MyUnitTestClass myUnitTestClass)
    {
        Console.WriteLine("testing");
    }

    /// <inheritdoc />
    public async Task TestAsync(int x, string y, MyUnitTestClass myUnitTestClass)
    {
        await Task.Delay(100);
    }

    /// <inheritdoc />
    public void GenericTest<T>(int x, T entity, bool enable)
    {
    }
}
