using Microsoft.AspNetCore.Mvc;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.SimpleApi.Controllers;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Orders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Tests;

[TestFixture]
public class OrdersControllerTests
{
    private Mock<IOrderService> _orderServiceMock;
    private OrdersController _ordersController;

    [SetUp]
    public void Setup()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _ordersController = new OrdersController(_orderServiceMock.Object);
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
