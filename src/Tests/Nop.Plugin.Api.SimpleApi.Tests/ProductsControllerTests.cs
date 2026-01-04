using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Plugin.Api.SimpleApi.Controllers;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Catalog;
using Nop.Services.Media;

namespace Nop.Plugin.Api.SimpleApi.Tests;

[TestFixture]
public class ProductsControllerTests
{
    private Mock<IProductService> _productServiceMock;
    private Mock<IPictureService> _pictureServiceMock;
    private Mock<ILogger<ProductsController>> _loggerMock;
    private ProductsController _productsController;

    [SetUp]
    public void Setup()
    {
        _productServiceMock = new Mock<IProductService>();
        _pictureServiceMock = new Mock<IPictureService>();
        _loggerMock = new Mock<ILogger<ProductsController>>();
        
        _productsController = new ProductsController(
            _productServiceMock.Object, 
            _pictureServiceMock.Object, 
            _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _productsController.Dispose();
    }

    [Test]
    public async Task GetProducts_ShouldReturnOkResultWithProducts()
    {
        // Arrange
        var products = new PagedList<Product>(new List<Product>
        {
            new() { Id = 1, Name = "Product 1", Price = 10 },
            new() { Id = 2, Name = "Product 2", Price = 20 }
        }, 0, 2);

        _productServiceMock.Setup(x => x.SearchProductsAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<IList<int>>(),
            It.IsAny<IList<int>>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<ProductType?>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<decimal?>(),
            It.IsAny<decimal?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<IList<SpecificationAttributeOption>>(),
            It.IsAny<ProductSortingEnum>(),
            It.IsAny<bool>(),
            It.IsAny<bool?>()
        )).ReturnsAsync(products);

        _pictureServiceMock.Setup(x => x.GetPicturesByProductIdAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Picture>());

        // Act
        var result = await _productsController.GetProducts();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var returnedProducts = okResult.Value as List<ProductDto>;
        Assert.That(returnedProducts, Is.Not.Null);
        Assert.That(returnedProducts.Count, Is.EqualTo(2));
    }
}
