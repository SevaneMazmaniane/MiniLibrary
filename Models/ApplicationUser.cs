using Microsoft.AspNetCore.Identity;

namespace MiniLibrary.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
