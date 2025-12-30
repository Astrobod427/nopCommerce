using System;

namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class ReturnRequestDto
    {
        public int Id { get; set; }
        public string CustomNumber { get; set; }
        public int CustomerId { get; set; }
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
        public string ReasonForReturn { get; set; }
        public string RequestedAction { get; set; }
        public string CustomerComments { get; set; }
        public string StaffNotes { get; set; }
        public int ReturnRequestStatusId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}
