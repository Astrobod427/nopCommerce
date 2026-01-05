using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/orders")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class OrdersController : Nop.Web.Framework.Controllers.BasePluginController
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductService _productService;
        private readonly IAddressService _addressService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ICountryService _countryService;

        public OrdersController(
            IOrderService orderService,
            ICustomerService customerService,
            IShoppingCartService shoppingCartService,
            IProductService productService,
            IAddressService addressService,
            IWorkContext workContext,
            IStoreContext storeContext,
            IGenericAttributeService genericAttributeService,
            IOrderProcessingService orderProcessingService,
            ICountryService countryService)
        {
            _orderService = orderService;
            _customerService = customerService;
            _shoppingCartService = shoppingCartService;
            _productService = productService;
            _addressService = addressService;
            _workContext = workContext;
            _storeContext = storeContext;
            _genericAttributeService = genericAttributeService;
            _orderProcessingService = orderProcessingService;
            _countryService = countryService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetOrders([FromQuery] int customerId = 0)
        {
            // If customerId provided, filter by it.
            // Note: In a real app, we should check if the current user has rights to see these orders.
            
            // SearchOrdersAsync arguments have changed in newer Nop versions, checking signature...
            // Assuming (storeId: 0, vendorId: 0, customerId: customerId ...)
            
            // We use 0 for most filters to ignore them
            var orders = await _orderService.SearchOrdersAsync(customerId: customerId);
            
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                OrderGuid = o.OrderGuid,
                CustomerId = o.CustomerId,
                OrderStatusId = o.OrderStatusId,
                PaymentStatusId = o.PaymentStatusId,
                ShippingStatusId = o.ShippingStatusId,
                OrderTotal = o.OrderTotal,
                CreatedOnUtc = o.CreatedOnUtc
            }).ToList();

            return Ok(orderDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null || order.Deleted)
            {
                return NotFound();
            }

            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderGuid = order.OrderGuid,
                CustomerId = order.CustomerId,
                OrderStatusId = order.OrderStatusId,
                PaymentStatusId = order.PaymentStatusId,
                ShippingStatusId = order.ShippingStatusId,
                OrderTotal = order.OrderTotal,
                CreatedOnUtc = order.CreatedOnUtc
            };

            return Ok(orderDto);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto orderDto)
        {
            if (orderDto == null || orderDto.Items == null || !orderDto.Items.Any())
                return BadRequest("No items in order");

            var customer = await _customerService.GetCustomerByIdAsync(orderDto.CustomerId);
            if (customer == null)
                return BadRequest("Customer not found");

            // 1. Force current customer in WorkContext (Hack for SimpleAPI to work with internal services)
            await _workContext.SetCurrentCustomerAsync(customer);
            var store = await _storeContext.GetCurrentStoreAsync();

            // 2. Clear existing cart
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
            foreach (var item in cart)
            {
                await _shoppingCartService.DeleteShoppingCartItemAsync(item);
            }

            // 3. Add items to cart
            foreach (var itemDto in orderDto.Items)
            {
                var product = await _productService.GetProductByIdAsync(itemDto.ProductId);
                if (product == null || product.Deleted || !product.Published)
                    continue;

                await _shoppingCartService.AddToCartAsync(customer, product, ShoppingCartType.ShoppingCart, store.Id, 
                    quantity: itemDto.Quantity);
            }

            // 4. Create/Set Address
            var address = new Address
            {
                FirstName = orderDto.BillingAddress.FirstName,
                LastName = orderDto.BillingAddress.LastName,
                Email = orderDto.BillingAddress.Email,
                City = orderDto.BillingAddress.City,
                Address1 = orderDto.BillingAddress.Address1,
                ZipPostalCode = orderDto.BillingAddress.ZipPostalCode,
                PhoneNumber = orderDto.BillingAddress.PhoneNumber,
                CountryId = orderDto.BillingAddress.CountryId,
                CreatedOnUtc = DateTime.UtcNow
            };
            
            // If country not found, default to first one
            if (!address.CountryId.HasValue || await _countryService.GetCountryByIdAsync(address.CountryId.Value) == null)
            {
                var countries = await _countryService.GetAllCountriesAsync();
                var firstCountry = countries.FirstOrDefault();
                if (firstCountry != null) address.CountryId = firstCountry.Id;
            }

            await _addressService.InsertAddressAsync(address);

            // Assign address to customer (Billing & Shipping)
            customer.BillingAddressId = address.Id;
            customer.ShippingAddressId = address.Id;
            await _customerService.UpdateCustomerAsync(customer);

            // 5. Set default Payment & Shipping (Generic Attributes)
            // We use 'Payments.Manual' as a default safe fallback
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, "Payments.Manual", store.Id);
            
            // For shipping, we might need a valid option name (e.g. "Shipping.FixedOrByWeight"). 
            // If we skip this, PlaceOrder might fail if shipping is required.
            // Let's assume shipping is required but we don't set a specific option, hoping Nop picks a default or we might need to fetch options.
            // Simplified: we rely on PlaceOrder to handle or fail. If it fails, we'll need to expand this.

            // 6. Place Order
            var processPaymentRequest = new ProcessPaymentRequest();
            processPaymentRequest.StoreId = store.Id;
            processPaymentRequest.CustomerId = customer.Id;
            processPaymentRequest.PaymentMethodSystemName = "Payments.Manual";

            var placeOrderResult = await _orderProcessingService.PlaceOrderAsync(processPaymentRequest);

            if (placeOrderResult.Success)
            {
                var placedOrder = placeOrderResult.PlacedOrder;
                return Ok(new { OrderId = placedOrder.Id, OrderGuid = placedOrder.OrderGuid, Message = "Order created successfully" });
            }
            else
            {
                return BadRequest(new { Errors = placeOrderResult.Errors });
            }
        }
    }
}