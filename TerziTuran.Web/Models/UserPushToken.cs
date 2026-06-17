using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.Models;

public class UserPushToken
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, StringLength(512)]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Platform { get; set; } = string.Empty;

    [StringLength(150)]
    public string? DeviceName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
