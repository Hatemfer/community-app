using System.ComponentModel.DataAnnotations;

namespace community_app.DTOs
{
    public class MemberResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class UpdateMemberRoleDto
    {
        [Required]
        public string Role { get; set; } // "member", "moderator", "admin"
    }
}