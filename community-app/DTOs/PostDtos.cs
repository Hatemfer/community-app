using System.ComponentModel.DataAnnotations;

namespace community_app.DTOs
{
    public class CreatePostDto
    {
        [Required]
        public string Content { get; set; }
    }

    public class PatchPostDto
    {
        public string? Content { get; set; }
    }

    public class PostResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorUsername { get; set; }
        public int AuthorId { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByMe { get; set; }
        public int CommentCount { get; set; }
    }

    public class CreateCommentDto
    {
        [Required]
        public string Content { get; set; }
    }

    public class PatchCommentDto
    {
        public string? Content { get; set; }
    }

    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorUsername { get; set; }
        public int AuthorId { get; set; }
    }
}