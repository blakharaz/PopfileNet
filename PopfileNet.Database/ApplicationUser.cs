using Microsoft.AspNetCore.Identity;

namespace PopfileNet.Database;

public class ApplicationUser : IdentityUser
{
    public string? TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
