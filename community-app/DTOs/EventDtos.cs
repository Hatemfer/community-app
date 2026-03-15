using System.ComponentModel.DataAnnotations;

namespace community_app.DTOs
{
    public class CreateEventDto
    {
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        [Required]
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
    }

    public class PatchEventDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? EventDate { get; set; }
        public string? Location { get; set; }
    }

    public class EventResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUsername { get; set; }
        public int ParticipantCount { get; set; }
        public bool IsParticipating { get; set; }
    }
}