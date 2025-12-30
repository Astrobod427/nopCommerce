using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class CategoryCreateDto
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Published { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
}
