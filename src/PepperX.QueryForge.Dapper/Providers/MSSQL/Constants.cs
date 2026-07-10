namespace PepperX.QueryForge.Dapper.Providers.MSSQL
{
    internal static class Constants
    {
        internal static class Scripts
        {
            internal static class Initialization
            {
                public const string usp_QueryForge_BuildWhere = @"CREATE OR ALTER PROCEDURE [dbo].[usp_QueryForge_BuildWhere]
    @Conditions NVARCHAR(MAX),
    @ConditionLogic NVARCHAR(10),
    @WhereFrag NVARCHAR(MAX) OUTPUT,
    @WhereClause NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Q NCHAR(1) = NCHAR(39);

    INSERT INTO #CondFragments
    (
        GrpOrd,
        GrpLogic,
        CondOrd,
        Fragment
    )
    SELECT CAST(g.[key] AS INT),
           UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(g.value, N'$.Logic'), @ConditionLogic)))),
           CAST(c.[key] AS INT),
           CASE JSON_VALUE(c.value, N'$.Operator')
               WHEN N'Equals' THEN
                   CASE
                       WHEN JSON_VALUE(c.value, N'$.Value') IS NULL THEN
                           QUOTENAME(vc.ColName) + N' IS NULL'
                       ELSE
                           QUOTENAME(vc.ColName) + N' = N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q)
                           + @Q
                   END
               WHEN N'NotEquals' THEN
                   CASE
                       WHEN JSON_VALUE(c.value, N'$.Value') IS NULL THEN
                           QUOTENAME(vc.ColName) + N' IS NOT NULL'
                       ELSE
                           QUOTENAME(vc.ColName) + N' <> N' + @Q
                           + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
                   END
               WHEN N'Contains' THEN
                   QUOTENAME(vc.ColName) + N' LIKE N' + @Q + N'%'
                   + REPLACE(
                                REPLACE(
                                           REPLACE(REPLACE(JSON_VALUE(c.value, N'$.Value'), N'\', N'\\'), N'%', N'\%'),
                                           N'_',
                                           N'\_'
                                       ),
                                @Q,
                                @Q + @Q
                            ) + N'%' + @Q + N' ESCAPE N' + @Q + N'\' + @Q
               WHEN N'NotContains' THEN
                   QUOTENAME(vc.ColName) + N' NOT LIKE N' + @Q + N'%'
                   + REPLACE(
                                REPLACE(
                                           REPLACE(REPLACE(JSON_VALUE(c.value, N'$.Value'), N'\', N'\\'), N'%', N'\%'),
                                           N'_',
                                           N'\_'
                                       ),
                                @Q,
                                @Q + @Q
                            ) + N'%' + @Q + N' ESCAPE N' + @Q + N'\' + @Q
               WHEN N'StartsWith' THEN
                   QUOTENAME(vc.ColName) + N' LIKE N' + @Q
                   + REPLACE(
                                REPLACE(
                                           REPLACE(REPLACE(JSON_VALUE(c.value, N'$.Value'), N'\', N'\\'), N'%', N'\%'),
                                           N'_',
                                           N'\_'
                                       ),
                                @Q,
                                @Q + @Q
                            ) + N'%' + @Q + N' ESCAPE N' + @Q + N'\' + @Q
               WHEN N'EndsWith' THEN
                   QUOTENAME(vc.ColName) + N' LIKE N' + @Q + N'%'
                   + REPLACE(
                                REPLACE(
                                           REPLACE(REPLACE(JSON_VALUE(c.value, N'$.Value'), N'\', N'\\'), N'%', N'\%'),
                                           N'_',
                                           N'\_'
                                       ),
                                @Q,
                                @Q + @Q
                            ) + @Q + N' ESCAPE N' + @Q + N'\' + @Q
               WHEN N'LessThan' THEN
                   QUOTENAME(vc.ColName) + N' < N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
               WHEN N'GreaterThan' THEN
                   QUOTENAME(vc.ColName) + N' > N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
               WHEN N'LessThanOrEqualTo' THEN
                   QUOTENAME(vc.ColName) + N' <= N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
               WHEN N'GreaterThanOrEqualTo' THEN
                   QUOTENAME(vc.ColName) + N' >= N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
               WHEN N'Between' THEN
                   QUOTENAME(vc.ColName) + N' BETWEEN N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q)
                   + @Q + N' AND N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.ValueTo'), @Q, @Q + @Q) + @Q
               ELSE
                   NULL
           END
    FROM OPENJSON(@Conditions) g
        CROSS APPLY OPENJSON(JSON_QUERY(g.value, N'$.Conditions')) c
        JOIN #ValidColumns vc
            ON vc.ColName = JSON_VALUE(c.value, N'$.ColumnName')
    WHERE JSON_VALUE(c.value, N'$.Operator') IN ( N'Equals', N'NotEquals', N'Contains', N'NotContains', N'StartsWith',
                                                  N'EndsWith', N'LessThan', N'GreaterThan', N'LessThanOrEqualTo',
                                                  N'GreaterThanOrEqualTo', N'Between'
                                                )
          AND
          (
              JSON_VALUE(c.value, N'$.Value') IS NOT NULL
              OR JSON_VALUE(c.value, N'$.Operator') IN ( N'Equals', N'NotEquals' )
          )
          AND
          (
              JSON_VALUE(c.value, N'$.Operator') <> N'Between'
              OR JSON_VALUE(c.value, N'$.ValueTo') IS NOT NULL
          );

    DELETE FROM #CondFragments
    WHERE Fragment IS NULL;

    SET @WhereFrag = N'';

    IF EXISTS (SELECT 1 FROM #CondFragments)
    BEGIN
        DECLARE @gOrd INT =
                (
                    SELECT MIN(GrpOrd)FROM #CondFragments
                );
        DECLARE @gMax INT =
                (
                    SELECT MAX(GrpOrd)FROM #CondFragments
                );
        DECLARE @gLogic NVARCHAR(20);
        DECLARE @iLogic NVARCHAR(5);
        DECLARE @inner NVARCHAR(MAX);
        DECLARE @firstGrp BIT = 1;
        DECLARE @conn NVARCHAR(5);

        WHILE @gOrd <= @gMax
        BEGIN
            IF EXISTS (SELECT 1 FROM #CondFragments WHERE GrpOrd = @gOrd)
            BEGIN
                SELECT TOP 1
                       @gLogic = GrpLogic
                FROM #CondFragments
                WHERE GrpOrd = @gOrd;
                SET @gLogic = UPPER(LTRIM(RTRIM(ISNULL(@gLogic, N'AND'))));
                IF @gLogic NOT IN ( N'AND', N'OR', N'ANDNOT', N'OrNot' )
                    SET @gLogic = N'AND';

                SET @iLogic = CASE
                                  WHEN @gLogic LIKE N'OR%' THEN
                                      N'OR'
                                  ELSE
                                      N'AND'
                              END;

                SET @inner
                    = STUFF(
                               (
                                   SELECT N' ' + @iLogic + N' ' + cf.Fragment
                                   FROM #CondFragments cf
                                   WHERE cf.GrpOrd = @gOrd
                                   ORDER BY cf.CondOrd
                                   FOR XML PATH(N''), TYPE
                               ).value(N'.', N'NVARCHAR(MAX)'),
                               1,
                               LEN(@iLogic) + 2,
                               N''
                           );

                IF ISNULL(@inner, N'') <> N''
                BEGIN
                    IF @firstGrp = 1
                    BEGIN
                        SET @WhereFrag = CASE
                                             WHEN @gLogic LIKE N'%NOT' THEN
                                                 N'NOT '
                                             ELSE
                                                 N''
                                         END + N'(' + @inner + N')';
                        SET @firstGrp = 0;
                    END;
                    ELSE
                    BEGIN
                        SET @conn = CASE
                                        WHEN @ConditionLogic LIKE N'OR%' THEN
                                            N'OR'
                                        ELSE
                                            N'AND'
                                    END;
                        SET @WhereFrag = @WhereFrag + N' ' + @conn + CASE
                                                                         WHEN @gLogic LIKE N'%NOT' THEN
                                                                             N' NOT'
                                                                         ELSE
                                                                             N''
                                                                     END + N' (' + @inner + N')';
                    END;
                END;
            END;
            SET @gOrd += 1;
        END;
    END;

    SET @WhereClause = CASE
                           WHEN ISNULL(@WhereFrag, N'') = N'' THEN
                               N''
                           ELSE
                               N'WHERE ' + @WhereFrag
                       END;
END;";
                public const string usp_QueryForge_ExecGrouped = @"CREATE OR ALTER PROCEDURE [dbo].[usp_QueryForge_ExecGrouped]
    @FromSource NVARCHAR(MAX),
    @SelectList NVARCHAR(MAX),
    @WhereFrag NVARCHAR(MAX),
    @WhereClause NVARCHAR(MAX),
    @OrderByClause NVARCHAR(MAX),
    @PageSize INT,
    @PageNumber INT,
    @IsSP BIT,
    @ColDefs NVARCHAR(MAX),
    @ExecParamStr NVARCHAR(MAX),
    @QObj NVARCHAR(300),
    @TotalRows INT OUTPUT,
    @ModelsJSON NVARCHAR(MAX) OUTPUT,
    @HierarchySQL NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Q NCHAR(1) = NCHAR(39);
    DECLARE @PSz NVARCHAR(12) = CAST(@PageSize AS NVARCHAR(12));
    DECLARE @OfsStr NVARCHAR(12) = CAST((@PageNumber - 1) * @PageSize AS NVARCHAR(12));
    DECLARE @TmpTbl NVARCHAR(128) = N'#_QF_Result';

    DECLARE @GroupCount INT;
    SELECT @GroupCount = COUNT(*)
    FROM #GroupLevels;

    DECLARE @LvlIdx INT;
    DECLARE @LvlColQ NVARCHAR(132);
    DECLARE @LvlDir NVARCHAR(4);
    DECLARE @LvlAlias NVARCHAR(10);
    DECLARE @LvlDistinctSQL NVARCHAR(MAX);
    DECLARE @LvlOuterCond NVARCHAR(MAX);
    DECLARE @LvlOuterWhere NVARCHAR(MAX);
    DECLARE @LvlFullCond NVARCHAR(MAX);
    DECLARE @LvlCountWhere NVARCHAR(MAX);
    DECLARE @CountExpr NVARCHAR(MAX);
    DECLARE @innerLvl INT;
    DECLARE @innerColQ NVARCHAR(132);

    DECLARE @LeafCond NVARCHAR(MAX) = N'';
    SET @innerLvl = 1;
    WHILE @innerLvl <= @GroupCount
    BEGIN
        SELECT @innerColQ = ColQuoted
        FROM #GroupLevels
        WHERE LevelNum = @innerLvl;
        SET @LeafCond += N' AND (' + @innerColQ + N' = d' + CAST(@innerLvl AS NVARCHAR(4)) + N'.' + @innerColQ
                         + N' OR (' + @innerColQ + N' IS NULL AND d' + CAST(@innerLvl AS NVARCHAR(4)) + N'.'
                         + @innerColQ + N' IS NULL))';
        SET @innerLvl += 1;
    END;

    DECLARE @LeafWhere NVARCHAR(MAX);
    IF @WhereFrag = N''
        SET @LeafWhere = CASE
                             WHEN @LeafCond = N'' THEN
                                 N''
                             ELSE
                                 N'WHERE ' + STUFF(@LeafCond, 1, 5, N'')
                         END;
    ELSE
        SET @LeafWhere = N'WHERE (' + @WhereFrag + N')' + @LeafCond;

    SET @HierarchySQL
        = N'SELECT TOP 2147483647 ' + @SelectList + N' FROM ' + @FromSource + N' ' + @LeafWhere + N' ' + @OrderByClause
          + N' FOR JSON PATH, INCLUDE_NULL_VALUES';

    SET @LvlIdx = @GroupCount;
    WHILE @LvlIdx >= 1
    BEGIN
        SELECT @LvlColQ = ColQuoted,
               @LvlDir = SortDir
        FROM #GroupLevels
        WHERE LevelNum = @LvlIdx;
        SET @LvlAlias = N'd' + CAST(@LvlIdx AS NVARCHAR(4));

        SET @LvlOuterCond = N'';
        SET @innerLvl = 1;
        WHILE @innerLvl < @LvlIdx
        BEGIN
            SELECT @innerColQ = ColQuoted
            FROM #GroupLevels
            WHERE LevelNum = @innerLvl;
            SET @LvlOuterCond += N' AND (' + @innerColQ + N' = d' + CAST(@innerLvl AS NVARCHAR(4)) + N'.' + @innerColQ
                                 + N' OR (' + @innerColQ + N' IS NULL AND d' + CAST(@innerLvl AS NVARCHAR(4)) + N'.'
                                 + @innerColQ + N' IS NULL))';
            SET @innerLvl += 1;
        END;

        IF @WhereFrag = N''
            SET @LvlOuterWhere = CASE
                                     WHEN @LvlOuterCond = N'' THEN
                                         N''
                                     ELSE
                                         N'WHERE ' + STUFF(@LvlOuterCond, 1, 5, N'')
                                 END;
        ELSE
            SET @LvlOuterWhere = N'WHERE (' + @WhereFrag + N')' + @LvlOuterCond;

        SET @LvlFullCond
            = @LvlOuterCond + N' AND (' + @LvlColQ + N' = ' + @LvlAlias + N'.' + @LvlColQ + N' OR (' + @LvlColQ
              + N' IS NULL AND ' + @LvlAlias + N'.' + @LvlColQ + N' IS NULL))';

        IF @WhereFrag = N''
            SET @LvlCountWhere = CASE
                                     WHEN @LvlFullCond = N'' THEN
                                         N''
                                     ELSE
                                         N'WHERE ' + STUFF(@LvlFullCond, 1, 5, N'')
                                 END;
        ELSE
            SET @LvlCountWhere = N'WHERE (' + @WhereFrag + N')' + @LvlFullCond;

        SET @CountExpr = N'(SELECT COUNT(*) FROM ' + @FromSource + N' ' + @LvlCountWhere + N')';

        IF @LvlIdx = 1
        BEGIN
            SET @LvlDistinctSQL
                = N'SELECT DISTINCT ' + @LvlColQ + N' FROM ' + @FromSource + N' ' + @LvlOuterWhere + N' ORDER BY '
                  + @LvlColQ + N' ' + @LvlDir + N' OFFSET ' + @OfsStr + N' ROWS FETCH NEXT ' + @PSz + N' ROWS ONLY';

            SET @HierarchySQL
                = N'SELECT d1.' + @LvlColQ + N' AS [key],' + CHAR(10) + N'       ' + @CountExpr + N' AS [count],'
                  + CHAR(10) + N'       JSON_QUERY(ISNULL((' + @HierarchySQL + N'), N' + @Q + N'[]' + @Q
                  + N')) AS items' + CHAR(10) + N'FROM  (' + @LvlDistinctSQL + N') AS d1' + CHAR(10) + N'ORDER BY d1.'
                  + @LvlColQ + N' ' + @LvlDir + CHAR(10) + N'FOR JSON PATH, INCLUDE_NULL_VALUES';
        END;
        ELSE
        BEGIN
            SET @LvlDistinctSQL = N'SELECT DISTINCT ' + @LvlColQ + N' FROM ' + @FromSource + N' ' + @LvlOuterWhere;

            SET @HierarchySQL
                = N'SELECT ' + @LvlAlias + N'.' + @LvlColQ + N' AS [key],' + CHAR(10) + N'       ' + @CountExpr
                  + N' AS [count],' + CHAR(10) + N'       JSON_QUERY(ISNULL((' + @HierarchySQL + N'), N' + @Q + N'[]'
                  + @Q + N')) AS items' + CHAR(10) + N'FROM   (VALUES(0)) AS __anchor(v)' + CHAR(10) + N'CROSS APPLY ('
                  + @LvlDistinctSQL + N') AS ' + @LvlAlias + CHAR(10) + N'ORDER BY ' + @LvlAlias + N'.' + @LvlColQ
                  + N' ' + @LvlDir + CHAR(10) + N'FOR JSON PATH, INCLUDE_NULL_VALUES';
        END;

        SET @LvlIdx -= 1;
    END;

    DECLARE @TopLvlColQ NVARCHAR(132);
    SELECT @TopLvlColQ = ColQuoted
    FROM #GroupLevels
    WHERE LevelNum = 1;

    DECLARE @GrpCountSQL NVARCHAR(MAX)
        = N'SELECT @Total = COUNT(*) FROM (SELECT DISTINCT ' + @TopLvlColQ + N' FROM ' + @FromSource + N' '
          + @WhereClause + N') AS __GC';

    DECLARE @GrpDataSQL NVARCHAR(MAX) = N'SET @Result = (' + @HierarchySQL + N')';

    IF @IsSP = 1
    BEGIN
        DECLARE @SPGrpSQL NVARCHAR(MAX)
            = N'IF OBJECT_ID(N''tempdb..' + @TmpTbl + N''') IS NOT NULL DROP TABLE ' + @TmpTbl + N';' + CHAR(10)
              + N'CREATE TABLE ' + @TmpTbl + N' (' + @ColDefs + N');' + CHAR(10) + N'INSERT INTO ' + @TmpTbl
              + N' EXEC ' + @QObj + CASE
                                        WHEN ISNULL(@ExecParamStr, N'') <> N'' THEN
                                            N' ' + @ExecParamStr
                                        ELSE
                                            N''
                                    END + N';' + CHAR(10) + N'SELECT @Total = COUNT(*) FROM (SELECT DISTINCT '
              + @TopLvlColQ + N' FROM ' + @TmpTbl + N' ' + @WhereClause + N') AS __GC;' + CHAR(10) + N'SET @Result = ('
              + @HierarchySQL + N');' + CHAR(10) + N'DROP TABLE ' + @TmpTbl + N';';

        EXEC sp_executesql @SPGrpSQL,
                           N'@Total INT OUTPUT, @Result NVARCHAR(MAX) OUTPUT',
                           @Total = @TotalRows OUTPUT,
                           @Result = @ModelsJSON OUTPUT;
    END;
    ELSE
    BEGIN
        EXEC sp_executesql @GrpCountSQL,
                           N'@Total INT OUTPUT',
                           @Total = @TotalRows OUTPUT;
        EXEC sp_executesql @GrpDataSQL,
                           N'@Result NVARCHAR(MAX) OUTPUT',
                           @Result = @ModelsJSON OUTPUT;
    END;
END;";
                public const string usp_QueryForge = @"CREATE OR ALTER PROCEDURE [dbo].[usp_QueryForge]
    /* ── Object ──────────────────────────────────────────────────
       {
         ""Schema""     : ""dbo"",          -- optional, defaults to ""dbo""
         ""Name""       : ""UsersTable"",   -- REQUIRED
         ""Type""       : 0,              -- optional  0=Auto 1=Table 2=View 3=TVF 4=SP
         ""Parameters"" : {}              -- optional  TVF matched positionally, SP matched by name
       }
       ──────────────────────────────────────────────────────────── */
    @Object NVARCHAR(MAX),

    -- N'[""*""]' | N'[""UserId"",""UserName"",""IsActive"",""CreatedOn""]'
    @SelectColumns NVARCHAR(MAX) = N'[""*""]',

    /* ── Filter criteria ──────────────────────────────────────────
       {
         ""Logic""  : ""AND"",            -- optional  ""AND""|""OR""|""ANDNOT""|""OrNot""
         ""Groups"" : [
           {
             ""Logic"" : ""AND"",         -- per-group, suffix ""NOT"" negates
             ""Conditions"" : [
               { ""ColumnName"":""Name"", ""Operator"":""Contains"", ""Value"":""ali"", ""ValueTo"":null },
               ...
             ]
           }
         ]
       }
       Operators: Equals | NotEquals | Contains | NotContains | StartsWith | EndsWith
                  LessThan | GreaterThan | LessThanOrEqualTo | GreaterThanOrEqualTo
                  Between  (ValueTo required)
       ──────────────────────────────────────────────────────────── */
    @Criteria NVARCHAR(MAX) = N'{}',

    /* ── Sort columns ─────────────────────────────────────────────
       N'[{""ColumnName"":""CreatedOn"",""SortOrder"":""Descending""},{""ColumnName"":""Name"",""SortOrder"":""Ascending""}]'
       SortOrder accepts: Ascending | ASC | Descending | DESC   (case-insensitive)
       ──────────────────────────────────────────────────────────── */
    @SortColumns NVARCHAR(MAX) = N'[]',

    /* ── Group-by columns ─────────────────────────────────────────
       Pagination applies to the outermost (first) group level.
       Result becomes a nested  key / count / items  tree.
       N'[{""ColumnName"":""Country"",""SortOrder"":""Ascending""},{""ColumnName"":""City"",""SortOrder"":""Descending""}]'
       ──────────────────────────────────────────────────────────── */
    @GroupByColumns NVARCHAR(MAX) = N'[]',

    /* ── Paging ───────────────────────────────────────────────────
       {
         ""Size""   : 12,    -- rows per page (flat) or top-level groups per page (grouped). Default 12.
         ""Number"" : 1      -- 1-based page number. Default 1.
       }
       ──────────────────────────────────────────────────────────── */
    @Paging NVARCHAR(MAX) = N'{}',

    -- 0 = off (default). 1 = PRINT a full debug report including all generated SQL strings.
    @DebugMode BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StartTime DATETIME2(3) = SYSDATETIME();
    DECLARE @Q NCHAR(1) = NCHAR(39);

    /* ── Unpack @Object ────────────────────────────────────────── */
    DECLARE @ObjectSchemaName NVARCHAR(128);
    DECLARE @ObjectName NVARCHAR(128);
    DECLARE @ObjectType TINYINT;
    DECLARE @ObjectParameters NVARCHAR(MAX);

    SET @ObjectSchemaName = ISNULL(JSON_VALUE(@Object, N'$.Schema'), N'dbo');
    SET @ObjectName = JSON_VALUE(@Object, N'$.Name');
    SET @ObjectType = CAST(ISNULL(JSON_VALUE(@Object, N'$.Type'), N'0') AS TINYINT);
    SET @ObjectParameters = ISNULL(JSON_QUERY(@Object, N'$.Parameters'), N'{}');

    IF ISNULL(@ObjectName, N'') = N''
    BEGIN
        RAISERROR(N'[usp_QueryForge] @Object JSON must include a non-empty ""Name"" property.', 16, 1);
        RETURN -1;
    END;

    /* ── Unpack @Criteria ──────────────────────────────────────── */
    DECLARE @ConditionLogic NVARCHAR(10);
    DECLARE @Conditions NVARCHAR(MAX);

    SET @ConditionLogic = UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(@Criteria, N'$.Logic'), N'AND'))));
    IF @ConditionLogic NOT IN ( N'AND', N'OR', N'ANDNOT', N'OrNot' )
        SET @ConditionLogic = N'AND';
    SET @Conditions = ISNULL(JSON_QUERY(@Criteria, N'$.Groups'), N'[]');

    /* ── Unpack @Paging ────────────────────────────────────────── */
    DECLARE @PageSize INT;
    DECLARE @PageNumber INT;

    SET @PageSize = CAST(ISNULL(NULLIF(LTRIM(RTRIM(JSON_VALUE(@Paging, N'$.Size'))), N''), N'12') AS INT);
    SET @PageNumber = CAST(ISNULL(NULLIF(LTRIM(RTRIM(JSON_VALUE(@Paging, N'$.Number'))), N''), N'1') AS INT);

    IF ISNULL(@PageSize, 0) < 1
        SET @PageSize = 12;
    IF ISNULL(@PageNumber, 0) < 1
        SET @PageNumber = 1;

    /* ── Resolve object ────────────────────────────────────────── */
    DECLARE @ObjectId INT;
    DECLARE @ObjectKind NVARCHAR(4);

    SELECT @ObjectId = o.object_id,
           @ObjectKind = RTRIM(o.type)
    FROM sys.objects o
        JOIN sys.schemas s
            ON s.schema_id = o.schema_id
    WHERE s.name = @ObjectSchemaName
          AND o.name = @ObjectName
          AND
          (
              (
                  @ObjectType = 0
                  AND o.type IN ( 'U', 'V', 'IF', 'TF', 'FT', 'P' )
              )
              OR
              (
                  @ObjectType = 1
                  AND o.type = 'U'
              )
              OR
              (
                  @ObjectType = 2
                  AND o.type = 'V'
              )
              OR
              (
                  @ObjectType = 3
                  AND o.type IN ( 'IF', 'TF', 'FT' )
              )
              OR
              (
                  @ObjectType = 4
                  AND o.type = 'P'
              )
          );

    IF @ObjectId IS NULL
    BEGIN
        RAISERROR(
                     N'[usp_QueryForge] Object [%s].[%s] not found or Type (%d) mismatch.',
                     16,
                     1,
                     @ObjectSchemaName,
                     @ObjectName,
                     @ObjectType
                 );
        RETURN -2;
    END;

    DECLARE @QObj NVARCHAR(300) = QUOTENAME(@ObjectSchemaName) + N'.' + QUOTENAME(@ObjectName);
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    DECLARE @PSz NVARCHAR(12) = CAST(@PageSize AS NVARCHAR(12));
    DECLARE @PNum NVARCHAR(12) = CAST(@PageNumber AS NVARCHAR(12));
    DECLARE @OfsStr NVARCHAR(12) = CAST(@Offset AS NVARCHAR(12));

    DECLARE @FromSource NVARCHAR(MAX) = N'';
    DECLARE @IsSP BIT = 0;
    DECLARE @ExecParamStr NVARCHAR(MAX) = N'';
    DECLARE @ColDefs NVARCHAR(MAX) = N'';
    DECLARE @TmpTbl NVARCHAR(128) = N'#_QF_Result';
    DECLARE @DescStmt NVARCHAR(MAX) = N'';

    CREATE TABLE #ValidColumns
    (
        ColOrdinal INT NOT NULL,
        ColName NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
        ColType NVARCHAR(256) COLLATE DATABASE_DEFAULT NULL
    );

    /* ── Build column catalogue & FROM source ──────────────────── */
    IF @ObjectKind = N'P'
    BEGIN
        SET @IsSP = 1;

        SELECT @ExecParamStr = STUFF(
        (
            SELECT N', ' + sp.name + N' = N' + @Q + REPLACE(pv.[Value], @Q, @Q + @Q) + @Q
            FROM sys.parameters sp
                OUTER APPLY
            (
                SELECT TOP 1
                       j.[value]
                FROM OPENJSON(@ObjectParameters) j
                WHERE j.[key] = STUFF(sp.name, 1, 1, N'')COLLATE DATABASE_DEFAULT
            ) pv
            WHERE sp.object_id = @ObjectId
                  AND sp.parameter_id > 0
                  AND pv.[Value] IS NOT NULL
            ORDER BY sp.parameter_id
            FOR XML PATH(N''), TYPE
        ).value(N'.', N'NVARCHAR(MAX)'),
        1   ,
        2   ,
        N''
                                    );

        SET @DescStmt = N'EXEC ' + @QObj + CASE
                                               WHEN ISNULL(@ExecParamStr, N'') <> N'' THEN
                                                   N' ' + @ExecParamStr
                                               ELSE
                                                   N''
                                           END;

        BEGIN TRY
            INSERT INTO #ValidColumns
            (
                ColOrdinal,
                ColName,
                ColType
            )
            SELECT column_ordinal,
                   name,
                   system_type_name
            FROM sys.dm_exec_describe_first_result_set(@DescStmt, NULL, 0)
            WHERE is_hidden = 0
                  AND name IS NOT NULL;
        END TRY
        BEGIN CATCH
            RAISERROR(
                         N'[usp_QueryForge] Cannot describe result set of [%s].[%s]. SPs that rely on #temp tables internally cannot be described — wrap them in a view or TVF.',
                         16,
                         1,
                         @ObjectSchemaName,
                         @ObjectName
                     );
            RETURN -3;
        END CATCH;

        SELECT @ColDefs = STUFF(
        (
            SELECT N', ' + QUOTENAME(ColName) + N' ' + ISNULL(ColType, N'NVARCHAR(MAX)') + N' NULL'
            FROM #ValidColumns
            ORDER BY ColOrdinal
            FOR XML PATH(N''), TYPE
        ).value(N'.', N'NVARCHAR(MAX)'),
        1   ,
        2   ,
        N''
                               );

        SET @FromSource = @TmpTbl;
    END;
    ELSE IF @ObjectKind IN ( N'IF', N'TF', N'FT' )
    BEGIN
        SELECT @ExecParamStr = STUFF(
        (
            SELECT N', ' + CASE
                               WHEN pv.[Value] IS NULL THEN
                                   N'NULL'
                               ELSE
                                   N'N' + @Q + REPLACE(pv.[Value], @Q, @Q + @Q) + @Q
                           END
            FROM sys.parameters sp
                OUTER APPLY
            (
                SELECT TOP 1
                       j.[value]
                FROM OPENJSON(@ObjectParameters) j
                WHERE j.[key] = STUFF(sp.name, 1, 1, N'')COLLATE DATABASE_DEFAULT
            ) pv
            WHERE sp.object_id = @ObjectId
                  AND sp.parameter_id > 0
            ORDER BY sp.parameter_id
            FOR XML PATH(N''), TYPE
        ).value(N'.', N'NVARCHAR(MAX)'),
        1   ,
        2   ,
        N''
                                    );

        SET @FromSource = @QObj + N'(' + ISNULL(@ExecParamStr, N'') + N')';

        INSERT INTO #ValidColumns
        (
            ColOrdinal,
            ColName,
            ColType
        )
        SELECT c.column_id,
               c.name,
               tp.name
        FROM sys.columns c
            JOIN sys.types tp
                ON tp.user_type_id = c.user_type_id
        WHERE c.object_id = @ObjectId;
    END;
    ELSE
    BEGIN
        SET @FromSource = @QObj;

        INSERT INTO #ValidColumns
        (
            ColOrdinal,
            ColName,
            ColType
        )
        SELECT c.column_id,
               c.name,
               tp.name
        FROM sys.columns c
            JOIN sys.types tp
                ON tp.user_type_id = c.user_type_id
        WHERE c.object_id = @ObjectId;
    END;

    IF NOT EXISTS (SELECT 1 FROM #ValidColumns)
    BEGIN
        RAISERROR(N'[usp_QueryForge] No columns resolved for [%s].[%s].', 16, 1, @ObjectSchemaName, @ObjectName);
        RETURN -4;
    END;

    /* ── Build SELECT list ─────────────────────────────────────── */
    DECLARE @SelectList NVARCHAR(MAX);

    IF ISNULL(@SelectColumns, N'') IN ( N'', N'[]', N'[""*""]', N'[""""]' )
        SET @SelectList = N'*';
    ELSE
    BEGIN
        SELECT @SelectList = STUFF(
        (
            SELECT N', ' + QUOTENAME(vc.ColName)
            FROM OPENJSON(@SelectColumns) j
                JOIN #ValidColumns vc
                    ON vc.ColName = j.value
            ORDER BY CAST(j.[key] AS INT)
            FOR XML PATH(N''), TYPE
        ).value(N'.', N'NVARCHAR(MAX)'),
        1   ,
        2   ,
        N''
                                  );

        IF ISNULL(@SelectList, N'') = N''
            SET @SelectList = N'*';
    END;

    /* ── Build WHERE clause ────────────────────────────────────── */
    CREATE TABLE #CondFragments
    (
        GrpOrd INT NOT NULL,
        GrpLogic NVARCHAR(20) COLLATE DATABASE_DEFAULT NOT NULL,
        CondOrd INT NOT NULL,
        Fragment NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL
    );

    DECLARE @WhereFrag NVARCHAR(MAX);
    DECLARE @WhereClause NVARCHAR(MAX);

    EXEC usp_QueryForge_BuildWhere @Conditions = @Conditions,
                                   @ConditionLogic = @ConditionLogic,
                                   @WhereFrag = @WhereFrag OUTPUT,
                                   @WhereClause = @WhereClause OUTPUT;

    /* ── Build GROUP BY levels ─────────────────────────────────── */
    CREATE TABLE #GroupLevels
    (
        LevelNum INT NOT NULL PRIMARY KEY,
        ColName NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
        ColQuoted NVARCHAR(132) COLLATE DATABASE_DEFAULT NOT NULL,
        SortDir NVARCHAR(4) COLLATE DATABASE_DEFAULT NOT NULL
    );

    IF ISNULL(@GroupByColumns, N'') NOT IN ( N'', N'[]' )
        INSERT INTO #GroupLevels
        (
            LevelNum,
            ColName,
            ColQuoted,
            SortDir
        )
        SELECT CAST(j.[key] AS INT) + 1,
               vc.ColName,
               QUOTENAME(vc.ColName),
               CASE
                   WHEN UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(j.value, N'$.SortOrder'), N'')))) IN ( N'DESC',
                                                                                                   N'DESCENDING'
                                                                                                 ) THEN
                       N'DESC'
                   ELSE
                       N'ASC'
               END
        FROM OPENJSON(@GroupByColumns) j
            JOIN #ValidColumns vc
                ON vc.ColName = JSON_VALUE(j.value, N'$.ColumnName')
        ORDER BY CAST(j.[key] AS INT);

    DECLARE @GroupCount INT =
            (
                SELECT COUNT(*)FROM #GroupLevels
            );

    /* ── Build ORDER BY ────────────────────────────────────────── */
    DECLARE @OrderParts NVARCHAR(MAX) = N'';

    IF ISNULL(@SortColumns, N'') NOT IN ( N'', N'[]' )
        SELECT @OrderParts
            = STUFF(
        (
            SELECT N', ' + QUOTENAME(vc.ColName) + N' '
                   + CASE
                         WHEN UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(j.value, N'$.SortOrder'), N'')))) IN ( N'DESC',
                                                                                                         N'DESCENDING'
                                                                                                       ) THEN
                             N'DESC'
                         ELSE
                             N'ASC'
                     END
            FROM OPENJSON(@SortColumns) j
                JOIN #ValidColumns vc
                    ON vc.ColName = JSON_VALUE(j.value, N'$.ColumnName')
            ORDER BY CAST(j.[key] AS INT)
            FOR XML PATH(N''), TYPE
        ).value(N'.', N'NVARCHAR(MAX)'),
        1   ,
        2   ,
        N''
                   );

    IF ISNULL(@OrderParts, N'') = N''
        SET @OrderParts = N'(SELECT NULL)';
    DECLARE @OrderByClause NVARCHAR(MAX) = N'ORDER BY ' + @OrderParts;

    /* ── Execute ───────────────────────────────────────────────── */
    DECLARE @TotalRows INT = 0;
    DECLARE @TotalPages INT = 0;
    DECLARE @ModelsJSON NVARCHAR(MAX) = N'[]';
    DECLARE @HierarchySQL NVARCHAR(MAX) = N'';

    IF @GroupCount = 0
    BEGIN
        DECLARE @FlatCountSQL NVARCHAR(MAX) = N'SELECT @Total = COUNT(*) FROM ' + @FromSource + N' ' + @WhereClause;

        DECLARE @FlatDataSQL NVARCHAR(MAX)
            = N'SET @Result = (SELECT ' + @SelectList + N' FROM ' + @FromSource + N' ' + @WhereClause + N' '
              + @OrderByClause + N' OFFSET ' + @OfsStr + N' ROWS FETCH NEXT ' + @PSz + N' ROWS ONLY'
              + N' FOR JSON PATH, INCLUDE_NULL_VALUES)';

        IF @IsSP = 1
        BEGIN
            DECLARE @SPFlatSQL NVARCHAR(MAX)
                = N'IF OBJECT_ID(N''tempdb..' + @TmpTbl + N''') IS NOT NULL DROP TABLE ' + @TmpTbl + N';' + CHAR(10)
                  + N'CREATE TABLE ' + @TmpTbl + N' (' + @ColDefs + N');' + CHAR(10) + N'INSERT INTO ' + @TmpTbl
                  + N' EXEC ' + @QObj + CASE
                                            WHEN ISNULL(@ExecParamStr, N'') <> N'' THEN
                                                N' ' + @ExecParamStr
                                            ELSE
                                                N''
                                        END + N';' + CHAR(10) + N'SELECT @Total = COUNT(*) FROM ' + @TmpTbl + N' '
                  + @WhereClause + N';' + CHAR(10) + N'SET @Result = (SELECT ' + @SelectList + N' FROM ' + @TmpTbl
                  + N' ' + @WhereClause + N' ' + @OrderByClause + N' OFFSET ' + @OfsStr + N' ROWS FETCH NEXT ' + @PSz
                  + N' ROWS ONLY' + N' FOR JSON PATH, INCLUDE_NULL_VALUES);' + CHAR(10) + N'DROP TABLE ' + @TmpTbl
                  + N';';

            EXEC sp_executesql @SPFlatSQL,
                               N'@Total INT OUTPUT, @Result NVARCHAR(MAX) OUTPUT',
                               @Total = @TotalRows OUTPUT,
                               @Result = @ModelsJSON OUTPUT;
        END;
        ELSE
        BEGIN
            EXEC sp_executesql @FlatCountSQL,
                               N'@Total INT OUTPUT',
                               @Total = @TotalRows OUTPUT;
            EXEC sp_executesql @FlatDataSQL,
                               N'@Result NVARCHAR(MAX) OUTPUT',
                               @Result = @ModelsJSON OUTPUT;
        END;
    END;
    ELSE
    BEGIN
        EXEC usp_QueryForge_ExecGrouped @FromSource = @FromSource,
                                        @SelectList = @SelectList,
                                        @WhereFrag = @WhereFrag,
                                        @WhereClause = @WhereClause,
                                        @OrderByClause = @OrderByClause,
                                        @PageSize = @PageSize,
                                        @PageNumber = @PageNumber,
                                        @IsSP = @IsSP,
                                        @ColDefs = @ColDefs,
                                        @ExecParamStr = @ExecParamStr,
                                        @QObj = @QObj,
                                        @TotalRows = @TotalRows OUTPUT,
                                        @ModelsJSON = @ModelsJSON OUTPUT,
                                        @HierarchySQL = @HierarchySQL OUTPUT;
    END;

    /* ── Final output ──────────────────────────────────────────── */
    SET @TotalRows = ISNULL(@TotalRows, 0);
    SET @TotalPages = CASE
                          WHEN @TotalRows = 0
                               OR @PageSize = 0 THEN
                              0
                          ELSE
                              CEILING(@TotalRows * 1.0 / @PageSize)
                      END;
    SET @ModelsJSON = ISNULL(@ModelsJSON, N'[]');

    DECLARE @ElapsedMs INT = DATEDIFF(MILLISECOND, @StartTime, SYSDATETIME());

    IF @DebugMode = 1
    BEGIN
        PRINT N'';
        PRINT N'╔══════════════════════════════════════════════════════════════════╗';
        PRINT N'║  usp_QueryForge  DEBUG REPORT                                   ║';
        PRINT N'╠══════════════════════════════════════════════════════════════════╣';
        PRINT N'║  INPUTS';
        PRINT N'║  @Object (raw)      : ' + ISNULL(@Object, N'NULL');
        PRINT N'║    → Schema         : ' + @ObjectSchemaName;
        PRINT N'║    → Name           : ' + @ObjectName;
        PRINT N'║    → Type           : ' + CAST(@ObjectType AS NVARCHAR) + N'  (resolved kind=' + @ObjectKind + N')';
        PRINT N'║    → Parameters     : ' + ISNULL(@ObjectParameters, N'{}');
        PRINT N'║  @SelectColumns     : ' + ISNULL(@SelectColumns, N'NULL');
        PRINT N'║  @Criteria (raw)    : ' + ISNULL(@Criteria, N'NULL');
        PRINT N'║    → Logic          : ' + @ConditionLogic;
        PRINT N'║    → Groups         : ' + ISNULL(@Conditions, N'[]');
        PRINT N'║  @SortColumns       : ' + ISNULL(@SortColumns, N'NULL');
        PRINT N'║  @GroupByColumns    : ' + ISNULL(@GroupByColumns, N'NULL');
        PRINT N'║  @Paging (raw)      : ' + ISNULL(@Paging, N'NULL');
        PRINT N'║    → Size           : ' + @PSz;
        PRINT N'║    → Number         : ' + @PNum;
        PRINT N'╠══════════════════════════════════════════════════════════════════╣';
        PRINT N'║  RESOLVED OBJECT';
        PRINT N'║  QualifiedName      : ' + @QObj;
        PRINT N'║  FromSource         : ' + @FromSource;
        IF @IsSP = 1
        BEGIN
            PRINT N'║  DescStmt           : ' + ISNULL(@DescStmt, N'');
            PRINT N'║  ExecParamStr       : ' + ISNULL(@ExecParamStr, N'<none>');
            PRINT N'║  ColDefs            : ' + ISNULL(@ColDefs, N'');
        END;
        IF @ObjectKind IN ( N'IF', N'TF', N'FT' )
            PRINT N'║  TVF params call    : ' + ISNULL(@ExecParamStr, N'<none>');
        PRINT N'╠══════════════════════════════════════════════════════════════════╣';
        PRINT N'║  COLUMN CATALOGUE';
        DECLARE @colList NVARCHAR(MAX) = N'';
        SELECT @colList += N'  [' + ColName + N'] ' + ISNULL(ColType, N'?') + N'  |'
        FROM #ValidColumns
        ORDER BY ColOrdinal;
        PRINT N'║  ' + @colList;
        PRINT N'╠══════════════════════════════════════════════════════════════════╣';
        PRINT N'║  BUILT QUERY PARTS';
        PRINT N'║  SelectList         : ' + @SelectList;
        PRINT N'║  WhereFrag          : ' + ISNULL(@WhereFrag, N'<empty>');
        PRINT N'║  WhereClause        : ' + ISNULL(@WhereClause, N'<empty>');
        PRINT N'║  OrderByClause      : ' + @OrderByClause;
        PRINT N'║  GroupCount         : ' + CAST(@GroupCount AS NVARCHAR);
        PRINT N'║  Offset             : ' + @OfsStr;
        IF @GroupCount > 0
        BEGIN
            PRINT N'╠══════════════════════════════════════════════════════════════════╣';
            PRINT N'║  GROUP LEVELS';
            DECLARE @grpList NVARCHAR(MAX) = N'';
            SELECT @grpList += N'  L' + CAST(LevelNum AS NVARCHAR) + N':' + ColName + N' ' + SortDir + N'  |'
            FROM #GroupLevels
            ORDER BY LevelNum;
            PRINT N'║  ' + @grpList;
            PRINT N'╠══════════════════════════════════════════════════════════════════╣';
            PRINT N'║  HIERARCHY SQL';
            DECLARE @hPos INT = 1;
            WHILE @hPos <= LEN(@HierarchySQL)
            BEGIN
                PRINT SUBSTRING(@HierarchySQL, @hPos, 3800);
                SET @hPos += 3800;
            END;
        END;
        PRINT N'╠══════════════════════════════════════════════════════════════════╣';
        PRINT N'║  RESULTS';
        PRINT N'║  Total.Rows         : ' + CAST(@TotalRows AS NVARCHAR);
        PRINT N'║  Total.Pages        : ' + CAST(@TotalPages AS NVARCHAR);
        PRINT N'║  Type             : ' + CASE
                                              WHEN @GroupCount > 0 THEN
                                                  'Grouped'
                                              ELSE
                                                  'Flat'
                                          END;
        PRINT N'║  Elapsed (ms)       : ' + CAST(@ElapsedMs AS NVARCHAR);
        PRINT N'╚══════════════════════════════════════════════════════════════════╝';
    END;

    SELECT JSON_QUERY(   N'{""Total"":{""Rows"":' + CAST(@TotalRows AS NVARCHAR(20)) + N',""Pages"":'
                         + CAST(@TotalPages AS NVARCHAR(20)) + N'},""Type"":""' + CASE
                                                                                   WHEN @GroupCount > 0 THEN
                                                                                       'Grouped'
                                                                                   ELSE
                                                                                       'Flat'
                                                                               END + N'""}'
                     ) AS Meta,
           JSON_QUERY(@ModelsJSON) AS [Data];

    RETURN 0;
END;";
            }
            public const string StoredProcedureName = "usp_QueryForge";
            public const string RawQuery = @"-- ╔══════════════════════════════════════════════════════════════════════╗
-- ║  QueryForge — Ad-Hoc Edition  |  SQL Server 2016+  |  compat ≥ 130 ║
-- ║  Copy this entire script, fill in PARAMETERS below, and execute.    ║
-- ║  No CREATE PROCEDURE permission required.                            ║
-- ╚══════════════════════════════════════════════════════════════════════╝

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @StartTime DATETIME2(3) = SYSDATETIME();
DECLARE @Q NCHAR(1) = NCHAR(39);

-- Re-runnable safety
DROP TABLE IF EXISTS #ValidColumns;
DROP TABLE IF EXISTS #CondFragments;
DROP TABLE IF EXISTS #GroupLevels;

/* ── Unpack @Object ──────────────────────────────────────────── */
DECLARE @ObjectSchemaName NVARCHAR(128) = ISNULL(JSON_VALUE(@Object, N'$.Schema'), N'dbo');
DECLARE @ObjectName NVARCHAR(128) = JSON_VALUE(@Object, N'$.Name');
DECLARE @DapperObjectType TINYINT = CAST(ISNULL(JSON_VALUE(@Object, N'$.Type'), N'0') AS TINYINT);
DECLARE @ObjectParameters NVARCHAR(MAX) = ISNULL(JSON_QUERY(@Object, N'$.Parameters'), N'{}');

IF ISNULL(@ObjectName, N'') = N''
BEGIN
    RAISERROR(N'[QueryForge] @Object JSON must include a non-empty ""Name"" property.', 16, 1);
    RETURN;
END;

/* ── Unpack @Criteria ────────────────────────────────────────── */
DECLARE @ConditionLogic NVARCHAR(10) = UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(@Criteria, N'$.Logic'), N'AND'))));
IF @ConditionLogic NOT IN ( N'AND', N'OR', N'AND NOT', N'OR NOT' )
    SET @ConditionLogic = N'AND';
DECLARE @Conditions NVARCHAR(MAX) = ISNULL(JSON_QUERY(@Criteria, N'$.Groups'), N'[]');

/* ── Unpack @Paging ──────────────────────────────────────────── */
DECLARE @PageSize INT = CAST(ISNULL(NULLIF(LTRIM(RTRIM(JSON_VALUE(@Paging, N'$.Size'))), N''), N'12') AS INT);
DECLARE @PageNumber INT = CAST(ISNULL(NULLIF(LTRIM(RTRIM(JSON_VALUE(@Paging, N'$.Number'))), N''), N'1') AS INT);
IF ISNULL(@PageSize, 0) < 1
    SET @PageSize = 12;
IF ISNULL(@PageNumber, 0) < 1
    SET @PageNumber = 1;

/* ── Resolve object ──────────────────────────────────────────── */
DECLARE @ObjectId INT;
DECLARE @ObjectKind NVARCHAR(4);

SELECT @ObjectId = o.object_id,
       @ObjectKind = RTRIM(o.type)
FROM sys.objects o
    JOIN sys.schemas s
        ON s.schema_id = o.schema_id
WHERE s.name = @ObjectSchemaName
      AND o.name = @ObjectName
      AND
      (
          (
              @DapperObjectType = 0
              AND o.type IN ( 'U', 'V', 'IF', 'TF', 'FT', 'P' )
          )
          OR
          (
              @DapperObjectType = 1
              AND o.type = 'U'
          )
          OR
          (
              @DapperObjectType = 2
              AND o.type = 'V'
          )
          OR
          (
              @DapperObjectType = 3
              AND o.type IN ( 'IF', 'TF', 'FT' )
          )
          OR
          (
              @DapperObjectType = 4
              AND o.type = 'P'
          )
      );

IF @ObjectId IS NULL
BEGIN
    RAISERROR(
                 N'[QueryForge] Object [%s].[%s] not found or Type (%d) mismatch.',
                 16,
                 1,
                 @ObjectSchemaName,
                 @ObjectName,
                 @DapperObjectType
             );
    RETURN;
END;

DECLARE @QObj NVARCHAR(300) = QUOTENAME(@ObjectSchemaName) + N'.' + QUOTENAME(@ObjectName);
DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
DECLARE @PSz NVARCHAR(12) = CAST(@PageSize AS NVARCHAR(12));
DECLARE @PNum NVARCHAR(12) = CAST(@PageNumber AS NVARCHAR(12));
DECLARE @OfsStr NVARCHAR(12) = CAST(@Offset AS NVARCHAR(12));

DECLARE @FromSource NVARCHAR(MAX) = N'';
DECLARE @IsSP BIT = 0;
DECLARE @ExecParamStr NVARCHAR(MAX) = N'';
DECLARE @ColDefs NVARCHAR(MAX) = N'';
DECLARE @TmpTbl NVARCHAR(128) = N'#_QF_Result';
DECLARE @DescStmt NVARCHAR(MAX) = N'';

CREATE TABLE #ValidColumns
(
    ColOrdinal INT NOT NULL,
    ColName NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
    ColType NVARCHAR(256) COLLATE DATABASE_DEFAULT NULL
);

/* ── Build column catalogue & FROM source ────────────────────── */
IF @ObjectKind = N'P'
BEGIN
    SET @IsSP = 1;

    SELECT @ExecParamStr = STUFF(
    (
        SELECT N', ' + sp.name + N' = N' + @Q + REPLACE(pv.[Value], @Q, @Q + @Q) + @Q
        FROM sys.parameters sp
            OUTER APPLY
        (
            SELECT TOP 1
                   j.[value]
            FROM OPENJSON(@ObjectParameters) j
            WHERE j.[key] = STUFF(sp.name, 1, 1, N'')COLLATE DATABASE_DEFAULT
        ) pv
        WHERE sp.object_id = @ObjectId
              AND sp.parameter_id > 0
              AND pv.[Value] IS NOT NULL
        ORDER BY sp.parameter_id
        FOR XML PATH(N''), TYPE
    ).value(N'.', N'NVARCHAR(MAX)'),
    1   ,
    2   ,
    N''
                                );

    SET @DescStmt = N'EXEC ' + @QObj + CASE
                                           WHEN ISNULL(@ExecParamStr, N'') <> N'' THEN
                                               N' ' + @ExecParamStr
                                           ELSE
                                               N''
                                       END;

    BEGIN TRY
        INSERT INTO #ValidColumns
        (
            ColOrdinal,
            ColName,
            ColType
        )
        SELECT column_ordinal,
               name,
               system_type_name
        FROM sys.dm_exec_describe_first_result_set(@DescStmt, NULL, 0)
        WHERE is_hidden = 0
              AND name IS NOT NULL;
    END TRY
    BEGIN CATCH
        RAISERROR(
                     N'[QueryForge] Cannot describe result set of [%s].[%s]. SPs with internal #temp tables cannot be described — wrap in a view or TVF.',
                     16,
                     1,
                     @ObjectSchemaName,
                     @ObjectName
                 );
        RETURN;
    END CATCH;

    SELECT @ColDefs = STUFF(
    (
        SELECT N', ' + QUOTENAME(ColName) + N' ' + ISNULL(ColType, N'NVARCHAR(MAX)') + N' NULL'
        FROM #ValidColumns
        ORDER BY ColOrdinal
        FOR XML PATH(N''), TYPE
    ).value(N'.', N'NVARCHAR(MAX)'),
    1   ,
    2   ,
    N''
                           );

    SET @FromSource = @TmpTbl;
END;
ELSE IF @ObjectKind IN ( N'IF', N'TF', N'FT' )
BEGIN
    SELECT @ExecParamStr = STUFF(
    (
        SELECT N', ' + CASE
                           WHEN pv.[Value] IS NULL THEN
                               N'NULL'
                           ELSE
                               N'N' + @Q + REPLACE(pv.[Value], @Q, @Q + @Q) + @Q
                       END
        FROM sys.parameters sp
            OUTER APPLY
        (
            SELECT TOP 1
                   j.[value]
            FROM OPENJSON(@ObjectParameters) j
            WHERE j.[key] = STUFF(sp.name, 1, 1, N'')COLLATE DATABASE_DEFAULT
        ) pv
        WHERE sp.object_id = @ObjectId
              AND sp.parameter_id > 0
        ORDER BY sp.parameter_id
        FOR XML PATH(N''), TYPE
    ).value(N'.', N'NVARCHAR(MAX)'),
    1   ,
    2   ,
    N''
                                );

    SET @FromSource = @QObj + N'(' + ISNULL(@ExecParamStr, N'') + N')';

    INSERT INTO #ValidColumns
    (
        ColOrdinal,
        ColName,
        ColType
    )
    SELECT c.column_id,
           c.name,
           tp.name
    FROM sys.columns c
        JOIN sys.types tp
            ON tp.user_type_id = c.user_type_id
    WHERE c.object_id = @ObjectId;
END;
ELSE
BEGIN
    SET @FromSource = @QObj;

    INSERT INTO #ValidColumns
    (
        ColOrdinal,
        ColName,
        ColType
    )
    SELECT c.column_id,
           c.name,
           tp.name
    FROM sys.columns c
        JOIN sys.types tp
            ON tp.user_type_id = c.user_type_id
    WHERE c.object_id = @ObjectId;
END;

IF NOT EXISTS (SELECT 1 FROM #ValidColumns)
BEGIN
    RAISERROR(N'[QueryForge] No columns resolved for [%s].[%s].', 16, 1, @ObjectSchemaName, @ObjectName);
    RETURN;
END;

/* ── Build SELECT list ───────────────────────────────────────── */
DECLARE @SelectList NVARCHAR(MAX);

IF ISNULL(@SelectColumns, N'') IN ( N'', N'[]', N'[""*""]', N'[""""]' )
    SET @SelectList = N'*';
ELSE
BEGIN
    SELECT @SelectList = STUFF(
    (
        SELECT N', ' + QUOTENAME(vc.ColName)
        FROM OPENJSON(@SelectColumns) j
            JOIN #ValidColumns vc
                ON vc.ColName = j.value
        ORDER BY CAST(j.[key] AS INT)
        FOR XML PATH(N''), TYPE
    ).value(N'.', N'NVARCHAR(MAX)'),
    1   ,
    2   ,
    N''
                              );

    IF ISNULL(@SelectList, N'') = N''
        SET @SelectList = N'*';
END;

/* ══════════════════════════════════════════════════════════════
   Inlined: usp_QueryForge_BuildWhere
   ══════════════════════════════════════════════════════════════ */
CREATE TABLE #CondFragments
(
    GrpOrd INT NOT NULL,
    GrpLogic NVARCHAR(20) COLLATE DATABASE_DEFAULT NOT NULL,
    CondOrd INT NOT NULL,
    Fragment NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL
);

DECLARE @WhereFrag NVARCHAR(MAX);
DECLARE @WhereClause NVARCHAR(MAX);

INSERT INTO #CondFragments
(
    GrpOrd,
    GrpLogic,
    CondOrd,
    Fragment
)
SELECT CAST(g.[key] AS INT),
       UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(g.value, N'$.Logic'), @ConditionLogic)))),
       CAST(c.[key] AS INT),
       CASE JSON_VALUE(c.value, N'$.Operator')
           WHEN N'Equals' THEN
               CASE
                   WHEN JSON_VALUE(c.value, N'$.Value') IS NULL THEN
                       QUOTENAME(vc.ColName) + N' IS NULL'
                   ELSE
                       QUOTENAME(vc.ColName) + N' = N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q)
                       + @Q
               END
           WHEN N'NotEquals' THEN
               CASE
                   WHEN JSON_VALUE(c.value, N'$.Value') IS NULL THEN
                       QUOTENAME(vc.ColName) + N' IS NOT NULL'
                   ELSE
                       QUOTENAME(vc.ColName) + N' <> N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q)
                       + @Q
               END
           WHEN N'Contains' THEN
               QUOTENAME(vc.ColName) + N' LIKE N' + @Q + N'%'
               + REPLACE(
                            REPLACE(
                                       REPLACE(REPLACE(JSON_VALUE(c.value, N'$.Value'), N'\', N'\\'), N'%', N'\%'),
                                       N'_',
                                       N'\_'
                                   ),
                            @Q,
                            @Q + @Q
                        ) + N'%' + @Q + N' ESCAPE N' + @Q + N'\' + @Q
           WHEN N'NotContains' THEN
               QUOTENAME(vc.ColName) + N' NOT LIKE N' + @Q + N'%'
               + REPLACE(
                            REPLACE(
                                       REPLACE(REPLACE(JSON_VALUE(c.value, N'$.Value'), N'\', N'\\'), N'%', N'\%'),
                                       N'_',
                                       N'\_'
                                   ),
                            @Q,
                            @Q + @Q
                        ) + N'%' + @Q + N' ESCAPE N' + @Q + N'\' + @Q
           WHEN N'StartsWith' THEN
               QUOTENAME(vc.ColName) + N' LIKE N' + @Q
               + REPLACE(
                            REPLACE(
                                       REPLACE(REPLACE(JSON_VALUE(c.value, N'$.Value'), N'\', N'\\'), N'%', N'\%'),
                                       N'_',
                                       N'\_'
                                   ),
                            @Q,
                            @Q + @Q
                        ) + N'%' + @Q + N' ESCAPE N' + @Q + N'\' + @Q
           WHEN N'EndsWith' THEN
               QUOTENAME(vc.ColName) + N' LIKE N' + @Q + N'%'
               + REPLACE(
                            REPLACE(
                                       REPLACE(REPLACE(JSON_VALUE(c.value, N'$.Value'), N'\', N'\\'), N'%', N'\%'),
                                       N'_',
                                       N'\_'
                                   ),
                            @Q,
                            @Q + @Q
                        ) + @Q + N' ESCAPE N' + @Q + N'\' + @Q
           WHEN N'LessThan' THEN
               QUOTENAME(vc.ColName) + N' < N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
           WHEN N'GreaterThan' THEN
               QUOTENAME(vc.ColName) + N' > N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
           WHEN N'LessThanOrEqualTo' THEN
               QUOTENAME(vc.ColName) + N' <= N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
           WHEN N'GreaterThanOrEqualTo' THEN
               QUOTENAME(vc.ColName) + N' >= N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
           WHEN N'Between' THEN
               QUOTENAME(vc.ColName) + N' BETWEEN N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.Value'), @Q, @Q + @Q) + @Q
               + N' AND N' + @Q + REPLACE(JSON_VALUE(c.value, N'$.ValueTo'), @Q, @Q + @Q) + @Q
           ELSE
               NULL
       END
FROM OPENJSON(@Conditions) g
    CROSS APPLY OPENJSON(JSON_QUERY(g.value, N'$.Conditions')) c
    JOIN #ValidColumns vc
        ON vc.ColName = JSON_VALUE(c.value, N'$.ColumnName')
WHERE JSON_VALUE(c.value, N'$.Operator') IN ( N'Equals', N'NotEquals', N'Contains', N'NotContains', N'StartsWith',
                                              N'EndsWith', N'LessThan', N'GreaterThan', N'LessThanOrEqualTo',
                                              N'GreaterThanOrEqualTo', N'Between'
                                            )
      AND
      (
          JSON_VALUE(c.value, N'$.Value') IS NOT NULL
          OR JSON_VALUE(c.value, N'$.Operator') IN ( N'Equals', N'NotEquals' )
      )
      AND
      (
          JSON_VALUE(c.value, N'$.Operator') <> N'Between'
          OR JSON_VALUE(c.value, N'$.ValueTo') IS NOT NULL
      );

DELETE FROM #CondFragments
WHERE Fragment IS NULL;

SET @WhereFrag = N'';

IF EXISTS (SELECT 1 FROM #CondFragments)
BEGIN
    DECLARE @gOrd INT =
            (
                SELECT MIN(GrpOrd)FROM #CondFragments
            );
    DECLARE @gMax INT =
            (
                SELECT MAX(GrpOrd)FROM #CondFragments
            );
    DECLARE @gLogic NVARCHAR(20);
    DECLARE @iLogic NVARCHAR(5);
    DECLARE @inner NVARCHAR(MAX);
    DECLARE @firstGrp BIT = 1;
    DECLARE @conn NVARCHAR(5);

    WHILE @gOrd <= @gMax
    BEGIN
        IF EXISTS (SELECT 1 FROM #CondFragments WHERE GrpOrd = @gOrd)
        BEGIN
            SELECT TOP 1
                   @gLogic = GrpLogic
            FROM #CondFragments
            WHERE GrpOrd = @gOrd;
            SET @gLogic = UPPER(LTRIM(RTRIM(ISNULL(@gLogic, N'AND'))));
            IF @gLogic NOT IN ( N'AND', N'OR', N'AND NOT', N'OR NOT' )
                SET @gLogic = N'AND';

            SET @iLogic = CASE
                              WHEN @gLogic LIKE N'OR%' THEN
                                  N'OR'
                              ELSE
                                  N'AND'
                          END;

            SET @inner
                = STUFF(
                           (
                               SELECT N' ' + @iLogic + N' ' + cf.Fragment
                               FROM #CondFragments cf
                               WHERE cf.GrpOrd = @gOrd
                               ORDER BY cf.CondOrd
                               FOR XML PATH(N''), TYPE
                           ).value(N'.', N'NVARCHAR(MAX)'),
                           1,
                           LEN(@iLogic) + 2,
                           N''
                       );

            IF ISNULL(@inner, N'') <> N''
            BEGIN
                IF @firstGrp = 1
                BEGIN
                    SET @WhereFrag = CASE
                                         WHEN @gLogic LIKE N'%NOT' THEN
                                             N'NOT '
                                         ELSE
                                             N''
                                     END + N'(' + @inner + N')';
                    SET @firstGrp = 0;
                END;
                ELSE
                BEGIN
                    SET @conn = CASE
                                    WHEN @ConditionLogic LIKE N'OR%' THEN
                                        N'OR'
                                    ELSE
                                        N'AND'
                                END;
                    SET @WhereFrag = @WhereFrag + N' ' + @conn + CASE
                                                                     WHEN @gLogic LIKE N'%NOT' THEN
                                                                         N' NOT'
                                                                     ELSE
                                                                         N''
                                                                 END + N' (' + @inner + N')';
                END;
            END;
        END;
        SET @gOrd += 1;
    END;
END;

SET @WhereClause = CASE
                       WHEN ISNULL(@WhereFrag, N'') = N'' THEN
                           N''
                       ELSE
                           N'WHERE ' + @WhereFrag
                   END;

/* ── Build GROUP BY levels ───────────────────────────────────── */
CREATE TABLE #GroupLevels
(
    LevelNum INT NOT NULL PRIMARY KEY,
    ColName NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
    ColQuoted NVARCHAR(132) COLLATE DATABASE_DEFAULT NOT NULL,
    SortDir NVARCHAR(4) COLLATE DATABASE_DEFAULT NOT NULL
);

IF ISNULL(@GroupByColumns, N'') NOT IN ( N'', N'[]' )
    INSERT INTO #GroupLevels
    (
        LevelNum,
        ColName,
        ColQuoted,
        SortDir
    )
    SELECT CAST(j.[key] AS INT) + 1,
           vc.ColName,
           QUOTENAME(vc.ColName),
           CASE
               WHEN UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(j.value, N'$.SortOrder'), N'')))) IN ( N'DESC', N'DESCENDING' ) THEN
                   N'DESC'
               ELSE
                   N'ASC'
           END
    FROM OPENJSON(@GroupByColumns) j
        JOIN #ValidColumns vc
            ON vc.ColName = JSON_VALUE(j.value, N'$.ColumnName')
    ORDER BY CAST(j.[key] AS INT);

DECLARE @GroupCount INT =
        (
            SELECT COUNT(*)FROM #GroupLevels
        );

/* ── Build ORDER BY ──────────────────────────────────────────── */
DECLARE @OrderParts NVARCHAR(MAX) = N'';

IF ISNULL(@SortColumns, N'') NOT IN ( N'', N'[]' )
    SELECT @OrderParts
        = STUFF(
    (
        SELECT N', ' + QUOTENAME(vc.ColName) + N' '
               + CASE
                     WHEN UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(j.value, N'$.SortOrder'), N'')))) IN ( N'DESC',
                                                                                                     N'DESCENDING'
                                                                                                   ) THEN
                         N'DESC'
                     ELSE
                         N'ASC'
                 END
        FROM OPENJSON(@SortColumns) j
            JOIN #ValidColumns vc
                ON vc.ColName = JSON_VALUE(j.value, N'$.ColumnName')
        ORDER BY CAST(j.[key] AS INT)
        FOR XML PATH(N''), TYPE
    ).value(N'.', N'NVARCHAR(MAX)'),
    1   ,
    2   ,
    N''
               );

