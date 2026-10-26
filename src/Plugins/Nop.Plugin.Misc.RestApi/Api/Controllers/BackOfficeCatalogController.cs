using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.RestApi.Api.Filters;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Plugin.Misc.RestApi.Factories;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Seo;

namespace Nop.Plugin.Misc.RestApi.Api.Controllers;

/// <summary>
/// Back office: products and categories, with the permissions of the admin area
/// (Catalog > Products / Categories, view or create-edit-delete)
/// </summary>
[Route(RestApiDefaults.ROUTE_PREFIX + "/admin")]
[ApiAuthorize(ApiTokenKind.BackOffice)]
public class BackOfficeCatalogController : BaseApiController
{
    #region Fields

    protected readonly BackOfficeModelFactory _backOfficeModelFactory;
    protected readonly ICategoryService _categoryService;
    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IProductService _productService;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly RestApiSettings _settings;

    #endregion

    #region Ctor

    public BackOfficeCatalogController(BackOfficeModelFactory backOfficeModelFactory,
        ICategoryService categoryService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IProductService productService,
        IUrlRecordService urlRecordService,
        RestApiSettings settings)
    {
        _backOfficeModelFactory = backOfficeModelFactory;
        _categoryService = categoryService;
        _customerActivityService = customerActivityService;
        _localizationService = localizationService;
        _productService = productService;
        _urlRecordService = urlRecordService;
        _settings = settings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<IList<string>> ValidateCategoryIdsAsync(IList<int> categoryIds)
    {
        var errors = new List<string>();
        foreach (var id in categoryIds?.Distinct() ?? [])
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null || category.Deleted)
                errors.Add($"Category {id} not found.");
        }

