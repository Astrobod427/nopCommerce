using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.RestApi.Domain;

namespace Nop.Plugin.Misc.RestApi.Data.Migrations;

[NopMigration("2026-10-26 12:00:00", "Nop.Plugin.Misc.RestApi schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        this.CreateTableIfNotExists<ApiClient>();
        this.CreateTableIfNotExists<ApiToken>();
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
        this.DeleteTableIfExists<ApiToken>();
        this.DeleteTableIfExists<ApiClient>();
    }
}
