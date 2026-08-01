using System.Data;
using PepperX.QueryForge.Conformance;
using PepperX.QueryForge.Dapper.Compiler;

namespace PepperX.QueryForge.Dapper.Tests.Integration;

/// <summary>
/// Creates and seeds the conformance tables on a real database server.
/// </summary>
/// <remarks>
/// The DDL differs per engine, so each dialect supplies its own column types; the seeding itself is
/// shared, which keeps the data provably identical across engines. Everything is parameterized —
/// these tests insert the same awkward values the query tests later search for, including names
/// containing quotes and LIKE wildcards.
/// </remarks>
internal static class RelationalTestDatabase
{
    public static void CreateWidgets(IDbConnection connection, ISqlDialect dialect, string table)
    {
        var q = dialect.QuoteIdentifier;

        DropTable(connection, dialect, q(table));

        Execute(connection,
            $"""
             CREATE TABLE {q(table)} (
                 {q("Id")}         {Type(dialect, "int")} NOT NULL,
                 {q("Name")}       {Type(dialect, "text")} NOT NULL,
                 {q("Category")}   {Type(dialect, "text")} NULL,
                 {q("Region")}     {Type(dialect, "text")} NULL,
                 {q("Quantity")}   {Type(dialect, "int")} NOT NULL,
                 {q("Price")}      {Type(dialect, "double")} NOT NULL,
                 {q("IsActive")}   {Type(dialect, "bool")} NOT NULL,
                 {q("ReleasedOn")} {Type(dialect, "datetime")} NOT NULL,
                 {q("Secret")}     {Type(dialect, "text")} NOT NULL
             )
             """);

        foreach (var w in WidgetData.All)
        {
            Execute(connection,
                $"""
                 INSERT INTO {q(table)}
                 ({q("Id")}, {q("Name")}, {q("Category")}, {q("Region")}, {q("Quantity")},
                  {q("Price")}, {q("IsActive")}, {q("ReleasedOn")}, {q("Secret")})
                 VALUES ({P(dialect, "Id")}, {P(dialect, "Name")}, {P(dialect, "Category")},
                         {P(dialect, "Region")}, {P(dialect, "Quantity")}, {P(dialect, "Price")},
                         {P(dialect, "IsActive")}, {P(dialect, "ReleasedOn")}, {P(dialect, "Secret")})
                 """,
                ("Id", w.Id),
                ("Name", w.Name),
                ("Category", w.Category),
                ("Region", w.Region),
                ("Quantity", w.Quantity),
                ("Price", w.Price),
                ("IsActive", w.IsActive),
                ("ReleasedOn", w.ReleasedOn),
                ("Secret", w.Secret));
        }
    }

