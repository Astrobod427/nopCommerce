using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Misc.RestApi.Api.Filters;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Plugin.Misc.RestApi.Factories;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.RestApi.Api.Controllers;

/// <summary>
/// Storefront: store information and catalog, as a customer sees them (ACL, store mapping, prices)
/// </summary>
[Route(RestApiDefaults.ROUTE_PREFIX)]
[ApiAuthorize(ApiTokenKind.Storefront)]
public class StorefrontCatalogController : BaseApiController
{
    #region Fields

    protected readonly ApiModelFactory _apiModelFactory;
    protected readonly IAclService _aclService;
    protected readonly ICategoryService _categoryService;
    protected readonly ICountryService _countryService;
    protected readonly ICurrencyService _currencyService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductService _productService;
    protected readonly IStateProvinceService _stateProvinceService;
    protected readonly IStoreContext _storeContext;
    protected readonly IStoreMappingService _storeMappingService;
    protected readonly IWorkContext _workContext;
    protected readonly OrderSettings _orderSettings;
    protected readonly RestApiSettings _settings;
    protected readonly Nop.Core.Domain.StoreInformationSettings _storeInformationSettings;

    #endregion

    #region Ctor

    public StorefrontCatalogController(ApiModelFactory apiModelFactory,
        IAclService aclService,
        ICategoryService categoryService,
        ICountryService countryService,
        ICurrencyService currencyService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IProductService productService,
        IStateProvinceService stateProvinceService,
        IStoreContext storeContext,
        IStoreMappingService storeMappingService,
        IWorkContext workContext,
        OrderSettings orderSettings,
        RestApiSettings settings,
        Nop.Core.Domain.StoreInformationSettings storeInformationSettings)
    {
        _apiModelFactory = apiModelFactory;
        _aclService = aclService;
        _categoryService = categoryService;
        _countryService = countryService;
        _currencyService = currencyService;
        _languageService = languageService;
        _localizationService = localizationService;
        _pictureService = pictureService;
        _productService = productService;
        _stateProvinceService = stateProvinceService;
        _storeContext = storeContext;
        _storeMappingService = storeMappingService;
        _workContext = workContext;
        _orderSettings = orderSettings;
        _settings = settings;
        _storeInformationSettings = storeInformationSettings;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Same visibility rules as the product page of the web store
    /// </summary>
    protected virtual async Task<bool> IsProductVisibleAsync(Product product)
    {
        return product is { Deleted: false, Published: true }
            && await _aclService.AuthorizeAsync(product)
            && await _storeMappingService.AuthorizeAsync(product)
            && _productService.ProductIsAvailable(product);
    }

    #endregion

    #region Store

    /// <summary>
    /// Store information, languages, currencies and app theme (send X-Language-Id / X-Currency-Id to switch)
    /// </summary>
    [HttpGet("store")]
    public virtual async Task<IActionResult> Store()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();
        var currency = await _workContext.GetWorkingCurrencyAsync();

        var logoPictureId = _settings.AppLogoPictureId > 0 ? _settings.AppLogoPictureId : _storeInformationSettings.LogoPictureId;
        var logoUrl = logoPictureId > 0 ? await _pictureService.GetPictureUrlAsync(logoPictureId, showDefaultPicture: false) : null;

        return ApiOk(new StoreInfoDto
        {
            Id = store.Id,
            Name = await _localizationService.GetLocalizedAsync(store, s => s.Name),
            Url = store.Url,
            PricesIncludeTax = await _workContext.GetTaxDisplayTypeAsync() == TaxDisplayType.IncludingTax,
            AnonymousCheckoutAllowed = _orderSettings.AnonymousCheckoutAllowed,
            WorkingLanguage = new LanguageDto(language.Id, language.Name, language.LanguageCulture, language.Rtl),
            WorkingCurrency = new CurrencyDto(currency.Id, await _localizationService.GetLocalizedAsync(currency, c => c.Name), currency.CurrencyCode, currency.Rate),
            Languages = (await _languageService.GetAllLanguagesAsync(storeId: store.Id))
                .Select(l => new LanguageDto(l.Id, l.Name, l.LanguageCulture, l.Rtl)).ToList(),
            Currencies = await (await _currencyService.GetAllCurrenciesAsync(storeId: store.Id))
                .SelectAwait(async c => new CurrencyDto(c.Id, await _localizationService.GetLocalizedAsync(c, x => x.Name), c.CurrencyCode, c.Rate)).ToListAsync(),
            Theme = new AppThemeDto(_settings.AppPrimaryColor, _settings.AppSecondaryColor, logoUrl)
        });
    }