        return errors;
    }

    /// <summary>
    /// Makes the category mappings of a product match the given list
    /// </summary>
    protected virtual async Task SaveCategoryMappingsAsync(Product product, IList<int> categoryIds)
    {
        var existing = await _categoryService.GetProductCategoriesByProductIdAsync(product.Id, true);
        foreach (var mapping in existing.Where(pc => !categoryIds.Contains(pc.CategoryId)))
            await _categoryService.DeleteProductCategoryAsync(mapping);

        foreach (var categoryId in categoryIds.Distinct().Where(id => existing.All(pc => pc.CategoryId != id)))
        {
            await _categoryService.InsertProductCategoryAsync(new ProductCategory
            {
                ProductId = product.Id,
                CategoryId = categoryId,
                DisplayOrder = 1
            });
        }
    }

    protected virtual async Task<Product> GetProductAsync(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return product is { Deleted: false } ? product : null;
    }

    protected virtual async Task<Category> GetCategoryAsync(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        return category is { Deleted: false } ? category : null;
    }

    #endregion

    #region Products

    /// <summary>
    /// Searches products (published or not)
    /// </summary>
    [HttpGet("products")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.PRODUCTS_VIEW)]
    public virtual async Task<IActionResult> Products([FromQuery] string q, [FromQuery] int? categoryId,
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 0)
    {
        var products = await _productService.SearchProductsAsync(pageIndex: Math.Max(pageIndex, 0), pageSize: ClampPageSize(pageSize, _settings),
            categoryIds: categoryId is > 0 ? [categoryId.Value] : null,
            keywords: string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            orderBy: ProductSortingEnum.CreatedOn, showHidden: true);

        var items = new List<AdminProductDto>();
        foreach (var product in products)
            items.Add(await _backOfficeModelFactory.PrepareProductAsync(product));

        return ApiOk(ToPaged(products, items));
    }

    [HttpGet("products/{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.PRODUCTS_VIEW)]
    public virtual async Task<IActionResult> Product(int id)
    {
        var product = await GetProductAsync(id);
        if (product == null)
            return ApiNotFound("Product");

        return ApiOk(await _backOfficeModelFactory.PrepareProductAsync(product));
    }

    /// <summary>
    /// Creates a simple product with the defaults of the admin area
    /// </summary>
    [HttpPost("products")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> CreateProduct([FromBody] AdminProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Name))
            return ApiBadRequest("The product name is required.");

        var errors = await ValidateCategoryIdsAsync(request.CategoryIds);
        if (errors.Any())
            return ApiBadRequest("Invalid categories.", errors);

        var now = DateTime.UtcNow;
        var product = new Product
        {
            ProductType = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = request.Name.Trim(),
            ShortDescription = request.ShortDescription,
            FullDescription = request.FullDescription,
            Sku = request.Sku,
            Price = request.Price ?? 0,
            OldPrice = request.OldPrice ?? 0,
            StockQuantity = request.StockQuantity ?? 10000,
            NotifyAdminForQuantityBelow = 1,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 10000,
            IsShipEnabled = true,
            AllowCustomerReviews = true,
            Published = request.Published ?? true,
            CreatedOnUtc = now,
            UpdatedOnUtc = now
        };
        await _productService.InsertProductAsync(product);

        var seName = await _urlRecordService.ValidateSeNameAsync(product, null, product.Name, true);
        await _urlRecordService.SaveSlugAsync(product, seName, 0);

        if (request.CategoryIds != null)
            await SaveCategoryMappingsAsync(product, request.CategoryIds);

        await _customerActivityService.InsertActivityAsync("AddNewProduct",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.AddNewProduct"), product.Name), product);

        return ApiCreated(await _backOfficeModelFactory.PrepareProductAsync(product));
    }

    /// <summary>
    /// Updates a product; omitted properties are left unchanged
    /// </summary>
    [HttpPut("products/{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> UpdateProduct(int id, [FromBody] AdminProductRequest request)
    {
        var product = await GetProductAsync(id);
        if (product == null)
            return ApiNotFound("Product");

        if (request == null)
            return ApiBadRequest("A request body is required.");

        if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
            return ApiBadRequest("The product name cannot be empty.");

        var errors = await ValidateCategoryIdsAsync(request.CategoryIds);
        if (errors.Any())
            return ApiBadRequest("Invalid categories.", errors);

        if (request.Name != null)
            product.Name = request.Name.Trim();
        if (request.ShortDescription != null)
            product.ShortDescription = request.ShortDescription;
        if (request.FullDescription != null)
            product.FullDescription = request.FullDescription;
        if (request.Sku != null)
            product.Sku = request.Sku;
        if (request.Price.HasValue)
            product.Price = request.Price.Value;
        if (request.OldPrice.HasValue)
            product.OldPrice = request.OldPrice.Value;
        if (request.Published.HasValue)
            product.Published = request.Published.Value;
        var manageStock = product.ManageInventoryMethod == ManageInventoryMethod.ManageStock;
        if (request.StockQuantity.HasValue && !manageStock)
            product.StockQuantity = request.StockQuantity.Value;
        product.UpdatedOnUtc = DateTime.UtcNow;
        await _productService.UpdateProductAsync(product);

        //managed stock changes go through the inventory service so that they appear in the stock history
        if (request.StockQuantity.HasValue && manageStock && request.StockQuantity.Value != product.StockQuantity)
            await _productService.AdjustInventoryAsync(product, request.StockQuantity.Value - product.StockQuantity, message: "REST API");

        if (request.CategoryIds != null)
            await SaveCategoryMappingsAsync(product, request.CategoryIds);

        await _customerActivityService.InsertActivityAsync("EditProduct",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditProduct"), product.Name), product);

        return ApiOk(await _backOfficeModelFactory.PrepareProductAsync(await _productService.GetProductByIdAsync(product.Id)));
    }

    /// <summary>
    /// Deletes a product (soft delete, as in the admin area)
    /// </summary>
    [HttpDelete("products/{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await GetProductAsync(id);
        if (product == null)
            return ApiNotFound("Product");

        await _productService.DeleteProductAsync(product);
        await _customerActivityService.InsertActivityAsync("DeleteProduct",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.DeleteProduct"), product.Name), product);

        return NoContent();
    }

    #endregion

    #region Categories

    [HttpGet("categories")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.CATEGORIES_VIEW)]
    public virtual async Task<IActionResult> Categories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync(showHidden: true);

        return ApiOk(categories.Select(_backOfficeModelFactory.PrepareCategory).ToList());
    }

    [HttpGet("categories/{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.CATEGORIES_VIEW)]
    public virtual async Task<IActionResult> Category(int id)
    {
        var category = await GetCategoryAsync(id);
        if (category == null)
            return ApiNotFound("Category");

        return ApiOk(_backOfficeModelFactory.PrepareCategory(category));
    }

    [HttpPost("categories")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.CATEGORIES_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> CreateCategory([FromBody] AdminCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Name))
            return ApiBadRequest("The category name is required.");

        if (request.ParentCategoryId is > 0 && await GetCategoryAsync(request.ParentCategoryId.Value) == null)
            return ApiBadRequest($"Parent category {request.ParentCategoryId} not found.");

        var now = DateTime.UtcNow;
        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId ?? 0,
            DisplayOrder = request.DisplayOrder ?? 0,
            Published = request.Published ?? true,
            AllowCustomersToSelectPageSize = true,
            PageSize = 6,
            PageSizeOptions = "6, 3, 9",
            CreatedOnUtc = now,
            UpdatedOnUtc = now
        };
        await _categoryService.InsertCategoryAsync(category);

        var seName = await _urlRecordService.ValidateSeNameAsync(category, null, category.Name, true);
        await _urlRecordService.SaveSlugAsync(category, seName, 0);

        await _customerActivityService.InsertActivityAsync("AddNewCategory",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.AddNewCategory"), category.Name), category);

        return ApiCreated(_backOfficeModelFactory.PrepareCategory(category));
    }

    [HttpPut("categories/{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.CATEGORIES_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> UpdateCategory(int id, [FromBody] AdminCategoryRequest request)
    {
        var category = await GetCategoryAsync(id);
        if (category == null)
            return ApiNotFound("Category");

        if (request == null)
            return ApiBadRequest("A request body is required.");

        if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
            return ApiBadRequest("The category name cannot be empty.");

        if (request.ParentCategoryId is > 0)
        {
            if (request.ParentCategoryId == category.Id || await GetCategoryAsync(request.ParentCategoryId.Value) == null)
                return ApiBadRequest($"Invalid parent category {request.ParentCategoryId}.");

            //no loop in the tree
            var breadcrumb = await _categoryService.GetCategoryBreadCrumbAsync(await _categoryService.GetCategoryByIdAsync(request.ParentCategoryId.Value), showHidden: true);
            if (breadcrumb.Any(c => c.Id == category.Id))
                return ApiBadRequest("A category cannot be moved under one of its subcategories.");
        }

        if (request.Name != null)
            category.Name = request.Name.Trim();
        if (request.Description != null)
            category.Description = request.Description;
        if (request.ParentCategoryId.HasValue)
            category.ParentCategoryId = Math.Max(request.ParentCategoryId.Value, 0);
        if (request.DisplayOrder.HasValue)
            category.DisplayOrder = request.DisplayOrder.Value;
        if (request.Published.HasValue)
            category.Published = request.Published.Value;
        category.UpdatedOnUtc = DateTime.UtcNow;
        await _categoryService.UpdateCategoryAsync(category);

        await _customerActivityService.InsertActivityAsync("EditCategory",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditCategory"), category.Name), category);

        return ApiOk(_backOfficeModelFactory.PrepareCategory(category));
    }

    [HttpDelete("categories/{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Catalog.CATEGORIES_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await GetCategoryAsync(id);
        if (category == null)
            return ApiNotFound("Category");

        await _categoryService.DeleteCategoryAsync(category);
        await _customerActivityService.InsertActivityAsync("DeleteCategory",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.DeleteCategory"), category.Name), category);

        return NoContent();
    }

    #endregion
}
