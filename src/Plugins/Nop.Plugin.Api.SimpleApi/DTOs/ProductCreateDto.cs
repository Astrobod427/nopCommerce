using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class ProductCreateDto
    {
        [Required]
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public decimal Price { get; set; }
        public bool Published { get; set; } = true;
    }
}
