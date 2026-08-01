using System.Collections.Concurrent;
using System.Data;
using Dapper;
using PepperX.QueryForge.Dapper.Compiler;

namespace PepperX.QueryForge.Dapper.Internals;

/// <summary>
/// Discovers and caches the real column names of each query object.
/// </summary>
/// <remarks>
/// The column list is read from the object's own result set using a statement that returns no rows,
/// rather than from engine-specific catalog views. That keeps discovery identical on every database
/// and, unlike a catalog lookup, it works for table-valued functions and views whose columns are
/// computed.
/// <para>
/// Results are cached for the lifetime of the process because an object's shape changing under a
/// running application is a deployment event, not a request-time concern.
/// </para>
/// </remarks>
internal sealed class SchemaCache(SqlQueryCompiler compiler)
{
    private readonly ConcurrentDictionary<string, ColumnWhitelist> _cache = new();

    public async Task<ColumnWhitelist> GetAsync(
        IDbConnection connection,
        DapperQuery query,
        int? commandTimeout,
        IDbTransaction? transaction)
    {
        var target = query.Object ?? throw new InvalidOperationException(
            "Query.Object must be specified before executing a query. Use ForObject(...) on the builder to set it.");

        var schema = string.IsNullOrWhiteSpace(target.Schema) ? compiler.Dialect.DefaultSchema : target.Schema;
        var key = $"{compiler.Dialect.ProviderType}|{schema}|{target.Name}|{target.Type}";

        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var probe = compiler.CompileSchemaProbe(query);

        using var reader = await connection.ExecuteReaderAsync(
            probe.Text,
            ToDapperParameters(probe),
            transaction,
            commandTimeout);

        var columns = new List<KeyValuePair<string, Type?>>(reader.FieldCount);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            // The reported type is what lets the compiler coerce a caller's loosely-typed value
            // before binding it, which strict engines require.
            Type? fieldType = null;

            try
            {
                fieldType = reader.GetFieldType(i);
            }
            catch (Exception e) when (e is NotSupportedException or InvalidCastException or IndexOutOfRangeException)
            {
                // A driver that cannot describe a column still yields a usable name.
            }

            columns.Add(new KeyValuePair<string, Type?>(reader.GetName(i), fieldType));
        }

        var whitelist = new ColumnWhitelist(columns);
        _cache[key] = whitelist;

        return whitelist;
    }

    /// <summary>
    /// Converts compiled parameters into the form Dapper expects, binding them by name.
    /// </summary>
    internal static SqlMapper.IDynamicParameters ToDapperParameters(CompiledSql compiled)
        => new QueryForgeParameters(compiled);
}
