using System.ComponentModel.DataAnnotations;

namespace community_app.Models
{
    public class Community
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CreatedByUserId { get; set; }
        public User CreatedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<CommunityMember> Members { get; set; }

    }
}