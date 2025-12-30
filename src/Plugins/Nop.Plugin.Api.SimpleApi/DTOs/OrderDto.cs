using System;

namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public Guid OrderGuid { get; set; }
        public int CustomerId { get; set; }
        public int OrderStatusId { get; set; }
        public int PaymentStatusId { get; set; }
        public int ShippingStatusId { get; set; }
        public decimal OrderTotal { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}
