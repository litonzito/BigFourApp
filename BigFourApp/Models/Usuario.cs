using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public bool IsEventManager { get; set; } = false;
}
