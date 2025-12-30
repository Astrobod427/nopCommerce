using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Orders;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/orders")]
    [ApiController]
    public class OrdersController : Nop.Web.Framework.Controllers.BasePluginController
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _orderService.SearchOrdersAsync();
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
    }
}
