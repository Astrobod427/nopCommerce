using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Shipping;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/shipments")]
    [ApiController]
    public class ShipmentsController : Nop.Web.Framework.Controllers.BasePluginController
    {
        private readonly IShipmentService _shipmentService;

        public ShipmentsController(IShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetShipments()
        {
            var shipments = await _shipmentService.GetAllShipmentsAsync();
            var shipmentDtos = shipments.Select(s => new ShipmentDto
            {
                Id = s.Id,
                OrderId = s.OrderId,
                TrackingNumber = s.TrackingNumber,
                TotalWeight = s.TotalWeight,
                ShippedDateUtc = s.ShippedDateUtc,
                DeliveryDateUtc = s.DeliveryDateUtc,
                ReadyForPickupDateUtc = s.ReadyForPickupDateUtc,
                AdminComment = s.AdminComment,
                CreatedOnUtc = s.CreatedOnUtc
            }).ToList();

            return Ok(shipmentDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetShipmentById(int id)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(id);
            if (shipment == null)
            {
                return NotFound();
            }

            var shipmentDto = new ShipmentDto
            {
                Id = shipment.Id,
                OrderId = shipment.OrderId,
                TrackingNumber = shipment.TrackingNumber,
                TotalWeight = shipment.TotalWeight,
                ShippedDateUtc = shipment.ShippedDateUtc,
                DeliveryDateUtc = shipment.DeliveryDateUtc,
                ReadyForPickupDateUtc = shipment.ReadyForPickupDateUtc,
                AdminComment = shipment.AdminComment,
                CreatedOnUtc = shipment.CreatedOnUtc
            };

            return Ok(shipmentDto);
        }
    }
}
