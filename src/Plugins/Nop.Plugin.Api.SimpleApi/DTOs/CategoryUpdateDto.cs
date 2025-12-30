using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class CategoryUpdateDto
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Published { get; set; }
        public int DisplayOrder { get; set; }
    }
}
