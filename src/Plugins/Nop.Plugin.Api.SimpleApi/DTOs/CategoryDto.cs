namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Published { get; set; }
        public int DisplayOrder { get; set; }
        public string ImageUrl { get; set; }
    }
}
