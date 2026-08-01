namespace PepperX.QueryForge.Dapper.Compiler;

/// <summary>
/// A parameterized SQL statement produced by <see cref="SqlQueryCompiler"/>.
/// </summary>
/// <param name="Text">The SQL text, containing only placeholders — never inlined literals.</param>
/// <param name="Parameters">The parameter values, keyed by name without the dialect's prefix.</param>
public sealed record CompiledSql(string Text, IReadOnlyDictionary<string, object?> Parameters)
{
    /// <inheritdoc />
    public override string ToString() => Text;
}
