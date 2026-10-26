using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Customers;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.RestApi.Domain;

namespace Nop.Plugin.Misc.RestApi.Data.Mapping.Builders;

public class ApiTokenBuilder : NopEntityBuilder<ApiToken>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ApiToken.TokenHash)).AsString(64).NotNullable().Unique()
            .WithColumn(nameof(ApiToken.CustomerId)).AsInt32().ForeignKey<Customer>()
            .WithColumn(nameof(ApiToken.ApiClientId)).AsInt32().Nullable().ForeignKey<ApiClient>()
            .WithColumn(nameof(ApiToken.IpAddress)).AsString(100).Nullable();
    }
}
