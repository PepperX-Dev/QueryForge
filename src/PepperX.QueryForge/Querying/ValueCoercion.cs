using System.Globalization;

namespace PepperX.QueryForge.Querying;

/// <summary>
/// Converts a caller-supplied filter value to the type of the column it is compared against.
/// </summary>
/// <remarks>
/// A <see cref="Query"/> arriving as JSON has no type information worth trusting: numbers can appear
/// as strings, dates always do. Loosely-typed stores paper over this, but a strict one does not —
/// PostgreSQL rejects <c>integer &gt; text</c> outright rather than guessing. Coercing before the
/// value is bound keeps the same request working on every engine, and lets the database compare
/// using the column's real type and its indexes.
/// </remarks>
public static class ValueCoercion
{
    /// <summary>
    /// Attempts to convert <paramref name="value"/> to <paramref name="targetType"/>.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when the value cannot represent that type at all — <c>"abc"</c> for an
    /// integer column. Callers treat that as an unusable filter rather than an error.
    /// </returns>
    public static bool TryCoerce(object? value, Type targetType, out object? result)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        value = ConditionSemantics.Unwrap(value);
        result = value;

        if (value is null)
            return true;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsInstanceOfType(value))
            return true;

        try
        {
            if (underlying.IsEnum)
            {
                result = value is string name
                    ? Enum.Parse(underlying, name, ignoreCase: true)
                    : Enum.ToObject(underlying, value);

                return true;
            }

            if (underlying == typeof(Guid))
            {
                result = value as Guid? ?? Guid.Parse(value.ToString()!);
                return true;
            }

            if (underlying == typeof(DateTime))
            {
                result = value is DateTimeOffset dto
                    ? dto.DateTime
                    : DateTime.Parse(value.ToString()!, CultureInfo.InvariantCulture, DateTimeStyles.None);

                return true;
            }

            if (underlying == typeof(DateTimeOffset))
            {
                result = DateTimeOffset.Parse(value.ToString()!, CultureInfo.InvariantCulture);
                return true;
            }

            if (underlying == typeof(DateOnly))
            {
                result = DateOnly.Parse(value.ToString()!, CultureInfo.InvariantCulture);
                return true;
            }

            if (underlying == typeof(TimeOnly))
            {
                result = TimeOnly.Parse(value.ToString()!, CultureInfo.InvariantCulture);
                return true;
            }

            if (underlying == typeof(TimeSpan))
            {
                result = TimeSpan.Parse(value.ToString()!, CultureInfo.InvariantCulture);
                return true;
            }

            if (underlying == typeof(bool))
            {
                // A checkbox can arrive as true, "true", 1 or "1".
                if (value is string text)
                {
                    if (bool.TryParse(text, out var parsed))
                    {
                        result = parsed;
                        return true;
                    }

                    if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var numeric))
                    {
                        result = numeric != 0;
                        return true;
                    }

                    return false;
                }

                result = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                return true;
            }

            if (underlying == typeof(string))
            {
                result = value.ToString();
                return true;
            }

            result = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            result = value;
            return false;
        }
    }
}
