using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using community_app.Data;
using community_app.Models;
using community_app.DTOs;
using System.Security.Claims;

namespace community_app.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommunitiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommunitiesController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/communities
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CommunityResponseDto>>> GetCommunities()
        {
            var communities = await _context.Communities
                .Include(c => c.CreatedBy)
                .Where(c => c.IsActive)
                .Select(c => new CommunityResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CreatedAt = c.CreatedAt,
                    CreatedByUsername = c.CreatedBy.Username,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(communities);
        }

        // GET /api/communities/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<CommunityResponseDto>> GetCommunity(int id)
        {
            var community = await _context.Communities
                .Include(c => c.CreatedBy)
                .Where(c => c.Id == id && c.IsActive)
                .Select(c => new CommunityResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CreatedAt = c.CreatedAt,
                    CreatedByUsername = c.CreatedBy.Username,
                    IsActive = c.IsActive
                })
                .FirstOrDefaultAsync();

            if (community == null)
                return NotFound();

            return Ok(community);
        }

        // GET /api/communities/joined
        [HttpGet("joined")]
        public async Task<ActionResult<IEnumerable<CommunityResponseDto>>> GetJoinedCommunities()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var communities = await _context.CommunityMembers
                .Where(cm => cm.UserId == userId)
                .Include(cm => cm.Community)
                    .ThenInclude(c => c.CreatedBy)
                .Select(cm => new CommunityResponseDto
                {
                    Id = cm.Community.Id,
                    Name = cm.Community.Name,
                    Description = cm.Community.Description,
                    CreatedAt = cm.Community.CreatedAt,
                    CreatedByUsername = cm.Community.CreatedBy.Username,
                    IsActive = cm.Community.IsActive
                })
                .ToListAsync();

            return Ok(communities);
        }

        // POST /api/communities
        [HttpPost]
        public async Task<ActionResult<CommunityResponseDto>> CreateCommunity(CreateCommunityDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var community = new Community
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Communities.Add(community);
            await _context.SaveChangesAsync();

            // Auto-add creator as community Admin
            _context.CommunityMembers.Add(new CommunityMember
            {
                CommunityId = community.Id,
                UserId = userId,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await _context.Entry(community).Reference(c => c.CreatedBy).LoadAsync();

            var response = new CommunityResponseDto
            {
                Id = community.Id,
                Name = community.Name,
                Description = community.Description,
                CreatedAt = community.CreatedAt,
                CreatedByUsername = community.CreatedBy.Username,
                IsActive = community.IsActive
            };

            return CreatedAtAction(nameof(GetCommunity), new { id = community.Id }, response);
        }

        // PATCH /api/communities/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateCommunity(int id, PatchCommunityDto dto)
        {
            var community = await _context.Communities.FindAsync(id);
            if (community == null)
                return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var membership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == id && cm.UserId == userId);

            if (membership == null || membership.Role != "Admin")
                return Forbid();

            if (dto.Name != null) community.Name = dto.Name;
            if (dto.Description != null) community.Description = dto.Description;
            if (dto.IsActive != null) community.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/communities/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommunity(int id)
        {
            var community = await _context.Communities.FindAsync(id);
            if (community == null)
                return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var membership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == id && cm.UserId == userId);

            if (membership == null || membership.Role != "Admin")
                return Forbid();

            community.IsActive = false; // soft delete
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST /api/communities/{id}/join
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinCommunity(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var community = await _context.Communities.FindAsync(id);
            if (community == null || !community.IsActive)
                return NotFound();

            bool alreadyMember = await _context.CommunityMembers
                .AnyAsync(cm => cm.CommunityId == id && cm.UserId == userId);

            if (alreadyMember)
                return BadRequest("Already a member of this community.");

            _context.CommunityMembers.Add(new CommunityMember
            {
                CommunityId = id,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok("Joined successfully.");
        }

        // POST /api/communities/{id}/leave
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> LeaveCommunity(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var membership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == id && cm.UserId == userId);

            if (membership == null)
                return BadRequest("You are not a member of this community.");

            _context.CommunityMembers.Remove(membership);
            await _context.SaveChangesAsync();
            return Ok("Left successfully.");
        }

        private bool CommunityExists(int id)
        {
            return _context.Communities.Any(e => e.Id == id);
        }
    }
}