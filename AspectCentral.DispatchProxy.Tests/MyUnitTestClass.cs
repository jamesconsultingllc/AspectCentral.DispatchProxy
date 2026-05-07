// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MyUnitTestClass.cs" company="James Consulting LLC">
//   
// </copyright>
// // <summary>
//   The my class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Simple value object used by proxy tests as a method parameter and return value.
/// </summary>
public class MyUnitTestClass
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MyUnitTestClass" /> class.
    /// </summary>
    /// <param name="x">
    /// Numeric test value used by equality assertions.
    /// </param>
    /// <param name="y">
    /// String test value used by equality assertions.
    /// </param>
    public MyUnitTestClass(int? x, string y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the numeric test value.
    /// </summary>
    public int? X { get; }

    /// <summary>
    /// Gets the string test value.
    /// </summary>
    public string Y { get; }

    /// <summary>
    /// Compares two <see cref="MyUnitTestClass" /> instances for equality.
    /// </summary>
    /// <param name="left">
    /// The left operand.
    /// </param>
    /// <param name="right">
    /// The right operand.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when both operands are equal; otherwise <see langword="false" />.
    /// </returns>
    public static bool operator ==(MyUnitTestClass left, MyUnitTestClass right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares two <see cref="MyUnitTestClass" /> instances for inequality.
    /// </summary>
    /// <param name="left">
    /// The left operand.
    /// </param>
    /// <param name="right">
    /// The right operand.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the operands are not equal; otherwise <see langword="false" />.
    /// </returns>
    public static bool operator !=(MyUnitTestClass left, MyUnitTestClass right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Determines whether the supplied object represents the same test values.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with this instance.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the supplied object is equivalent; otherwise
    /// <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return ToString().Equals(obj?.ToString());
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return ((X?.GetHashCode() ?? 0) * 397) ^ Y.GetHashCode();
        }
    }

    /// <summary>
    /// Formats the test values into the string representation used by equality comparisons.
    /// </summary>
    /// <returns>
    /// A deterministic string containing the test values.
    /// </returns>
    public override string ToString()
    {
        return $"X - {X} : Y - testing{Y}3";
    }

    /// <summary>
    /// Determines whether another <see cref="MyUnitTestClass" /> has the same values.
    /// </summary>
    /// <param name="other">
    /// The other test value object to compare.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when both value objects contain the same values; otherwise
    /// <see langword="false" />.
    /// </returns>
    protected bool Equals(MyUnitTestClass other)
    {
        return X == other.X && string.Equals(Y, other.Y);
    }
}
