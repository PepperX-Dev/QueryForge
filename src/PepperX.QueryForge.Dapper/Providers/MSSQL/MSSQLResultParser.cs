using System.Text.Json;
using System.Text.Json.Serialization;

namespace PepperX.QueryForge.Dapper.Providers.MSSQL;

/// <summary>
/// Parses the raw SQL JSON output into the strongly-typed QueryResult.
/// </summary>
internal static class MSSQLResultParser
{
    /// <summary>
    /// Internal DTO that Dapper maps the SQL result set columns to.
    /// </summary>
    internal sealed class RawQueryForgeResult
    {
        public string Meta { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    public static QueryResult<TModel> Parse<TModel>(RawQueryForgeResult raw)
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true ,
            Converters = { new JsonStringEnumConverter() }};

        var meta = JsonSerializer.Deserialize<QueryResultMeta>(raw.Meta, jsonOptions)
            ?? new QueryResultMeta(new QueryResultMetaTotal(0, 0), QueryResultType.Flat);

        if (string.IsNullOrWhiteSpace(raw.Data))
            return new QueryResult<TModel> { Meta = meta, Models = Array.Empty<TModel>() };

        using var doc = JsonDocument.Parse(raw.Data);

        if (meta.Type == QueryResultType.Grouped)
        {
            var hierarchy = ParseHierarchy<TModel>(doc.RootElement, jsonOptions);
            return new QueryResult<TModel> { Meta = meta, Groups = hierarchy };
        }
        else
        {
            var models = doc.RootElement.Deserialize<IReadOnlyList<TModel>>(jsonOptions) ?? Array.Empty<TModel>();
            return new QueryResult<TModel> { Meta = meta, Models = models };
        }
    }

    private static IReadOnlyList<HierarchyNode<TModel>> ParseHierarchy<TModel>(JsonElement element, JsonSerializerOptions options)
    {
        var list = new List<HierarchyNode<TModel>>();
        foreach (var item in element.EnumerateArray())
        {
            object? key = null;
            if (item.TryGetProperty("key", out var k))
            {
                key = k.ValueKind switch
                {
                    JsonValueKind.String => k.GetString(),
                    JsonValueKind.Number => k.TryGetInt64(out var l) ? l : k.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => k.GetRawText()
                };
            }

            int count = item.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
            IReadOnlyList<HierarchyNode<TModel>>? subGroups = null;
            IReadOnlyList<TModel>? items = null;

            if (item.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            {
                bool isSubGroup = false;
                using var en = itemsEl.EnumerateArray();
                if (en.MoveNext() && en.Current.TryGetProperty("key", out _) && en.Current.TryGetProperty("count", out _))
                    isSubGroup = true;

                if (isSubGroup) subGroups = ParseHierarchy<TModel>(itemsEl, options);
                else items = itemsEl.Deserialize<IReadOnlyList<TModel>>(options) ?? Array.Empty<TModel>();
            }
            list.Add(new HierarchyNode<TModel>(key, count, subGroups, items));
        }
        return list;
    }
}