IF ISNULL(@OrderParts, N'') = N''
    SET @OrderParts = N'(SELECT NULL)';
DECLARE @OrderByClause NVARCHAR(MAX) = N'ORDER BY ' + @OrderParts;

/* ── Execute ─────────────────────────────────────────────────── */
DECLARE @TotalRows INT = 0;
DECLARE @TotalPages INT = 0;
DECLARE @ModelsJSON NVARCHAR(MAX) = N'[]';
DECLARE @HierarchySQL NVARCHAR(MAX) = N'';

IF @GroupCount = 0
BEGIN
    /* ── Flat path ────────────────────────────────────────────── */
    DECLARE @FlatCountSQL NVARCHAR(MAX) = N'SELECT @Total = COUNT(*) FROM ' + @FromSource + N' ' + @WhereClause;

    DECLARE @FlatDataSQL NVARCHAR(MAX)
        = N'SET @Result = (SELECT ' + @SelectList + N' FROM ' + @FromSource + N' ' + @WhereClause + N' '
          + @OrderByClause + N' OFFSET ' + @OfsStr + N' ROWS FETCH NEXT ' + @PSz + N' ROWS ONLY'
          + N' FOR JSON PATH, INCLUDE_NULL_VALUES)';

    IF @IsSP = 1
    BEGIN
        DECLARE @SPFlatSQL NVARCHAR(MAX)
            = N'IF OBJECT_ID(N''tempdb..' + @TmpTbl + N''') IS NOT NULL DROP TABLE ' + @TmpTbl + N';' + CHAR(10)
              + N'CREATE TABLE ' + @TmpTbl + N' (' + @ColDefs + N');' + CHAR(10) + N'INSERT INTO ' + @TmpTbl
              + N' EXEC ' + @QObj + CASE
                                        WHEN ISNULL(@ExecParamStr, N'') <> N'' THEN
                                            N' ' + @ExecParamStr
                                        ELSE
                                            N''
                                    END + N';' + CHAR(10) + N'SELECT @Total = COUNT(*) FROM ' + @TmpTbl + N' '
              + @WhereClause + N';' + CHAR(10) + N'SET @Result = (SELECT ' + @SelectList + N' FROM ' + @TmpTbl + N' '
              + @WhereClause + N' ' + @OrderByClause + N' OFFSET ' + @OfsStr + N' ROWS FETCH NEXT ' + @PSz
              + N' ROWS ONLY' + N' FOR JSON PATH, INCLUDE_NULL_VALUES);' + CHAR(10) + N'DROP TABLE ' + @TmpTbl + N';';

        EXEC sp_executesql @SPFlatSQL,
                           N'@Total INT OUTPUT, @Result NVARCHAR(MAX) OUTPUT',
                           @Total = @TotalRows OUTPUT,
                           @Result = @ModelsJSON OUTPUT;
    END;
    ELSE
    BEGIN
        EXEC sp_executesql @FlatCountSQL,
                           N'@Total INT OUTPUT',
                           @Total = @TotalRows OUTPUT;
        EXEC sp_executesql @FlatDataSQL,
                           N'@Result NVARCHAR(MAX) OUTPUT',
                           @Result = @ModelsJSON OUTPUT;
    END;
END;
ELSE
BEGIN
    /* ══════════════════════════════════════════════════════════
       Inlined: usp_QueryForge_ExecGrouped
       ══════════════════════════════════════════════════════════ */
    DECLARE @LvlIdx INT;
    DECLARE @LvlColQ NVARCHAR(132);
    DECLARE @LvlDir NVARCHAR(4);
    DECLARE @LvlAlias NVARCHAR(10);
    DECLARE @LvlDistinctSQL NVARCHAR(MAX);
    DECLARE @LvlOuterCond NVARCHAR(MAX);
    DECLARE @LvlOuterWhere NVARCHAR(MAX);
    DECLARE @LvlFullCond NVARCHAR(MAX);
    DECLARE @LvlCountWhere NVARCHAR(MAX);
    DECLARE @CountExpr NVARCHAR(MAX);
    DECLARE @innerLvl INT;
    DECLARE @innerColQ NVARCHAR(132);

    DECLARE @LeafCond NVARCHAR(MAX) = N'';
    SET @innerLvl = 1;
    WHILE @innerLvl <= @GroupCount
    BEGIN
        SELECT @innerColQ = ColQuoted
        FROM #GroupLevels
        WHERE LevelNum = @innerLvl;
        SET @LeafCond += N' AND (' + @innerColQ + N' = d' + CAST(@innerLvl AS NVARCHAR(4)) + N'.' + @innerColQ
                         + N' OR (' + @innerColQ + N' IS NULL AND d' + CAST(@innerLvl AS NVARCHAR(4)) + N'.'
                         + @innerColQ + N' IS NULL))';
        SET @innerLvl += 1;
    END;

    DECLARE @LeafWhere NVARCHAR(MAX);
    IF @WhereFrag = N''
        SET @LeafWhere = CASE
                             WHEN @LeafCond = N'' THEN
                                 N''
                             ELSE
                                 N'WHERE ' + STUFF(@LeafCond, 1, 5, N'')
                         END;
    ELSE
        SET @LeafWhere = N'WHERE (' + @WhereFrag + N')' + @LeafCond;

    SET @HierarchySQL
        = N'SELECT TOP 2147483647 ' + @SelectList + N' FROM ' + @FromSource + N' ' + @LeafWhere + N' ' + @OrderByClause
          + N' FOR JSON PATH, INCLUDE_NULL_VALUES';

    SET @LvlIdx = @GroupCount;
    WHILE @LvlIdx >= 1
    BEGIN
        SELECT @LvlColQ = ColQuoted,
               @LvlDir = SortDir
        FROM #GroupLevels
        WHERE LevelNum = @LvlIdx;
        SET @LvlAlias = N'd' + CAST(@LvlIdx AS NVARCHAR(4));

        SET @LvlOuterCond = N'';
        SET @innerLvl = 1;
        WHILE @innerLvl < @LvlIdx
        BEGIN
            SELECT @innerColQ = ColQuoted
            FROM #GroupLevels
            WHERE LevelNum = @innerLvl;
            SET @LvlOuterCond += N' AND (' + @innerColQ + N' = d' + CAST(@innerLvl AS NVARCHAR(4)) + N'.' + @innerColQ
                                 + N' OR (' + @innerColQ + N' IS NULL AND d' + CAST(@innerLvl AS NVARCHAR(4)) + N'.'
                                 + @innerColQ + N' IS NULL))';
            SET @innerLvl += 1;
        END;

        IF @WhereFrag = N''
            SET @LvlOuterWhere = CASE
                                     WHEN @LvlOuterCond = N'' THEN
                                         N''
                                     ELSE
                                         N'WHERE ' + STUFF(@LvlOuterCond, 1, 5, N'')
                                 END;
        ELSE
            SET @LvlOuterWhere = N'WHERE (' + @WhereFrag + N')' + @LvlOuterCond;

        SET @LvlFullCond
            = @LvlOuterCond + N' AND (' + @LvlColQ + N' = ' + @LvlAlias + N'.' + @LvlColQ + N' OR (' + @LvlColQ
              + N' IS NULL AND ' + @LvlAlias + N'.' + @LvlColQ + N' IS NULL))';

        IF @WhereFrag = N''
            SET @LvlCountWhere = CASE
                                     WHEN @LvlFullCond = N'' THEN
                                         N''
                                     ELSE
                                         N'WHERE ' + STUFF(@LvlFullCond, 1, 5, N'')
                                 END;
        ELSE
            SET @LvlCountWhere = N'WHERE (' + @WhereFrag + N')' + @LvlFullCond;

        SET @CountExpr = N'(SELECT COUNT(*) FROM ' + @FromSource + N' ' + @LvlCountWhere + N')';

        IF @LvlIdx = 1
        BEGIN
            SET @LvlDistinctSQL
                = N'SELECT DISTINCT ' + @LvlColQ + N' FROM ' + @FromSource + N' ' + @LvlOuterWhere + N' ORDER BY '
                  + @LvlColQ + N' ' + @LvlDir + N' OFFSET ' + @OfsStr + N' ROWS FETCH NEXT ' + @PSz + N' ROWS ONLY';

            SET @HierarchySQL
                = N'SELECT d1.' + @LvlColQ + N' AS [key],' + CHAR(10) + N'       ' + @CountExpr + N' AS [count],'
                  + CHAR(10) + N'       JSON_QUERY(ISNULL((' + @HierarchySQL + N'), N' + @Q + N'[]' + @Q
                  + N')) AS items' + CHAR(10) + N'FROM  (' + @LvlDistinctSQL + N') AS d1' + CHAR(10) + N'ORDER BY d1.'
                  + @LvlColQ + N' ' + @LvlDir + CHAR(10) + N'FOR JSON PATH, INCLUDE_NULL_VALUES';
        END;
        ELSE
        BEGIN
            SET @LvlDistinctSQL = N'SELECT DISTINCT ' + @LvlColQ + N' FROM ' + @FromSource + N' ' + @LvlOuterWhere;

            SET @HierarchySQL
                = N'SELECT ' + @LvlAlias + N'.' + @LvlColQ + N' AS [key],' + CHAR(10) + N'       ' + @CountExpr
                  + N' AS [count],' + CHAR(10) + N'       JSON_QUERY(ISNULL((' + @HierarchySQL + N'), N' + @Q + N'[]'
                  + @Q + N')) AS items' + CHAR(10) + N'FROM   (VALUES(0)) AS __anchor(v)' + CHAR(10) + N'CROSS APPLY ('
                  + @LvlDistinctSQL + N') AS ' + @LvlAlias + CHAR(10) + N'ORDER BY ' + @LvlAlias + N'.' + @LvlColQ
                  + N' ' + @LvlDir + CHAR(10) + N'FOR JSON PATH, INCLUDE_NULL_VALUES';
        END;

        SET @LvlIdx -= 1;
    END;

    DECLARE @TopLvlColQ NVARCHAR(132);
    SELECT @TopLvlColQ = ColQuoted
    FROM #GroupLevels
    WHERE LevelNum = 1;

    DECLARE @GrpCountSQL NVARCHAR(MAX)
        = N'SELECT @Total = COUNT(*) FROM (SELECT DISTINCT ' + @TopLvlColQ + N' FROM ' + @FromSource + N' '
          + @WhereClause + N') AS __GC';

    DECLARE @GrpDataSQL NVARCHAR(MAX) = N'SET @Result = (' + @HierarchySQL + N')';

    IF @IsSP = 1
    BEGIN
        DECLARE @SPGrpSQL NVARCHAR(MAX)
            = N'IF OBJECT_ID(N''tempdb..' + @TmpTbl + N''') IS NOT NULL DROP TABLE ' + @TmpTbl + N';' + CHAR(10)
              + N'CREATE TABLE ' + @TmpTbl + N' (' + @ColDefs + N');' + CHAR(10) + N'INSERT INTO ' + @TmpTbl
              + N' EXEC ' + @QObj + CASE
                                        WHEN ISNULL(@ExecParamStr, N'') <> N'' THEN
                                            N' ' + @ExecParamStr
                                        ELSE
                                            N''
                                    END + N';' + CHAR(10) + N'SELECT @Total = COUNT(*) FROM (SELECT DISTINCT '
              + @TopLvlColQ + N' FROM ' + @TmpTbl + N' ' + @WhereClause + N') AS __GC;' + CHAR(10) + N'SET @Result = ('
              + @HierarchySQL + N');' + CHAR(10) + N'DROP TABLE ' + @TmpTbl + N';';

        EXEC sp_executesql @SPGrpSQL,
                           N'@Total INT OUTPUT, @Result NVARCHAR(MAX) OUTPUT',
                           @Total = @TotalRows OUTPUT,
                           @Result = @ModelsJSON OUTPUT;
    END;
    ELSE
    BEGIN
        EXEC sp_executesql @GrpCountSQL,
                           N'@Total INT OUTPUT',
                           @Total = @TotalRows OUTPUT;
        EXEC sp_executesql @GrpDataSQL,
                           N'@Result NVARCHAR(MAX) OUTPUT',
                           @Result = @ModelsJSON OUTPUT;
    END;
