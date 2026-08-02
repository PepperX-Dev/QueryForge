using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using PepperX.QueryForge.Querying;

namespace PepperX.QueryForge.EFCore.Translation;

/// <summary>
/// Translates QueryForge's query model into expression trees that EF Core can turn into SQL.
/// </summary>
/// <remarks>
/// <para>
/// The entity's own properties are the whitelist. A condition, sort, or grouping naming something
/// that is not a readable property of the entity is dropped, which gives the same protection against
/// schema probing that the SQL providers get from inspecting the result set — without a round trip.
/// </para>
/// <para>
/// Values are injected through a closure rather than as literal constants, so EF Core emits them as
/// SQL parameters and can reuse a cached query plan across calls that differ only in their values.
/// </para>
/// </remarks>
public static class ExpressionCompiler
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = new();

    private static readonly MethodInfo StringContains =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    private static readonly MethodInfo StringStartsWith =
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;

    private static readonly MethodInfo StringEndsWith =
        typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

    /// <summary>
    /// Builds the predicate for a <see cref="QueryCriteria"/>, or <see langword="null"/> when
    /// nothing usable remains after unknown columns and unfilled values are dropped.
    /// </summary>
    public static Expression<Func<TModel, bool>>? BuildPredicate<TModel>(QueryCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var parameter = Expression.Parameter(typeof(TModel), "e");
        Expression? combined = null;

        foreach (var group in criteria.Groups)
        {
            Expression? groupBody = null;

            var negated = ConditionSemantics.IsNegated(group.Logic);

            // A negated group is built by pushing the negation down to the individual conditions and
            // flipping the connector, rather than wrapping the group in NOT. Both give the same answer
            // when no value is null, but only this form reproduces SQL's three-valued logic: there,
            // NOT of a comparison against NULL stays unknown and the row drops out, whereas EF Core's
            // default null compensation would let it through.
            var joinWithOr = negated
                ? !ConditionSemantics.IsDisjunction(group.Logic)
                : ConditionSemantics.IsDisjunction(group.Logic);

            foreach (var condition in group.Conditions)
            {
                if (!ConditionSemantics.IsExecutable(condition))
                    continue;

                var property = ResolveProperty<TModel>(condition.ColumnName);

                if (property is null)
                    continue;

                var member = Expression.Property(parameter, property);

                var fragment = negated
                    ? BuildNegatedCondition(member, property, condition)
                    : BuildCondition(member, property, condition);

                if (fragment is null)
                    continue;

                groupBody = groupBody is null
                    ? fragment
                    : joinWithOr
                        ? Expression.OrElse(groupBody, fragment)
                        : Expression.AndAlso(groupBody, fragment);
            }

            if (groupBody is null)
                continue;

            combined = combined is null
                ? groupBody
                : ConditionSemantics.IsDisjunction(criteria.Logic)
                    ? Expression.OrElse(combined, groupBody)
                    : Expression.AndAlso(combined, groupBody);
        }

        return combined is null ? null : Expression.Lambda<Func<TModel, bool>>(combined, parameter);
    }

    /// <summary>
    /// Resolves a column that is valid to ORDER BY or GROUP BY — a single value rather than a related
    /// entity or a collection of them.
    /// </summary>
    /// <remarks>
    /// <see cref="ResolveProperty{TModel}"/> answers "is this a property of the entity", which is the
    /// right question for a filter and the wrong one for an ordering. A navigation property is a
    /// property, so it resolves — and a caller supplies the column name as a string, so both kinds of
    /// navigation are reachable from a request body. Each fails differently:
    /// <list type="bullet">
    /// <item>
    /// A <em>collection</em> navigation cannot be translated at all. <c>OrderBy(c =&gt; c.Orders)</c>
    /// throws <see cref="InvalidOperationException"/>, so the request crashes rather than being
    /// ignored.
    /// </item>
    /// <item>
    /// A <em>reference</em> navigation does translate — EF Core orders by the related key — which is
    /// worse in the grouping case than a failure would be. The group key becomes the whole related
    /// entity, so it serializes into <c>HierarchyNode.Key</c> carrying every column of that entity,
    /// including ones the caller was never offered. Ordering by a surrogate key is also not what
    /// anyone naming "Customer" meant; they wanted a column on it.
    /// </item>
    /// </list>
    /// Both are therefore dropped, exactly as an unknown column is.
    /// <para>
    /// Filtering is deliberately left alone: a value that cannot be converted to an entity type
    /// already fails to bind and drops the condition, and <c>Equals</c> against null is a meaningful,
    /// translatable test for whether a related row exists.
    /// </para>
    /// </remarks>
    public static PropertyInfo? ResolveOrderableProperty<TModel>(string? columnName)
    {
        var property = ResolveProperty<TModel>(columnName);

        return property is not null && IsOrderable(property.PropertyType) ? property : null;
    }

    /// <summary>
    /// Whether a value of this type can appear in ORDER BY or GROUP BY.
    /// </summary>
    /// <remarks>
    /// Decided from the CLR type alone, because this runs without access to the EF model. The test is
    /// deliberately generous: everything a database can order by satisfies it, including the
    /// value-converted structs used for strongly-typed ids, while entity references and collection
    /// navigations do not.
    /// </remarks>
    private static bool IsOrderable(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        // Both are sequences, and neither is a navigation.
        if (underlying == typeof(string) || underlying == typeof(byte[]))
            return true;

        if (underlying.IsPrimitive || underlying.IsEnum)
            return true;

        // A collection navigation.
        if (typeof(IEnumerable).IsAssignableFrom(underlying))
            return false;

        // decimal, DateTime, DateTimeOffset, DateOnly, TimeOnly, TimeSpan and Guid all qualify here.
        // An entity type does not, which is the distinction being drawn.
        return typeof(IComparable).IsAssignableFrom(underlying);
    }

    /// <summary>Builds a key selector for a grouping or sorting column.</summary>
    public static LambdaExpression? BuildSelector<TModel>(string columnName)
    {
        var property = ResolveProperty<TModel>(columnName);

        if (property is null)
            return null;

        var parameter = Expression.Parameter(typeof(TModel), "e");

        return Expression.Lambda(Expression.Property(parameter, property), parameter);
    }

    /// <summary>
    /// Builds a projection that copies only the selected columns into a fresh instance, so the
    /// database is asked for nothing more.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> when the model cannot be projected — no parameterless constructor, no
    /// settable properties, or a selection naming nothing real — in which case the caller should
    /// leave the query unprojected rather than fail it.
    /// </returns>
    public static Expression<Func<TModel, TModel>>? BuildProjection<TModel>(
        IReadOnlyList<string> selectColumns,
        IReadOnlyList<string>? alsoKeep = null)
    {
        ArgumentNullException.ThrowIfNull(selectColumns);

        if (selectColumns.Count == 0 || typeof(TModel).GetConstructor(Type.EmptyTypes) is null)
            return null;

        var wanted = new HashSet<string>(selectColumns, StringComparer.OrdinalIgnoreCase);

        if (alsoKeep is not null)
        {
            foreach (var column in alsoKeep)
                wanted.Add(column);
        }

        var parameter = Expression.Parameter(typeof(TModel), "e");
        var bindings = new List<MemberBinding>();

        foreach (var name in wanted)
        {
            var property = ResolveProperty<TModel>(name);

            if (property is null || !property.CanWrite || !property.CanRead)
                continue;

            bindings.Add(Expression.Bind(property, Expression.Property(parameter, property)));
        }

        if (bindings.Count == 0)
            return null;

        return Expression.Lambda<Func<TModel, TModel>>(
            Expression.MemberInit(Expression.New(typeof(TModel)), bindings),
            parameter);
    }

    /// <summary>
    /// Builds a key that sorts nulls to one end, for use as an ordering term before the real one.
    /// </summary>
    /// <param name="property">The column being sorted.</param>
    /// <param name="nullsFirst">Whether nulls should come first.</param>
    /// <returns>
    /// <see langword="null"/> when the column cannot hold a null, in which case no extra ordering
    /// term is needed.
    /// </returns>
    /// <remarks>
    /// Engines disagree on null placement and EF Core defers ORDER BY to the database, so ordering by
    /// an explicit rank first is what keeps the same query returning the same order on every one.
    /// </remarks>
    public static LambdaExpression? BuildNullRank<TModel>(PropertyInfo property, bool nullsFirst)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (!IsNullable(property.PropertyType))
            return null;

        var parameter = Expression.Parameter(typeof(TModel), "e");
        var member = Expression.Property(parameter, property);

        var body = Expression.Condition(
            Expression.Equal(member, Expression.Constant(null, property.PropertyType)),
            Expression.Constant(nullsFirst ? 0 : 1),
            Expression.Constant(nullsFirst ? 1 : 0));

        return Expression.Lambda(body, parameter);
    }

    /// <summary>
    /// The equivalent null-rank key for a bare value, used when ordering distinct grouping keys.
    /// </summary>
    public static Expression<Func<TKey, int>>? BuildKeyNullRank<TKey>(bool nullsFirst)
    {
        if (!IsNullable(typeof(TKey)))
            return null;

        var parameter = Expression.Parameter(typeof(TKey), "k");

        var body = Expression.Condition(
            Expression.Equal(parameter, Expression.Constant(null, typeof(TKey))),
            Expression.Constant(nullsFirst ? 0 : 1),
            Expression.Constant(nullsFirst ? 1 : 0));

        return Expression.Lambda<Func<TKey, int>>(body, parameter);
    }

    /// <summary>Looks up a readable instance property by name, case-insensitively.</summary>
    public static PropertyInfo? ResolveProperty<TModel>(string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return null;

        var map = PropertyCache.GetOrAdd(typeof(TModel), static type =>
        {
            var properties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0)
                    properties[property.Name] = property;
            }

            return properties;
        });

        return map.GetValueOrDefault(columnName);
    }

    private static Expression? BuildCondition(MemberExpression member, PropertyInfo property, Condition condition)
    {
        var targetType = property.PropertyType;
        var raw = ConditionSemantics.Unwrap(condition.Value);

        switch (condition.Operator)
        {
            case ConditionOperator.Equals when raw is null:
                return IsNullable(targetType)
                    ? Expression.Equal(member, Expression.Constant(null, targetType))
                    : Expression.Constant(false);

            case ConditionOperator.NotEquals when raw is null:
                return IsNullable(targetType)
                    ? Expression.NotEqual(member, Expression.Constant(null, targetType))
                    : Expression.Constant(true);

            case ConditionOperator.Equals:
                return TryBind(member, targetType, raw, Expression.Equal);

            case ConditionOperator.NotEquals:
                return TryBind(member, targetType, raw, Expression.NotEqual);

            case ConditionOperator.LessThan:
                return TryBind(member, targetType, raw, Expression.LessThan);

            case ConditionOperator.GreaterThan:
                return TryBind(member, targetType, raw, Expression.GreaterThan);

            case ConditionOperator.LessThanOrEqualTo:
                return TryBind(member, targetType, raw, Expression.LessThanOrEqual);

            case ConditionOperator.GreaterThanOrEqualTo:
                return TryBind(member, targetType, raw, Expression.GreaterThanOrEqual);

            case ConditionOperator.Between:
                var upper = ConditionSemantics.Unwrap(condition.ValueTo);
                var lowerBound = TryBind(member, targetType, raw, Expression.GreaterThanOrEqual);
                var upperBound = TryBind(member, targetType, upper, Expression.LessThanOrEqual);

                return lowerBound is null || upperBound is null
                    ? null
                    : Expression.AndAlso(lowerBound, upperBound);

            case ConditionOperator.Contains:
                return BuildLike(member, targetType, raw, StringContains, negate: false);

            case ConditionOperator.NotContains:
                return BuildLike(member, targetType, raw, StringContains, negate: true);

            case ConditionOperator.StartsWith:
                return BuildLike(member, targetType, raw, StringStartsWith, negate: false);

            case ConditionOperator.EndsWith:
                return BuildLike(member, targetType, raw, StringEndsWith, negate: false);

            default:
                return null;
        }
    }

    /// <summary>
    /// Builds the SQL-faithful negation of a condition.
    /// </summary>
    /// <remarks>
    /// Each negation carries an explicit "column is not null" guard, because in SQL a comparison
    /// against NULL is unknown and stays unknown under NOT — the row is excluded either way. The
    /// guard also neutralizes EF Core's null compensation, which would otherwise rewrite
    /// <c>col != value</c> into <c>col &lt;&gt; value OR col IS NULL</c> and let those rows back in.
    /// </remarks>
    private static Expression? BuildNegatedCondition(
        MemberExpression member,
        PropertyInfo property,
        Condition condition)
    {
        var targetType = property.PropertyType;
        var raw = ConditionSemantics.Unwrap(condition.Value);

        // IS NULL and IS NOT NULL are definite even when the value is null, so they simply invert.
        if (raw is null && condition.Operator is ConditionOperator.Equals or ConditionOperator.NotEquals)
        {
            if (!IsNullable(targetType))
                return Expression.Constant(condition.Operator is ConditionOperator.Equals);

            return condition.Operator is ConditionOperator.Equals
                ? Expression.NotEqual(member, Expression.Constant(null, targetType))
                : Expression.Equal(member, Expression.Constant(null, targetType));
        }

        var inverted = condition.Operator switch
        {
            ConditionOperator.Equals => ConditionOperator.NotEquals,
            ConditionOperator.NotEquals => ConditionOperator.Equals,
            ConditionOperator.LessThan => ConditionOperator.GreaterThanOrEqualTo,
            ConditionOperator.GreaterThan => ConditionOperator.LessThanOrEqualTo,
            ConditionOperator.LessThanOrEqualTo => ConditionOperator.GreaterThan,
            ConditionOperator.GreaterThanOrEqualTo => ConditionOperator.LessThan,
            ConditionOperator.Contains => ConditionOperator.NotContains,
            ConditionOperator.NotContains => ConditionOperator.Contains,
            _ => condition.Operator
        };

        Expression? body;

        if (inverted != condition.Operator)
        {
            body = BuildCondition(member, property, condition with { Operator = inverted });
        }
        else
        {
            // Between, StartsWith and EndsWith have no inverse operator in the model, so the
            // predicate itself is negated and then guarded.
            var positive = BuildCondition(member, property, condition);

            body = positive is null ? null : Expression.Not(positive);
        }

        if (body is null)
            return null;

        return IsNullable(targetType)
            ? Expression.AndAlso(Expression.NotEqual(member, Expression.Constant(null, targetType)), body)
            : body;
    }

    private static Expression? TryBind(
        MemberExpression member,
        Type targetType,
        object? value,
        Func<Expression, Expression, BinaryExpression> build)
    {
        if (!TryConvert(value, targetType, out var converted))
            return null;

        try
        {
            return build(member, Parameterize(converted, targetType));
        }
        catch (InvalidOperationException)
        {
            // The operator does not apply to this type — treat it as an unusable filter.
            return null;
        }
    }

    private static Expression? BuildLike(
        MemberExpression member,
        Type targetType,
        object? value,
        MethodInfo method,
        bool negate)
    {
        // Text matching only applies to text. Anything else would force a client-side ToString(),
        // which EF Core cannot translate.
        if (targetType != typeof(string))
            return null;

        var pattern = value?.ToString() ?? string.Empty;
        Expression call = Expression.Call(member, method, Parameterize(pattern, typeof(string)));

        if (negate)
            call = Expression.Not(call);

        // A null column never matches a text predicate, negated or not — the same result SQL gives,
        // where comparing NULL yields NULL rather than true. The guard must sit outside the
        // negation, otherwise NOT would turn "no value" into a match.
        return Expression.AndAlso(
            Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
            call);
    }

    /// <summary>
    /// Wraps a value so EF Core sees a captured variable rather than a constant, which is what makes
    /// it emit a SQL parameter instead of an inlined literal.
    /// </summary>
    private static Expression Parameterize(object? value, Type type)
    {
        var boxType = typeof(ValueBox<>).MakeGenericType(type);
        var box = Activator.CreateInstance(boxType, value)!;

        return Expression.Property(Expression.Constant(box, boxType), nameof(ValueBox<object>.Value));
    }

    private sealed class ValueBox<T>(T value)
    {
        public T Value { get; } = value;
    }

    private static bool IsNullable(Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static bool TryConvert(object? value, Type targetType, out object? converted)
    {
        converted = null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is null)
            return IsNullable(targetType);

        if (underlying.IsInstanceOfType(value))
        {
            converted = value;
            return true;
        }

        try
        {
            if (underlying.IsEnum)
            {
                converted = value is string name
                    ? Enum.Parse(underlying, name, ignoreCase: true)
                    : Enum.ToObject(underlying, value);

                return true;
            }

            if (underlying == typeof(Guid))
            {
                converted = value as Guid? ?? Guid.Parse(value.ToString()!);
                return true;
            }

            if (underlying == typeof(DateTime))
            {
                converted = DateTime.Parse(value.ToString()!, CultureInfo.InvariantCulture);
                return true;
            }

            if (underlying == typeof(DateTimeOffset))
            {
                converted = DateTimeOffset.Parse(value.ToString()!, CultureInfo.InvariantCulture);
                return true;
            }

            if (underlying == typeof(TimeSpan))
            {
                converted = TimeSpan.Parse(value.ToString()!, CultureInfo.InvariantCulture);
                return true;
            }

            converted = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            // A value that cannot be coerced to the column's type is not a match-nothing filter —
            // it is an unusable one, and is dropped like any other.
            return false;
        }
    }
}
