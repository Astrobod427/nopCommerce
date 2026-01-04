using Microsoft.AspNetCore.Mvc;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.SimpleApi.Controllers;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Orders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Tests;

[TestFixture]
public class OrdersControllerTests
{
    private Mock<IOrderService> _orderServiceMock;
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IShoppingCartService> _shoppingCartServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IAddressService> _addressServiceMock;
    private Mock<IWorkContext> _workContextMock;
    private Mock<IStoreContext> _storeContextMock;
    private Mock<IGenericAttributeService> _genericAttributeServiceMock;
    private Mock<IOrderProcessingService> _orderProcessingServiceMock;
    private Mock<ICountryService> _countryServiceMock;
    
    private OrdersController _ordersController;

    [SetUp]
    public void Setup()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _customerServiceMock = new Mock<ICustomerService>();
        _shoppingCartServiceMock = new Mock<IShoppingCartService>();
        _productServiceMock = new Mock<IProductService>();
        _addressServiceMock = new Mock<IAddressService>();
        _workContextMock = new Mock<IWorkContext>();
        _storeContextMock = new Mock<IStoreContext>();
        _genericAttributeServiceMock = new Mock<IGenericAttributeService>();
        _orderProcessingServiceMock = new Mock<IOrderProcessingService>();
        _countryServiceMock = new Mock<ICountryService>();

        _ordersController = new OrdersController(
            _orderServiceMock.Object,
            _customerServiceMock.Object,
            _shoppingCartServiceMock.Object,
            _productServiceMock.Object,
            _addressServiceMock.Object,
            _workContextMock.Object,
            _storeContextMock.Object,
            _genericAttributeServiceMock.Object,
            _orderProcessingServiceMock.Object,
            _countryServiceMock.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        _ordersController.Dispose();
    }

    [Test]
    public async Task GetOrders_ShouldReturnOkResultWithOrders()
    {
        // Arrange
        var orders = new PagedList<Order>(new List<Order>
        {
            new() { Id = 1, CustomerId = 1, OrderTotal = 100 },
            new() { Id = 2, CustomerId = 2, OrderTotal = 200 }
        }, 0, 2);

        _orderServiceMock.Setup(x => x.SearchOrdersAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<List<int>>(),
            It.IsAny<List<int>>(),
            It.IsAny<List<int>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<bool>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _ordersController.GetOrders();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var returnedOrders = okResult.Value as List<OrderDto>;
        Assert.That(returnedOrders, Is.Not.Null);
        Assert.That(returnedOrders.Count, Is.EqualTo(2));
    }
}
