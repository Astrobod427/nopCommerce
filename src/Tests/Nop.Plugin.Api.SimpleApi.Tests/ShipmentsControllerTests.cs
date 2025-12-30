using Microsoft.AspNetCore.Mvc;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Api.SimpleApi.Controllers;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Shipping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Tests;

[TestFixture]
public class ShipmentsControllerTests
{
    private Mock<IShipmentService> _shipmentServiceMock;
    private ShipmentsController _shipmentsController;

    [SetUp]
    public void Setup()
    {
        _shipmentServiceMock = new Mock<IShipmentService>();
        _shipmentsController = new ShipmentsController(_shipmentServiceMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _shipmentsController.Dispose();
    }

    [Test]
    public async Task GetShipments_ShouldReturnOkResultWithShipments()
    {
        // Arrange
        var shipments = new PagedList<Shipment>(new List<Shipment>
        {
            new() { Id = 1, OrderId = 10, TrackingNumber = "TRACK1" },
            new() { Id = 2, OrderId = 20, TrackingNumber = "TRACK2" }
        }, 0, 2);

        _shipmentServiceMock.Setup(x => x.GetAllShipmentsAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>()))
            .ReturnsAsync(shipments);

        // Act
        var result = await _shipmentsController.GetShipments();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var returnedShipments = okResult.Value as List<ShipmentDto>;
        Assert.That(returnedShipments, Is.Not.Null);
        Assert.That(returnedShipments.Count, Is.EqualTo(2));
    }
}