END;

/* ── Final output ────────────────────────────────────────────── */
SET @TotalRows = ISNULL(@TotalRows, 0);
SET @TotalPages = CASE
                      WHEN @TotalRows = 0
                           OR @PageSize = 0 THEN
                          0
                      ELSE
                          CEILING(@TotalRows * 1.0 / @PageSize)
                  END;
SET @ModelsJSON = ISNULL(@ModelsJSON, N'[]');

DECLARE @ElapsedMs INT = DATEDIFF(MILLISECOND, @StartTime, SYSDATETIME());

IF @DebugMode = 1
BEGIN
    DECLARE @colList NVARCHAR(MAX) = N'';
    SELECT @colList += N'  [' + ColName + N'] ' + ISNULL(ColType, N'?') + N'  |'
    FROM #ValidColumns
    ORDER BY ColOrdinal;

    DECLARE @grpList NVARCHAR(MAX) = N'';
    SELECT @grpList += N'  L' + CAST(LevelNum AS NVARCHAR) + N':' + ColName + N' ' + SortDir + N'  |'
    FROM #GroupLevels
    ORDER BY LevelNum;

    PRINT N'';
    PRINT N'╔══════════════════════════════════════════════════════════════════╗';
    PRINT N'║  QueryForge  DEBUG REPORT (Ad-Hoc)                              ║';
    PRINT N'╠══════════════════════════════════════════════════════════════════╣';
    PRINT N'║  INPUTS';
    PRINT N'║  @Object (raw)      : ' + ISNULL(@Object, N'NULL');
    PRINT N'║    → Schema         : ' + @ObjectSchemaName;
    PRINT N'║    → Name           : ' + @ObjectName;
    PRINT N'║    → Type           : ' + CAST(@DapperObjectType AS NVARCHAR) + N'  (resolved kind=' + @ObjectKind + N')';
    PRINT N'║    → Parameters     : ' + ISNULL(@ObjectParameters, N'{}');
    PRINT N'║  @SelectColumns     : ' + ISNULL(@SelectColumns, N'NULL');
    PRINT N'║  @Criteria (raw)    : ' + ISNULL(@Criteria, N'NULL');
    PRINT N'║    → Logic          : ' + @ConditionLogic;
    PRINT N'║    → Groups         : ' + ISNULL(@Conditions, N'[]');
    PRINT N'║  @SortColumns       : ' + ISNULL(@SortColumns, N'NULL');
    PRINT N'║  @GroupByColumns    : ' + ISNULL(@GroupByColumns, N'NULL');
    PRINT N'║  @Paging (raw)      : ' + ISNULL(@Paging, N'NULL');
    PRINT N'║    → Size           : ' + @PSz;
    PRINT N'║    → Number         : ' + @PNum;
    PRINT N'╠══════════════════════════════════════════════════════════════════╣';
    PRINT N'║  RESOLVED OBJECT';
    PRINT N'║  QualifiedName      : ' + @QObj;
    PRINT N'║  FromSource         : ' + @FromSource;
    IF @IsSP = 1
    BEGIN
        PRINT N'║  DescStmt           : ' + ISNULL(@DescStmt, N'');
        PRINT N'║  ExecParamStr       : ' + ISNULL(@ExecParamStr, N'<none>');
        PRINT N'║  ColDefs            : ' + ISNULL(@ColDefs, N'');
    END;
    IF @ObjectKind IN ( N'IF', N'TF', N'FT' )
        PRINT N'║  TVF params call    : ' + ISNULL(@ExecParamStr, N'<none>');
    PRINT N'╠══════════════════════════════════════════════════════════════════╣';
    PRINT N'║  COLUMN CATALOGUE';
    PRINT N'║  ' + @colList;
    PRINT N'╠══════════════════════════════════════════════════════════════════╣';
    PRINT N'║  BUILT QUERY PARTS';
    PRINT N'║  SelectList         : ' + @SelectList;
    PRINT N'║  WhereFrag          : ' + ISNULL(@WhereFrag, N'<empty>');
    PRINT N'║  WhereClause        : ' + ISNULL(@WhereClause, N'<empty>');
    PRINT N'║  OrderByClause      : ' + @OrderByClause;
    PRINT N'║  GroupCount         : ' + CAST(@GroupCount AS NVARCHAR);
    PRINT N'║  Offset             : ' + @OfsStr;
    IF @GroupCount > 0
    BEGIN
        PRINT N'╠══════════════════════════════════════════════════════════════════╣';
        PRINT N'║  GROUP LEVELS';
        PRINT N'║  ' + @grpList;
        PRINT N'╠══════════════════════════════════════════════════════════════════╣';
        PRINT N'║  HIERARCHY SQL';
        DECLARE @hPos INT = 1;
        WHILE @hPos <= LEN(@HierarchySQL)
        BEGIN
            PRINT SUBSTRING(@HierarchySQL, @hPos, 3800);
            SET @hPos += 3800;
        END;
    END;
    PRINT N'╠══════════════════════════════════════════════════════════════════╣';
    PRINT N'║  RESULTS';
    PRINT N'║  Total.Rows         : ' + CAST(@TotalRows AS NVARCHAR);
    PRINT N'║  Total.Pages        : ' + CAST(@TotalPages AS NVARCHAR);
    PRINT N'║  Type             : ' + (CASE
                                           WHEN @GroupCount > 0 THEN
                                               'Grouped'
                                           ELSE
                                               'Flat'
                                       END
                                      );
    PRINT N'║  Elapsed (ms)       : ' + CAST(@ElapsedMs AS NVARCHAR);
    PRINT N'╚══════════════════════════════════════════════════════════════════╝';
END;

SELECT JSON_QUERY(   N'{""Total"":{""Rows"":' + CAST(@TotalRows AS NVARCHAR(20)) + N',""Pages"":'
                     + CAST(@TotalPages AS NVARCHAR(20)) + N'},""Type"":""' + CASE
                                                                               WHEN @GroupCount > 0 THEN
                                                                                   'Grouped'
                                                                               ELSE
                                                                                   'Flat'
                                                                           END + N'""}'
                 ) AS Meta,
       JSON_QUERY(@ModelsJSON) AS [Data];";
        }
    }
}