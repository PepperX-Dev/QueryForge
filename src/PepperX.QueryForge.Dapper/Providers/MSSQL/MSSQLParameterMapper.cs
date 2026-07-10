using System.Text.Json;
using System.Text.Json.Serialization;

namespace PepperX.QueryForge.Dapper.Providers.MSSQL;

/// <summary>
/// Maps the provider-agnostic DapperQuery object to an anonymous type 
/// that Dapper will automatically map to the MSSQL Stored Procedure parameters.
/// </summary>
internal static class MSSQLParameterMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() } // Serializes Enums as strings (e.g., "And", "Equals")
    };

    /// <summary>
    /// Maps the DapperQuery to an anonymous object. Dapper natively supports anonymous types for parameters.
    /// </summary>
    public static object Map(DapperQuery query)
    {
        if (query.Object == null)
        {
            throw new InvalidOperationException(
                "Query.Object must be specified before executing a Dapper QueryForge query. Use QueryBuilder.ForObject(...) to set it.");
        }

        return new
        {
            Object = JsonSerializer.Serialize(new
            {
                query.Object.Schema,
                query.Object.Name,
                Type = (int)query.Object.Type,
                Parameters = query.Object.Parameters ?? new Dictionary<string, object?>()
            }, JsonOptions),

            Criteria = JsonSerializer.Serialize(new
            {
                Logic = query.Criteria.Logic.ToString(),
                Groups = query.Criteria.Groups.Select(g => new
                {
                    Logic = g.Logic.ToString(),
                    Conditions = g.Conditions.Select(c => new
                    {
                        c.ColumnName,
                        Operator = c.Operator.ToString(),
                        c.Value,
                        c.ValueTo
                    })
                })
            }, JsonOptions),

            Paging = JsonSerializer.Serialize(new
            {
                query.Paging.Size,
                query.Paging.Number
            }, JsonOptions),

            SelectColumns = JsonSerializer.Serialize(query.SelectColumns, JsonOptions),

            SortColumns = JsonSerializer.Serialize(
                query.SortColumns.Select(s => new { s.ColumnName, SortOrder = s.SortOrder.ToString() }),
                JsonOptions),

            GroupByColumns = JsonSerializer.Serialize(
                query.GroupByColumns.Select(g => new { g.ColumnName, SortOrder = g.SortOrder.ToString() }),
                JsonOptions),

            DebugMode = false
        };
    }
}