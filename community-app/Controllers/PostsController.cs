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
    public class PostsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PostsController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/communities/{id}/posts
        [HttpGet("communities/{communityId}/posts")]
        public async Task<ActionResult<IEnumerable<PostResponseDto>>> GetPosts(int communityId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var posts = await _context.Posts
                .Where(p => p.CommunityId == communityId)
                .Include(p => p.Author)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    AuthorUsername = p.Author.Username,
                    AuthorId = p.AuthorId,
                    LikeCount = p.Likes.Count,
                    IsLikedByMe = p.Likes.Any(l => l.UserId == userId),
                    CommentCount = p.Comments.Count
                })
                .ToListAsync();

            return Ok(posts);
        }

        // GET /api/posts/{id}
        [HttpGet("posts/{postId}")]
        public async Task<ActionResult<PostResponseDto>> GetPost(int postId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var post = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Where(p => p.Id == postId)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    AuthorUsername = p.Author.Username,
                    AuthorId = p.AuthorId,
                    LikeCount = p.Likes.Count,
                    IsLikedByMe = p.Likes.Any(l => l.UserId == userId),
                    CommentCount = p.Comments.Count
                })
                .FirstOrDefaultAsync();

            if (post == null)
                return NotFound();

            return Ok(post);
        }

        // POST /api/communities/{id}/posts
        [HttpPost("communities/{communityId}/posts")]
        public async Task<ActionResult<PostResponseDto>> CreatePost(int communityId, CreatePostDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var isMember = await _context.CommunityMembers
                .AnyAsync(cm => cm.CommunityId == communityId && cm.UserId == userId);

            if (!isMember)
                return Forbid();

            var post = new Post
            {
                CommunityId = communityId,
                AuthorId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            await _context.Entry(post).Reference(p => p.Author).LoadAsync();

            return Ok(new PostResponseDto
            {
                Id = post.Id,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                AuthorUsername = post.Author.Username,
                AuthorId = post.AuthorId,
                LikeCount = 0,
                IsLikedByMe = false,
                CommentCount = 0
            });
        }

        // PATCH /api/posts/{id}
        [HttpPatch("posts/{postId}")]
        public async Task<IActionResult> UpdatePost(int postId, PatchPostDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return NotFound();

            if (post.AuthorId != userId)
                return Forbid();

            if (dto.Content != null) post.Content = dto.Content;

            await _context.SaveChangesAsync();
            return Ok("Post updated successfully.");
        }

        // DELETE /api/posts/{id}
        [HttpDelete("posts/{postId}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return NotFound();

            var membership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == post.CommunityId && cm.UserId == userId);

            bool isAuthor = post.AuthorId == userId;
            bool isModerator = membership?.Role == "Moderator" || membership?.Role == "Admin";

            if (!isAuthor && !isModerator)
                return Forbid();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/posts/{id}/like  (toggle)
        [HttpPost("posts/{postId}/like")]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var existing = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existing != null)
            {
                _context.Likes.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { liked = false });
            }

            _context.Likes.Add(new Like
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { liked = true });
        }

        // GET /api/posts/{id}/comments
        [HttpGet("posts/{postId}/comments")]
        public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetComments(int postId)
        {
            var comments = await _context.Comments
                .Where(c => c.PostId == postId)
                .Include(c => c.Author)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    AuthorUsername = c.Author.Username,
                    AuthorId = c.AuthorId
                })
                .ToListAsync();

            return Ok(comments);
        }

        // POST /api/posts/{id}/comments
        [HttpPost("posts/{postId}/comments")]
        public async Task<ActionResult<CommentResponseDto>> AddComment(int postId, CreateCommentDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var comment = new Comment
            {
                PostId = postId,
                AuthorId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            await _context.Entry(comment).Reference(c => c.Author).LoadAsync();

            return Ok(new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                AuthorUsername = comment.Author.Username,
                AuthorId = comment.AuthorId
            });
        }

        // PATCH /api/posts/{postId}/comments/{commentId}
        [HttpPatch("posts/{postId}/comments/{commentId}")]
        public async Task<IActionResult> UpdateComment(int postId, int commentId, PatchCommentDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId);

            if (comment == null)
                return NotFound();

            if (comment.AuthorId != userId)
                return Forbid();

            if (dto.Content != null) comment.Content = dto.Content;

            await _context.SaveChangesAsync();
            return Ok("Comment updated successfully.");
        }

        // DELETE /api/posts/{postId}/comments/{commentId}
        [HttpDelete("posts/{postId}/comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(int postId, int commentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var comment = await _context.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId);

            if (comment == null)
                return NotFound();

            var membership = await _context.CommunityMembers
                .FirstOrDefaultAsync(cm => cm.CommunityId == comment.Post.CommunityId && cm.UserId == userId);

            bool isAuthor = comment.AuthorId == userId;
            bool isModerator = membership?.Role == "Moderator" || membership?.Role == "Admin";

            if (!isAuthor && !isModerator)
                return Forbid();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}