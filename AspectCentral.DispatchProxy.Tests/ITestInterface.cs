// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITestInterface.cs" company="James Consulting LLC">
//   
// </copyright>
// // <summary>
//   The Interface interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace AspectCentral.DispatchProxy.Tests
{
    /// <summary>
    ///     The Interface interface.
    /// </summary>
    internal interface ITestInterface
    {
        /// <summary>
        /// The get class by id.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<MyUnitTestClass> GetClassByIdAsync(int id);

        /// <summary>
        /// The test.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <param name="myUnitTestClass">
        /// The my class.
        /// </param>
        void Test(int x, string y, MyUnitTestClass myUnitTestClass);

        /// <summary>
        /// The test async.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <param name="myUnitTestClass">
        /// The my class.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task TestAsync(int x, string y, MyUnitTestClass myUnitTestClass);
    }
}