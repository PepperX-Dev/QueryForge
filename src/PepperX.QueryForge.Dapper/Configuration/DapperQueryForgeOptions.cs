using System.Data;

namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Configuration options for the Dapper execution provider.
/// </summary>
public class DapperQueryForgeOptions
{
    /// <summary>
    /// A factory that creates the ADO.NET connection used by the overloads which manage their own
    /// connection. Not required when every call supplies its own <see cref="IDbConnection"/>.
    /// </summary>
    public Func<IServiceProvider, IDbConnection>? ConnectionFactory { get; set; }
}
