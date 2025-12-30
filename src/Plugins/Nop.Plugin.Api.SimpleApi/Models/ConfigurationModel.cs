using Nop.Web.Framework.Models;

namespace Nop.Plugin.Api.SimpleApi.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public string ApiKey { get; set; }
    }
}
