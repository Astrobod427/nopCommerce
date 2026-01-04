using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Localization;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/languages")]
    [ApiController]
    public class LanguagesController : Nop.Web.Framework.Controllers.BasePluginController
    {
        private readonly ILanguageService _languageService;

        public LanguagesController(ILanguageService languageService)
        {
            _languageService = languageService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetLanguages()
        {
            var languages = await _languageService.GetAllLanguagesAsync();
            var languageDtos = languages.Select(l => new LanguageDto
            {
                Id = l.Id,
                Name = l.Name,
                LanguageCulture = l.LanguageCulture,
                UniqueSeoCode = l.UniqueSeoCode,
                FlagImageFileName = l.FlagImageFileName,
                Published = l.Published
            }).ToList();

            return Ok(languageDtos);
        }
    }
}
