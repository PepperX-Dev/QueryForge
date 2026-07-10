using System.Data;

namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Configuration options specific to the Dapper execution engine.
/// </summary>
public class DapperQueryForgeOptions
{
    /// <summary>
    /// The execution strategy. Defaults to <see cref="DapperExecutionApproach.DevelopAndUseSp"/>.
    /// </summary>
    public DapperExecutionApproach Approach { get; set; } = DapperExecutionApproach.DevelopAndUseSp;

    /// <summary>
    /// A factory function to create the ADO.NET database connection used for background initialization 
    /// and auto-managed query execution. 
    /// Required if <see cref="Approach"/> is set to <see cref="DapperExecutionApproach.DevelopAndUseSp"/> 
    /// or if using the parameterless <see cref="IDapperQueryService.QueryAsync{TModel}(DapperQuery, int?, CancellationToken)"/>.
    /// </summary>
    public Func<IServiceProvider, IDbConnection>? ConnectionFactory { get; set; }
}