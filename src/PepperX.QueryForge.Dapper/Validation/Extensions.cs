namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Provides extension methods for validating Dapper <see cref="DapperQuery"/> instances.
/// </summary>
public static class DapperQueryValidationExtensions
{
    /// <summary>
    /// Validates the Dapper DapperQuery against both Dapper-specific rules and base query rules.
    /// </summary>
    public static DapperQuery Validate(this DapperQuery query, Action<DapperQueryValidationRules> configure, PepperX.QueryForge.QueryValidationMode mode = PepperX.QueryForge.QueryValidationMode.SilentStrip)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(configure);

        var rules = new DapperQueryValidationRules();
        configure(rules);
        var errors = new List<string>();

        // 1. Validate Dapper-specific Object rules
        ValidateObject(query.Object, rules.ObjectRule, errors, mode, obj => query.Object = obj);

        // 2. Validate Base DapperQuery rules (Calls the internal hook from the base library to share the errors list)
        PepperX.QueryForge.QueryValidationExtensions.ApplyBaseRules(query, rules, errors, mode);

        // 3. Throw if any errors were collected from either Base or Dapper rules
        PepperX.QueryForge.QueryValidationExtensions.ThrowIfErrors(errors, mode);

        return query;
    }

    /// <summary>
    /// Internal helper to validate the Dapper-specific DapperQueryObject.
    /// </summary>
    private static void ValidateObject(DapperQueryObject? obj, DapperObjectRule rules, List<string> errors, PepperX.QueryForge.QueryValidationMode mode, Action<DapperQueryObject?> setter)
    {
        // 1. Check Name requirement
        if (rules.RequireName && (obj == null || string.IsNullOrWhiteSpace(obj.Name)))
        {
            if (mode == PepperX.QueryForge.QueryValidationMode.ThrowException)
                errors.Add("Object.Name is required.");
            // SilentStrip cannot fix a missing name, so we just flag it.
        }

        if (obj == null) return; // Nothing else to validate if object is null

        // 2. Check Schema rules
        bool schemaValid = true;
        if (rules.AllowedSchemas.Count > 0 && !rules.AllowedSchemas.Contains(obj.Schema)) schemaValid = false;
        if (rules.DeniedSchemas.Contains(obj.Schema)) schemaValid = false;

        if (!schemaValid)
        {
            if (mode == PepperX.QueryForge.QueryValidationMode.ThrowException)
                errors.Add($"Object.Schema '{obj.Schema}' is not allowed.");
            else
                setter(obj with { Schema = "dbo" }); // Silently fallback to default schema
        }

        // 3. Check Table/Object Name rules
        bool tableValid = true;
        if (rules.AllowedTables.Count > 0 && !rules.AllowedTables.Contains(obj.Name)) tableValid = false;
        if (rules.DeniedTables.Contains(obj.Name)) tableValid = false;

        if (!tableValid)
        {
            if (mode == PepperX.QueryForge.QueryValidationMode.ThrowException)
                errors.Add($"Object.Name '{obj.Name}' is not allowed.");
            // SilentStrip cannot safely "remove" the table name without breaking the query.
        }
    }
}