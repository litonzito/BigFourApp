using Microsoft.AspNetCore.Identity;
using BigFourApp.Models.Event;
namespace BigFourApp.Models
{
    public class ApplicationUser : IdentityUser
    {

        public bool IsEventManager { get; set; } = false;

        // navegacion
        public List<Venta> Ventas { get; set; }
        public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
        public ICollection<Evento> ManagedEvents { get; set; } = new List<Evento>();
    }
}
