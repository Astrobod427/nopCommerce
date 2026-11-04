using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Misc.RestApi.Data.Migrations;

[NopMigration("2026-10-30 12:00:00", "Misc.RestApi 5.00.2: IP restriction of API clients, protection settings", MigrationProcessType.Update)]
public class UpgradeTo50002 : Migration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        var table = NameCompatibilityManager.GetTableName(typeof(ApiClient));
        var column = nameof(ApiClient.AllowedIpAddresses);
        if (!Schema.Table(table).Column(column).Exists())
            Alter.Table(table).AddColumn(column).AsString(1000).Nullable();

        this.SetSettingIfNotExists<RestApiSettings, int>(settings => settings.RequestsPerMinute, 300);
        this.SetSettingIfNotExists<RestApiSettings, int>(settings => settings.AuthRequestsPerMinute, 10);
        this.SetSettingIfNotExists<RestApiSettings, string>(settings => settings.CorsAllowedOrigins, string.Empty);
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
        //nothing
    }
}
