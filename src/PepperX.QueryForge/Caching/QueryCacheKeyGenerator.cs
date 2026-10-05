using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PepperX.QueryForge.Querying;

namespace PepperX.QueryForge.Caching;

/// <summary>
/// Generates deterministic, collision-resistant cache keys for <see cref="Query"/> executions.
/// </summary>
public static class QueryCacheKeyGenerator
{
    private const string DefaultPrefix = "qf";

    /// <summary>
    /// Computes a unique, deterministic cache key for the given query and model type.
    /// </summary>
    public static string GenerateKey<TModel>(
        Query query,
        string? targetSource = null,
        string? customPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!string.IsNullOrWhiteSpace(query.Cache?.Key))
            return query.Cache.Key;

        var prefix = customPrefix ?? query.Cache?.KeyPrefix ?? DefaultPrefix;
        var modelName = typeof(TModel).FullName ?? typeof(TModel).Name;

        var builder = new StringBuilder();
        builder.Append("m:").Append(modelName).Append('|');

        if (!string.IsNullOrWhiteSpace(targetSource))
            builder.Append("src:").Append(targetSource).Append('|');

        // Criteria
        AppendCriteria(builder, query.Criteria);

        // Paging
        builder.Append("|p:").Append(query.Paging.Number).Append(':').Append(query.Paging.Size);

        // SelectColumns
        if (query.SelectColumns.Count > 0)
        {
            builder.Append("|sel:");
            for (var i = 0; i < query.SelectColumns.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(query.SelectColumns[i]);
            }
        }

        // SortColumns
        if (query.SortColumns.Count > 0)
        {
            builder.Append("|sort:");
            for (var i = 0; i < query.SortColumns.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(query.SortColumns[i].ColumnName)
                    .Append(':')
                    .Append((int)query.SortColumns[i].SortOrder);
            }
        }

        // GroupByColumns
        if (query.GroupByColumns.Count > 0)
        {
            builder.Append("|grp:");
            for (var i = 0; i < query.GroupByColumns.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(query.GroupByColumns[i].ColumnName)
                    .Append(':')
                    .Append((int)query.GroupByColumns[i].SortOrder);
            }
        }

        var rawPayload = builder.ToString();
        var hash = ComputeHash(rawPayload);

        return $"{prefix}:{modelName}:{hash}";
    }

    private static void AppendCriteria(StringBuilder builder, QueryCriteria criteria)
    {
        builder.Append("c:logic=").Append((int)criteria.Logic);

        for (var g = 0; g < criteria.Groups.Count; g++)
        {
            var group = criteria.Groups[g];
            builder.Append(";g").Append(g).Append(":logic=").Append((int)group.Logic);

            for (var c = 0; c < group.Conditions.Count; c++)
            {
                var cond = group.Conditions[c];
                builder.Append('[')
                    .Append(cond.ColumnName)
                    .Append(':')
                    .Append((int)cond.Operator)
                    .Append('=')
                    .Append(ValueToString(ConditionSemantics.Unwrap(cond.Value)));

                if (cond.ValueTo is not null)
                {
                    builder.Append("..")
                        .Append(ValueToString(ConditionSemantics.Unwrap(cond.ValueTo)));
                }

                builder.Append(']');
            }
        }
    }

    private static string ValueToString(object? value)
    {
        if (value is null) return "null";
        if (value is string s) return s;
        if (value is bool b) return b ? "true" : "false";
        if (value is IFormattable f) return f.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        if (value is System.Collections.IEnumerable enumerable)
        {
            var sb = new StringBuilder("[");
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first) sb.Append(',');
                sb.Append(ValueToString(item));
                first = false;
            }
            sb.Append(']');
            return sb.ToString();
        }

        return value.ToString() ?? "";
    }

    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes)[..24].ToLowerInvariant();
    }
}
