using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using community_app.Data;
using community_app.Models;
using community_app.DTOs;
using System.Security.Claims;

namespace community_app.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/communities/{id}/events
        [HttpGet("communities/{communityId}/events")]
        public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetEvents(int communityId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var events = await _context.Events
                .Where(e => e.CommunityId == communityId)
                .Include(e => e.CreatedBy)
                .Include(e => e.Participants)
                .OrderBy(e => e.EventDate)
                .Select(e => new EventResponseDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    CreatedAt = e.CreatedAt,
                    CreatedByUsername = e.CreatedBy.Username,
                    ParticipantCount = e.Participants.Count,
                    IsParticipating = e.Participants.Any(r => r.UserId == userId)
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET /api/events/{id}
        [HttpGet("events/{eventId}")]
        public async Task<ActionResult<EventResponseDto>> GetEvent(int eventId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var ev = await _context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.Participants)
                .Where(e => e.Id == eventId)
                .Select(e => new EventResponseDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    CreatedAt = e.CreatedAt,
                    CreatedByUsername = e.CreatedBy.Username,
                    ParticipantCount = e.Participants.Count,
                    IsParticipating = e.Participants.Any(r => r.UserId == userId)
                })
                .FirstOrDefaultAsync();

            if (ev == null)
                return NotFound();

            return Ok(ev);
        }

        // POST /api/communities/{id}/events
        [HttpPost("communities/{communityId}/events")]
        public async Task<ActionResult<EventResponseDto>> CreateEvent(int communityId, CreateEventDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var membership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == communityId && cm.UserId == userId);

            if (membership == null || (membership.Role != "Admin" && membership.Role != "Moderator"))
                return Forbid();

            var ev = new Event
            {
                CommunityId = communityId,
                CreatedById = userId,
                Title = dto.Title,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            await _context.Entry(ev).Reference(e => e.CreatedBy).LoadAsync();

            return Ok(new EventResponseDto
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                CreatedAt = ev.CreatedAt,
                CreatedByUsername = ev.CreatedBy.Username,
                ParticipantCount = 0,
                IsParticipating = false
            });
        }

        // PATCH /api/events/{id}
        [HttpPatch("events/{eventId}")]
        public async Task<IActionResult> UpdateEvent(int eventId, PatchEventDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null)
                return NotFound();

            var membership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == ev.CommunityId && cm.UserId == userId);

            if (membership == null || (membership.Role != "Admin" && membership.Role != "Moderator"))
                return Forbid();

            if (dto.Title != null) ev.Title = dto.Title;
            if (dto.Description != null) ev.Description = dto.Description;
            if (dto.EventDate != null) ev.EventDate = dto.EventDate.Value;
            if (dto.Location != null) ev.Location = dto.Location;

            await _context.SaveChangesAsync();
            return Ok("Event updated successfully.");
        }

        // DELETE /api/events/{id}
        [HttpDelete("events/{eventId}")]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null)
                return NotFound();

            var membership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == ev.CommunityId && cm.UserId == userId);

            if (membership == null || (membership.Role != "Admin" && membership.Role != "Moderator"))
                return Forbid();

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/events/{id}/participate  (toggle)
        [HttpPost("events/{eventId}/participate")]
        public async Task<IActionResult> ToggleParticipation(int eventId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var existing = await _context.EventParticipants
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (existing != null)
            {
                _context.EventParticipants.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { participating = false });
            }

            _context.EventParticipants.Add(new EventParticipant
            {
                EventId = eventId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { participating = true });
        }

        // GET /api/events/my-events
        [HttpGet("events/my-events")]
        public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetMyEvents()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var events = await _context.EventParticipants
                .Where(r => r.UserId == userId)
                .Include(r => r.Event).ThenInclude(e => e.CreatedBy)
                .Include(r => r.Event).ThenInclude(e => e.Participants)
                .Select(r => new EventResponseDto
                {
                    Id = r.Event.Id,
                    Title = r.Event.Title,
                    Description = r.Event.Description,
                    EventDate = r.Event.EventDate,
                    Location = r.Event.Location,
                    CreatedAt = r.Event.CreatedAt,
                    CreatedByUsername = r.Event.CreatedBy.Username,
                    ParticipantCount = r.Event.Participants.Count,
                    IsParticipating = true
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET /api/events/{id}/participants
        [HttpGet("events/{eventId}/participants")]
        public async Task<ActionResult<IEnumerable<MemberResponseDto>>> GetParticipants(int eventId)
        {
            var participants = await _context.EventParticipants
                .Where(r => r.EventId == eventId)
                .Include(r => r.User)
                .Select(r => new MemberResponseDto
                {
                    UserId = r.UserId,
                    Username = r.User.Username,
                    Email = r.User.Email,
                    JoinedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(participants);
        }
    }
}