using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class ProductUpdateDto
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public decimal Price { get; set; }
        public bool Published { get; set; }
    }
}
