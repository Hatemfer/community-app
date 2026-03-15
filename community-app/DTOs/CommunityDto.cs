using System.ComponentModel.DataAnnotations;

namespace community_app.DTOs
{
    public class CreateCommunityDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [StringLength(500)]
        public string Description { get; set; }
    }

    public class PatchCommunityDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CommunityResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUsername { get; set; }
        public bool IsActive { get; set; }
    }
}