    /// <summary>
    /// Countries available for addresses
    /// </summary>
    [HttpGet("countries")]
    public virtual async Task<IActionResult> Countries()
    {
        var language = await _workContext.GetWorkingLanguageAsync();
        var countries = await _countryService.GetAllCountriesAsync(language.Id);

        return ApiOk(await countries.SelectAwait(async c => new CountryDto(c.Id, await _localizationService.GetLocalizedAsync(c, x => x.Name),
            c.TwoLetterIsoCode, c.AllowsShipping, c.AllowsBilling)).ToListAsync());
    }

    /// <summary>
    /// States/provinces of a country (required in addresses of countries that have some, e.g. Swiss cantons)
    /// </summary>
    [HttpGet("countries/{id:int}/states")]
    public virtual async Task<IActionResult> States(int id)
    {
        var country = await _countryService.GetCountryByIdAsync(id);
        if (country is not { Published: true })
            return ApiNotFound("Country");

        var language = await _workContext.GetWorkingLanguageAsync();
        var states = await _stateProvinceService.GetStateProvincesByCountryIdAsync(country.Id, language.Id);

        return ApiOk(await states.SelectAwait(async s => new StateProvinceDto(s.Id, await _localizationService.GetLocalizedAsync(s, x => x.Name),
            s.Abbreviation)).ToListAsync());
    }

    #endregion

    #region Catalog

    /// <summary>
    /// Categories visible to the customer (flat list; build the tree with parentCategoryId)
    /// </summary>
    [HttpGet("categories")]
    public virtual async Task<IActionResult> Categories()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var categories = await _categoryService.GetAllCategoriesAsync(store.Id);

        return ApiOk(await categories.SelectAwait(async c => await _apiModelFactory.PrepareCategoryAsync(c)).ToListAsync());
    }

    /// <summary>
    /// Searches products (by category and/or keywords) with the customer's prices
    /// </summary>
    [HttpGet("products")]
    public virtual async Task<IActionResult> Products([FromQuery] int? categoryId, [FromQuery] string q,
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 0, [FromQuery] ProductSortingEnum orderBy = ProductSortingEnum.Position)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();

        IList<int> categoryIds = null;
        if (categoryId is > 0)
        {
            var category = await _categoryService.GetCategoryByIdAsync(categoryId.Value);
            if (category == null || category.Deleted || !category.Published || !await _aclService.AuthorizeAsync(category) || !await _storeMappingService.AuthorizeAsync(category))
                return ApiNotFound("Category");

            categoryIds = [category.Id];
        }

        var products = await _productService.SearchProductsAsync(pageIndex: Math.Max(pageIndex, 0), pageSize: ClampPageSize(pageSize, _settings),
            categoryIds: categoryIds, storeId: store.Id, visibleIndividuallyOnly: true, keywords: string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            languageId: language.Id, orderBy: orderBy);

        return ApiOk(ToPaged(products, await _apiModelFactory.PrepareProductSummariesAsync(products)));
    }

    /// <summary>
    /// Product details with the customer's price
    /// </summary>
    [HttpGet("products/{id:int}")]
    public virtual async Task<IActionResult> Product(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || !await IsProductVisibleAsync(product))
            return ApiNotFound("Product");

        return ApiOk(await _apiModelFactory.PrepareProductDetailAsync(product));
    }

    #endregion
}
