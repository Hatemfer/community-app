using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using community_app.Data;
using community_app.Models;
using community_app.DTOs;
using System.Security.Claims;

namespace community_app.Controllers
{
    [Route("api/communities/{communityId}")]
    [ApiController]
    [Authorize]
    public class MembersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MembersController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/communities/{id}/members
        [HttpGet("members")]
        public async Task<ActionResult<IEnumerable<MemberResponseDto>>> GetMembers(int communityId)
        {
            var members = await _context.CommunityMembers
                .Where(cm => cm.CommunityId == communityId)
                .Include(cm => cm.User)
                .Select(cm => new MemberResponseDto
                {
                    UserId = cm.UserId,
                    Username = cm.User.Username,
                    Email = cm.User.Email,
                    Role = cm.Role,
                    JoinedAt = cm.JoinedAt
                })
                .ToListAsync();

            return Ok(members);
        }

        // PUT /api/communities/{id}/members/{userId}/role
        [HttpPut("members/{targetUserId}/role")]
        public async Task<IActionResult> UpdateMemberRole(
            int communityId, int targetUserId, UpdateMemberRoleDto dto)
        {
            var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var requesterMembership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == communityId && cm.UserId == requesterId);

            if (requesterMembership == null || requesterMembership.Role != "Admin")
                return Forbid();

            var targetMembership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == communityId && cm.UserId == targetUserId);

            if (targetMembership == null)
                return NotFound("User is not a member of this community.");

            var validRoles = new[] { "Member", "Moderator", "Admin" };
            if (!validRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Invalid role. Use Member, Moderator, or Admin.");

            // Normalize casing
            targetMembership.Role = char.ToUpper(dto.Role[0]) + dto.Role.Substring(1).ToLower();
            await _context.SaveChangesAsync();

            return Ok("Role updated successfully.");
        }
    }
}