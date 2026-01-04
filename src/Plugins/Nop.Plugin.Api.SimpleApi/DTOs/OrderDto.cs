using System;
using System.Collections.Generic;

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

    public class OrderCreateDto
    {
        public int CustomerId { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public AddressDto BillingAddress { get; set; }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class AddressDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string City { get; set; }
        public string Address1 { get; set; }
        public string ZipPostalCode { get; set; }
        public string PhoneNumber { get; set; }
        public int CountryId { get; set; } = 1;
    }
}
