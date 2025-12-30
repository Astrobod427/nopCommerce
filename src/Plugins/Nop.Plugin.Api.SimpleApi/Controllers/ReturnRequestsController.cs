using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Orders;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/return-requests")]
    [ApiController]
    public class ReturnRequestsController : Nop.Web.Framework.Controllers.BasePluginController
    {
        private readonly IReturnRequestService _returnRequestService;

        public ReturnRequestsController(IReturnRequestService returnRequestService)
        {
            _returnRequestService = returnRequestService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetReturnRequests()
        {
            var returnRequests = await _returnRequestService.SearchReturnRequestsAsync();
            var returnRequestDtos = returnRequests.Select(rr => new ReturnRequestDto
            {
                Id = rr.Id,
                CustomNumber = rr.CustomNumber,
                CustomerId = rr.CustomerId,
                OrderItemId = rr.OrderItemId,
                Quantity = rr.Quantity,
                ReasonForReturn = rr.ReasonForReturn,
                RequestedAction = rr.RequestedAction,
                CustomerComments = rr.CustomerComments,
                StaffNotes = rr.StaffNotes,
                ReturnRequestStatusId = rr.ReturnRequestStatusId,
                CreatedOnUtc = rr.CreatedOnUtc
            }).ToList();

            return Ok(returnRequestDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReturnRequestById(int id)
        {
            var returnRequest = await _returnRequestService.GetReturnRequestByIdAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            var returnRequestDto = new ReturnRequestDto
            {
                Id = returnRequest.Id,
                CustomNumber = returnRequest.CustomNumber,
                CustomerId = returnRequest.CustomerId,
                OrderItemId = returnRequest.OrderItemId,
                Quantity = returnRequest.Quantity,
                ReasonForReturn = returnRequest.ReasonForReturn,
                RequestedAction = returnRequest.RequestedAction,
                CustomerComments = returnRequest.CustomerComments,
                StaffNotes = returnRequest.StaffNotes,
                ReturnRequestStatusId = returnRequest.ReturnRequestStatusId,
                CreatedOnUtc = returnRequest.CreatedOnUtc
            };

            return Ok(returnRequestDto);
        }
    }
}
