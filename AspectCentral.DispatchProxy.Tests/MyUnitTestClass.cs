// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MyUnitTestClass.cs" company="James Consulting LLC">
//   
// </copyright>
// // <summary>
//   The my class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AspectCentral.DispatchProxy.Tests
{
    /// <summary>
    ///     The my class.
    /// </summary>
    internal class MyUnitTestClass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MyUnitTestClass"/> class.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        public MyUnitTestClass(int? x, string y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        ///     Gets or sets the x.
        /// </summary>
        public int? X { get; }

        /// <summary>
        ///     Gets or sets the y.
        /// </summary>
        public string Y { get; }

        /// <summary>
        ///     The ==.
        /// </summary>
        /// <param name="left">
        ///     The left.
        /// </param>
        /// <param name="right">
        ///     The right.
        /// </param>
        /// <returns>
        /// </returns>
        public static bool operator ==(MyUnitTestClass left, MyUnitTestClass right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///     The !=.
        /// </summary>
        /// <param name="left">
        ///     The left.
        /// </param>
        /// <param name="right">
        ///     The right.
        /// </param>
        /// <returns>
        /// </returns>
        public static bool operator !=(MyUnitTestClass left, MyUnitTestClass right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// The equals.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return ToString().Equals(obj?.ToString());
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ (Y != null ? Y.GetHashCode() : 0);
            }
        }

        /// <summary>
        ///     The to string.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string ToString()
        {
            return $"X - {X} : Y - testing{Y}3";
        }

        /// <summary>
        /// The equals.
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        protected bool Equals(MyUnitTestClass other)
        {
            return X == other.X && string.Equals(Y, other.Y);
        }
    }
}