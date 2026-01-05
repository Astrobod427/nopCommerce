using System;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Web.Framework.Controllers;
using System.Linq;
using Nop.Plugin.Api.SimpleApi.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/products")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class ProductsController : BasePluginController
    {
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, 
            IPictureService pictureService,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _pictureService = pictureService;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetProducts([FromQuery] int categoryId = 0, [FromQuery] int languageId = 0)
        {
            _logger.LogInformation($"SimpleAPI: GetProducts called with categoryId={categoryId}, languageId={languageId}");
            
            var categoryIds = categoryId > 0 ? new List<int> { categoryId } : null;
            
            // Debugging category filter
            if (categoryIds != null)
            {
                 _logger.LogInformation($"SimpleAPI: Filtering by categoryIds count: {categoryIds.Count} - First ID: {categoryIds[0]}");
            }
            else
            {
                 _logger.LogInformation("SimpleAPI: No category filter applied.");
            }

            var products = await _productService.SearchProductsAsync(categoryIds: categoryIds, languageId: languageId);
            _logger.LogInformation($"SimpleAPI: SearchProductsAsync returned {products.Count} products");

            var productDtos = new List<ProductDto>();

            foreach (var p in products)
            {
                // Get default product picture
                var pictures = await _pictureService.GetPicturesByProductIdAsync(p.Id, 1);
                var defaultPicture = pictures.FirstOrDefault();
                string imageUrl = null;
                if (defaultPicture != null)
                {
                    (imageUrl, _) = await _pictureService.GetPictureUrlAsync(defaultPicture);
                }

                productDtos.Add(new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ShortDescription = p.ShortDescription,
                    Price = p.Price,
                    ImageUrl = imageUrl
                });
            }

            return Ok(productDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Get all product pictures
            var pictures = await _pictureService.GetPicturesByProductIdAsync(product.Id);
            var images = new List<string>();
            string defaultImageUrl = null;

            if (pictures.Any())
            {
                foreach (var picture in pictures)
                {
                    var (url, _) = await _pictureService.GetPictureUrlAsync(picture);
                    images.Add(url);
                }
                defaultImageUrl = images.First();
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                FullDescription = product.FullDescription,
                Price = product.Price,
                ImageUrl = defaultImageUrl,
                Images = images
            };

            return Ok(productDto);
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto productDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = new Nop.Core.Domain.Catalog.Product
            {
                Name = productDto.Name,
                ShortDescription = productDto.ShortDescription,
                Price = productDto.Price,
                Published = productDto.Published,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            await _productService.InsertProductAsync(product);

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product.Id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto productDto)
        {
            if (id != productDto.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Name = productDto.Name;
            product.ShortDescription = productDto.ShortDescription;
            product.Price = productDto.Price;
            product.Published = productDto.Published;
            product.UpdatedOnUtc = DateTime.UtcNow;

            await _productService.UpdateProductAsync(product);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            await _productService.DeleteProductAsync(product);

            return NoContent();
        }
    }
}
