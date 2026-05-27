using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceApp.Infrastructure.Data;

/// <summary>
/// One-time, self-disabling cutover from the old single-tenant schema to the
/// multi-tenant one. The <c>AddTenancyAndIdentity</c> migration adds non-nullable
/// <c>CompanyId</c> foreign keys that cannot apply on top of existing single-tenant
/// data (existing rows would reference a non-existent company), which is why the
/// production app failed to start with HTTP 500.30.
///
/// <para>If we detect that schema (the tenancy migration is still pending), we drop
/// the entire <c>blacktech</c> schema so <see cref="RelationalDatabaseFacadeExtensions.Migrate"/>
/// can rebuild it from scratch — this is the agreed "start fresh" data wipe. Once the
/// tenancy migration is applied this method returns immediately and never drops
/// anything again, so subsequent deploys leave real multi-tenant data untouched.</para>
/// </summary>
public static class LegacySchemaReset
{
    private const string TenancyMigration = "AddTenancyAndIdentity";

    public static void ResetIfLegacySchemaPresent(AppDbContext db, ILogger logger)
    {
        // Brand-new or unreachable database: nothing to reset; Migrate() will create it.
        if (!db.Database.CanConnect())
            return;

        var alreadyMultiTenant = db.Database.GetAppliedMigrations()
            .Any(m => m.EndsWith(TenancyMigration, StringComparison.Ordinal));
        var tenancyPending = db.Database.GetPendingMigrations()
            .Any(m => m.EndsWith(TenancyMigration, StringComparison.Ordinal));

        // Already migrated, or this build predates the tenancy migration -> do nothing.
        if (alreadyMultiTenant || !tenancyPending)
            return;

        logger.LogWarning(
            "Legacy (pre-tenancy) schema detected. Dropping the 'blacktech' schema so the " +
            "multi-tenant migration can rebuild it from scratch. EXISTING DATA WILL BE REMOVED.");

        db.Database.ExecuteSqlRaw(DropAllBlacktechObjectsSql);

        logger.LogWarning("'blacktech' schema dropped; migrations will now recreate it fresh.");
    }

    // Drops every foreign key in the schema first, then every table (including
    // __EFMigrationsHistory). No-op on an empty schema.
    private const string DropAllBlacktechObjectsSql = @"
DECLARE @sql nvarchar(max) = N'';

SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name)
             + ' DROP CONSTRAINT ' + QUOTENAME(f.name) + ';' + CHAR(10)
FROM sys.foreign_keys f
JOIN sys.tables t  ON f.parent_object_id = t.object_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'blacktech';

SELECT @sql += N'DROP TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';' + CHAR(10)
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'blacktech';

IF LEN(@sql) > 0 EXEC sp_executesql @sql;";
}
