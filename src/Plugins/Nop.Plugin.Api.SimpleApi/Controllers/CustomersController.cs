using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Api.SimpleApi.DTOs;
using Nop.Services.Customers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/customers")]
    [ApiController]
    public class CustomersController : Nop.Web.Framework.Controllers.BasePluginController
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerRegistrationService _customerRegistrationService;

        public CustomersController(ICustomerService customerService, ICustomerRegistrationService customerRegistrationService)
        {
            _customerService = customerService;
            _customerRegistrationService = customerRegistrationService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            var customerDtos = customers.Select(c => new CustomerDto
            {
                Id = c.Id,
                Email = c.Email,
                Username = c.Username,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Active = c.Active
            }).ToList();

            return Ok(customerDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null || customer.Deleted)
            {
                return NotFound();
            }

            var customerDto = new CustomerDto
            {
                Id = customer.Id,
                Email = customer.Email,
                Username = customer.Username,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Active = customer.Active
            };

            return Ok(customerDto);
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto customerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var registrationRequest = new CustomerRegistrationRequest(
                new Customer(),
                customerDto.Email,
                customerDto.Username,
                customerDto.Password,
                Nop.Core.Domain.Customers.PasswordFormat.Hashed,
                0, // StoreId
                true
            );

            var registrationResult = await _customerRegistrationService.RegisterCustomerAsync(registrationRequest);

            if (registrationResult.Success)
            {
                var customer = await _customerService.GetCustomerByEmailAsync(customerDto.Email);
                customer.FirstName = customerDto.FirstName;
                customer.LastName = customerDto.LastName;
                customer.Active = customerDto.Active;
                await _customerService.UpdateCustomerAsync(customer);

                return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, customer.Id);
            }

            return BadRequest(registrationResult.Errors);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDto customerDto)
        {
            if (id != customerDto.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null || customer.Deleted)
            {
                return NotFound();
            }

            customer.Email = customerDto.Email;
            customer.Username = customerDto.Username;
            customer.FirstName = customerDto.FirstName;
            customer.LastName = customerDto.LastName;
            customer.Active = customerDto.Active;

            await _customerService.UpdateCustomerAsync(customer);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            await _customerService.DeleteCustomerAsync(customer);

            return NoContent();
        }
    }
}
