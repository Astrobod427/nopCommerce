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
public class ReturnRequestsControllerTests
{
    private Mock<IReturnRequestService> _returnRequestServiceMock;
    private ReturnRequestsController _returnRequestsController;

    [SetUp]
    public void Setup()
    {
        _returnRequestServiceMock = new Mock<IReturnRequestService>();
        _returnRequestsController = new ReturnRequestsController(_returnRequestServiceMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _returnRequestsController.Dispose();
    }

    [Test]
    public async Task GetReturnRequests_ShouldReturnOkResultWithReturnRequests()
    {
        // Arrange
        var returnRequests = new PagedList<ReturnRequest>(new List<ReturnRequest>
        {
            new() { Id = 1, CustomNumber = "RR1", CustomerId = 1 },
            new() { Id = 2, CustomNumber = "RR2", CustomerId = 2 }
        }, 0, 2);

        _returnRequestServiceMock.Setup(x => x.SearchReturnRequestsAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<ReturnRequestStatus?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<bool>()))
            .ReturnsAsync(returnRequests);

        // Act
        var result = await _returnRequestsController.GetReturnRequests();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var returnedRequests = okResult.Value as List<ReturnRequestDto>;
        Assert.That(returnedRequests, Is.Not.Null);
        Assert.That(returnedRequests.Count, Is.EqualTo(2));
    }
}
