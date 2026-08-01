using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using Dapper;
using PepperX.QueryForge.Dapper.Compiler;

namespace PepperX.QueryForge.Dapper.Internals;

/// <summary>
/// Binds a compiled statement's parameters, insisting that the driver match them by name.
/// </summary>
/// <remarks>
/// QueryForge always emits named references — <c>@p0</c>, <c>:p0</c> — but not every driver binds by
/// name out of the box. ODP.NET binds positionally unless <c>BindByName</c> is set, which means a
/// statement whose parameters happen to be declared in a different order than they appear silently
/// compares the wrong values rather than failing.
/// <para>
/// The flag is set reflectively so that no driver becomes a dependency of this package: a command
/// type exposing a writable boolean <c>BindByName</c> gets it, and anything else is left alone.
/// </para>
/// </remarks>
internal sealed class QueryForgeParameters(CompiledSql compiled) : SqlMapper.IDynamicParameters
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> BindByNameProperties = new();

    public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
    {
        EnableNamedBinding(command);

        var parameters = new DynamicParameters();

        foreach (var (name, value) in compiled.Parameters)
            parameters.Add(name, value);

        ((SqlMapper.IDynamicParameters)parameters).AddParameters(command, identity);
    }

    private static void EnableNamedBinding(IDbCommand command)
    {
        var property = BindByNameProperties.GetOrAdd(
            command.GetType(),
            static type => type.GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance));

        if (property is { CanWrite: true } && property.PropertyType == typeof(bool))
            property.SetValue(command, true);
    }
}
