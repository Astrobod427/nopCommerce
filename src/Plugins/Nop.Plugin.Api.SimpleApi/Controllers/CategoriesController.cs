using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Web.Framework.Controllers;
using System.Linq;
using Nop.Plugin.Api.SimpleApi.DTOs;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using System; // For DateTime.UtcNow

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/categories")]
    [ApiController]
    public class CategoriesController : BasePluginController
    {
        private readonly ICategoryService _categoryService;
        private readonly Nop.Services.Media.IPictureService _pictureService;

        public CategoriesController(ICategoryService categoryService, Nop.Services.Media.IPictureService pictureService)
        {
            _categoryService = categoryService;
            _pictureService = pictureService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var categoryDtos = new System.Collections.Generic.List<CategoryDto>();

            foreach (var c in categories)
            {
                var picture = await _pictureService.GetPictureByIdAsync(c.PictureId);
                string imageUrl = null;
                if (picture != null)
                {
                    (imageUrl, _) = await _pictureService.GetPictureUrlAsync(picture);
                }

                categoryDtos.Add(new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Published = c.Published,
                    DisplayOrder = c.DisplayOrder,
                    ImageUrl = imageUrl
                });
            }

            return Ok(categoryDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var picture = await _pictureService.GetPictureByIdAsync(category.PictureId);
            string imageUrl = null;
            if (picture != null)
            {
                (imageUrl, _) = await _pictureService.GetPictureUrlAsync(picture);
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Published = category.Published,
                DisplayOrder = category.DisplayOrder,
                ImageUrl = imageUrl
            };

            return Ok(categoryDto);
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto categoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = new Category
            {
                Name = categoryDto.Name,
                Description = categoryDto.Description,
                Published = categoryDto.Published,
                DisplayOrder = categoryDto.DisplayOrder,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            await _categoryService.InsertCategoryAsync(category);

            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category.Id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto categoryDto)
        {
            if (id != categoryDto.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            category.Name = categoryDto.Name;
            category.Description = categoryDto.Description;
            category.Published = categoryDto.Published;
            category.DisplayOrder = categoryDto.DisplayOrder;
            category.UpdatedOnUtc = DateTime.UtcNow;

            await _categoryService.UpdateCategoryAsync(category);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            await _categoryService.DeleteCategoryAsync(category);

            return NoContent();
        }
    }
}
