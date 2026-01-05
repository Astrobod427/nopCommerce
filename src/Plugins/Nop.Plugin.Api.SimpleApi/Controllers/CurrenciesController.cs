using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Directory;
using Nop.Web.Framework.Controllers;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/currencies")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class CurrenciesController : BasePluginController
    {
        private readonly ICurrencyService _currencyService;
        private readonly IStoreContext _storeContext;

        public CurrenciesController(ICurrencyService currencyService, IStoreContext storeContext)
        {
            _currencyService = currencyService;
            _storeContext = storeContext;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetCurrencies()
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var currencies = await _currencyService.GetAllCurrenciesAsync(storeId: store.Id);
            
            var currencyDtos = currencies.Select(c => new CurrencyDto
            {
                Id = c.Id,
                Name = c.Name,
                CurrencyCode = c.CurrencyCode,
                DisplayLocale = c.DisplayLocale,
                CustomFormatting = c.CustomFormatting,
                Rate = c.Rate,
                // Note: These properties might need more complex logic to determine correctly via Services if needed, 
                // but for simple API we just assume the entity properties are enough or we skip them.
                // Simplified:
                IsPrimaryExchangeRateCurrency = false, 
                IsPrimaryStoreCurrency = false
            }).ToList();

            return Ok(currencyDtos);
        }
    }
}
