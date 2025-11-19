using BigFourApp.Models;
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public bool IsEventManager { get; set; } = false;

    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
}
