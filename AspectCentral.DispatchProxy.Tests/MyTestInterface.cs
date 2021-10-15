// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MyTestInterface.cs" company="James Consulting LLC">
//   
// </copyright>
// // <summary>
//   The my interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace AspectCentral.DispatchProxy.Tests
{
    /// <summary>
    ///     The my interface.
    /// </summary>
    public class MyTestInterface : ITestInterface
    {
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
        public Task TestAsync(int x, string y, MyUnitTestClass myUnitTestClass)
        {
            return Task.Delay(100);
        }

        public void GenericTest<T>(int x, T entity, bool enable)
        {
        }
    }
}