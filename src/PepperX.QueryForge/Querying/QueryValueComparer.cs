namespace PepperX.QueryForge.Querying;

/// <summary>
/// Orders and compares loosely-typed query values the way a database would.
/// </summary>
/// <remarks>
/// Values reaching a provider are not guaranteed to share a CLR type: a column may materialize as
/// <see cref="int"/> while the matching <see cref="Condition.Value"/> arrived from JSON as
/// <see cref="long"/> or <see cref="string"/>. This comparer reconciles them numerically where it
/// can, and falls back to ordinal string ordering so results stay stable rather than throwing.
/// <para>Nulls sort first ascending and last descending, matching SQL ordering.</para>
/// </remarks>
public sealed class QueryValueComparer : IComparer<object?>
{
    /// <summary>The shared instance.</summary>
    public static readonly QueryValueComparer Instance = new();

    private QueryValueComparer() { }

    /// <inheritdoc />
    public int Compare(object? x, object? y)
    {
        x = ConditionSemantics.Unwrap(x);
        y = ConditionSemantics.Unwrap(y);

        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        if (x.GetType() == y.GetType() && x is IComparable sameType)
            return sameType.CompareTo(y);

        if (TryCompareNumeric(x, y, out var numeric))
            return numeric;

        if (TryCompareAs<DateTimeOffset>(x, y, out var dto)) return dto;
        if (TryCompareAs<DateTime>(x, y, out var dt)) return dt;
        if (TryCompareAs<Guid>(x, y, out var guid)) return guid;

        return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether two loosely-typed values represent the same value.
    /// </summary>
    public bool AreEqual(object? x, object? y)
    {
        x = ConditionSemantics.Unwrap(x);
        y = ConditionSemantics.Unwrap(y);

        if (x is null || y is null)
            return x is null && y is null;

        // Strings compare case-insensitively to match the default SQL Server collation, which is
        // the behaviour QueryForge has always had.
        if (x is string sx && y is string sy)
            return string.Equals(sx, sy, StringComparison.OrdinalIgnoreCase);

        return Compare(x, y) == 0;
    }

    private static bool TryCompareNumeric(object x, object y, out int result)
    {
        result = 0;

        if (!TryToDecimal(x, out var dx) || !TryToDecimal(y, out var dy))
            return false;

        result = decimal.Compare(dx, dy);
        return true;
    }

    private static bool TryToDecimal(object value, out decimal result)
    {
        switch (value)
        {
            case bool or char or DateTime or DateTimeOffset or Guid or TimeSpan:
                result = 0;
                return false;

            case string text:
                return decimal.TryParse(
                    text,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out result);

            case IConvertible:
                try
                {
                    result = Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                {
                    result = 0;
                    return false;
                }

            default:
                result = 0;
                return false;
        }
    }

    private static bool TryCompareAs<T>(object x, object y, out int result) where T : struct, IComparable<T>
    {
        result = 0;

        if (!TryConvert<T>(x, out var tx) || !TryConvert<T>(y, out var ty))
            return false;

        result = tx.CompareTo(ty);
        return true;
    }

    private static bool TryConvert<T>(object value, out T result) where T : struct
    {
        if (value is T typed)
        {
            result = typed;
            return true;
        }

        if (value is string text)
        {
            if (typeof(T) == typeof(DateTime) && DateTime.TryParse(text, out var dt))
            {
                result = (T)(object)dt;
                return true;
            }

            if (typeof(T) == typeof(DateTimeOffset) && DateTimeOffset.TryParse(text, out var dto))
            {
                result = (T)(object)dto;
                return true;
            }

            if (typeof(T) == typeof(Guid) && Guid.TryParse(text, out var guid))
            {
                result = (T)(object)guid;
                return true;
            }
        }

        result = default;
        return false;
    }
}
