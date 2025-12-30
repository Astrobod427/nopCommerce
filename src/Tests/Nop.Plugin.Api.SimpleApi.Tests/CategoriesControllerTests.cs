using Microsoft.AspNetCore.Mvc;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Api.SimpleApi.Controllers;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Tests;

[TestFixture]
public class CategoriesControllerTests
{
    private Mock<ICategoryService> _categoryServiceMock;
    private CategoriesController _categoriesController;

    [SetUp]
    public void Setup()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _categoriesController = new CategoriesController(_categoryServiceMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _categoriesController.Dispose();
    }

    [Test]
    public async Task GetCategories_ShouldReturnOkResultWithCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Category 1" },
            new() { Id = 2, Name = "Category 2" }
        };

        _categoryServiceMock.Setup(x => x.GetAllCategoriesAsync(
            It.IsAny<int>(),
            It.IsAny<bool>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _categoriesController.GetCategories();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var returnedCategories = okResult.Value as List<CategoryDto>;
        Assert.That(returnedCategories, Is.Not.Null);
        Assert.That(returnedCategories.Count, Is.EqualTo(2));
    }
}
