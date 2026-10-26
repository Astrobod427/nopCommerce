using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Customers;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.RestApi.Domain;

namespace Nop.Plugin.Misc.RestApi.Data.Mapping.Builders;

public class ApiClientBuilder : NopEntityBuilder<ApiClient>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ApiClient.Name)).AsString(200).NotNullable()
            .WithColumn(nameof(ApiClient.ClientId)).AsString(64).NotNullable().Unique()
            .WithColumn(nameof(ApiClient.SecretHash)).AsString(64).NotNullable()
            .WithColumn(nameof(ApiClient.CustomerId)).AsInt32().ForeignKey<Customer>();
    }
}
