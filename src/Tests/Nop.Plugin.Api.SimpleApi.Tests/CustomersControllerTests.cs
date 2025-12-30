using Microsoft.AspNetCore.Mvc;
using Moq;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Api.SimpleApi.Controllers;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Customers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;

namespace Nop.Plugin.Api.SimpleApi.Tests;

[TestFixture]
public class CustomersControllerTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<ICustomerRegistrationService> _customerRegistrationServiceMock;
    private CustomersController _customersController;

    [SetUp]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerRegistrationServiceMock = new Mock<ICustomerRegistrationService>();
        _customersController = new CustomersController(_customerServiceMock.Object, _customerRegistrationServiceMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _customersController.Dispose();
    }

    [Test]
    public async Task GetCustomers_ShouldReturnOkResultWithCustomers()
    {
        // Arrange
        var customers = new PagedList<Customer>(new List<Customer>
        {
            new() { Id = 1, Email = "customer1@test.com" },
            new() { Id = 2, Email = "customer2@test.com" }
        }, 0, 2);

        _customerServiceMock.Setup(x => x.GetAllCustomersAsync(
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int[]>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<bool>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _customersController.GetCustomers();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var returnedCustomers = okResult.Value as List<CustomerDto>;
        Assert.That(returnedCustomers, Is.Not.Null);
        Assert.That(returnedCustomers.Count, Is.EqualTo(2));
    }
}