    public static void CreateSalesOrders(IDbConnection connection, ISqlDialect dialect, string table)
    {
        var q = dialect.QuoteIdentifier;

        DropTable(connection, dialect, q(table));

        Execute(connection,
            $"""
             CREATE TABLE {q(table)} (
                 {q("OrderId")}       {Type(dialect, "int")} NOT NULL,
                 {q("Number")}        {Type(dialect, "text")} NOT NULL,
                 {q("Customer")}      {Type(dialect, "text")} NOT NULL,
                 {q("Tier")}          {Type(dialect, "text")} NULL,
                 {q("Country")}       {Type(dialect, "text")} NOT NULL,
                 {q("Region")}        {Type(dialect, "text")} NULL,
                 {q("Channel")}       {Type(dialect, "text")} NOT NULL,
                 {q("Status")}        {Type(dialect, "text")} NOT NULL,
                 {q("Quantity")}      {Type(dialect, "int")} NOT NULL,
                 {q("Amount")}        {Type(dialect, "double")} NOT NULL,
                 {q("Discount")}      {Type(dialect, "double")} NOT NULL,
                 {q("Priority")}      {Type(dialect, "bool")} NOT NULL,
                 {q("PlacedOn")}      {Type(dialect, "datetime")} NOT NULL,
                 {q("ShippedOn")}     {Type(dialect, "datetime")} NULL,
                 {q("Rep")}           {Type(dialect, "text")} NOT NULL,
                 {q("InternalNotes")} {Type(dialect, "text")} NOT NULL
             )
             """);

        foreach (var o in SalesData.All)
        {
            Execute(connection,
                $"""
                 INSERT INTO {q(table)}
                 ({q("OrderId")}, {q("Number")}, {q("Customer")}, {q("Tier")}, {q("Country")},
                  {q("Region")}, {q("Channel")}, {q("Status")}, {q("Quantity")}, {q("Amount")},
                  {q("Discount")}, {q("Priority")}, {q("PlacedOn")}, {q("ShippedOn")}, {q("Rep")},
                  {q("InternalNotes")})
                 VALUES ({P(dialect, "OrderId")}, {P(dialect, "Number")}, {P(dialect, "Customer")},
                         {P(dialect, "Tier")}, {P(dialect, "Country")}, {P(dialect, "Region")},
                         {P(dialect, "Channel")}, {P(dialect, "Status")}, {P(dialect, "Quantity")},
                         {P(dialect, "Amount")}, {P(dialect, "Discount")}, {P(dialect, "Priority")},
                         {P(dialect, "PlacedOn")}, {P(dialect, "ShippedOn")}, {P(dialect, "Rep")},
                         {P(dialect, "InternalNotes")})
                 """,
                ("OrderId", o.OrderId),
                ("Number", o.Number),
                ("Customer", o.Customer),
                ("Tier", o.Tier),
                ("Country", o.Country),
                ("Region", o.Region),
                ("Channel", o.Channel),
                ("Status", o.Status),
                ("Quantity", o.Quantity),
                ("Amount", o.Amount),
                ("Discount", o.Discount),
                ("Priority", o.Priority),
                ("PlacedOn", o.PlacedOn),
                ("ShippedOn", o.ShippedOn),
                ("Rep", o.Rep),
                ("InternalNotes", o.InternalNotes));
        }
    }

    /// <summary>
    /// Drops a table if it is there.
    /// </summary>
    /// <remarks>
    /// Oracle has no <c>DROP TABLE IF EXISTS</c>, so the statement is issued and a "table does not
    /// exist" error is swallowed. Every other engine supports the conditional form directly.
    /// </remarks>
    private static void DropTable(IDbConnection connection, ISqlDialect dialect, string quotedTable)
    {
        if (dialect.ProviderType != DapperDatabaseProvider.Oracle)
        {
            Execute(connection, $"DROP TABLE IF EXISTS {quotedTable}");
            return;
        }

        try
        {
            Execute(connection, $"DROP TABLE {quotedTable} CASCADE CONSTRAINTS");
        }
        catch (Exception)
        {
            // ORA-00942: the table was not there to begin with, which is the desired end state.
        }
    }

    private static string P(ISqlDialect dialect, string name) => dialect.ParameterReference(name);

    private static string Type(ISqlDialect dialect, string logical) => dialect.ProviderType switch
    {
        DapperDatabaseProvider.PostgreSQL => logical switch
        {
            "int" => "integer",
            "text" => "text",
            "double" => "double precision",
            "bool" => "boolean",
            _ => "timestamp"
        },
        DapperDatabaseProvider.MySQL => logical switch
        {
            "int" => "int",
            "text" => "varchar(200)",
            "double" => "double",
            "bool" => "tinyint(1)",
            _ => "datetime"
        },
        DapperDatabaseProvider.MSSQL => logical switch
        {
            "int" => "int",
            "text" => "nvarchar(200)",
            "double" => "float",
            "bool" => "bit",
            _ => "datetime2"
        },
        // Oracle had no BOOLEAN in SQL before 23c, so a flag is a single-digit NUMBER.
        DapperDatabaseProvider.Oracle => logical switch
        {
            "int" => "NUMBER(10)",
            "text" => "VARCHAR2(200)",
            "double" => "BINARY_DOUBLE",
            "bool" => "NUMBER(1)",
            _ => "TIMESTAMP"
        },
        _ => logical switch
        {
            "int" => "INTEGER",
            "text" => "TEXT",
            "double" => "REAL",
            "bool" => "INTEGER",
            _ => "TEXT"
        }
    };

    private static void Execute(IDbConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        command.ExecuteNonQuery();
    }
}
