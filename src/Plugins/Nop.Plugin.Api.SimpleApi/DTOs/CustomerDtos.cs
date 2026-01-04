namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Active { get; set; }
    }

    public class CustomerCreateDto
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Active { get; set; } = true;
    }

    public class CustomerUpdateDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Active { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class ChangePasswordDto
    {
        public int CustomerId { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
