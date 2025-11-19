using BigFourApp.Models;
using Microsoft.AspNetCore.Identity;
namespace BigFourApp.Models
{
    public class ApplicationUser : IdentityUser
    {

        public bool IsEventManager { get; set; } = false;

        // navegacion
        public List<Venta> Ventas { get; set; }
        public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    }
}