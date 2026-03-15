namespace community_app.Models
{
    public class CommunityMember
    {
        public int Id { get; set; }

        public int CommunityId { get; set; }
        public Community Community { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public string Role { get; set; } = "Member"; // Member, Moderator, Admin

        public bool IsApproved { get; set; } = true;
    }
}