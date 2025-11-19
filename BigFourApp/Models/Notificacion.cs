using BigFourApp.Models.Event;
using System.ComponentModel.DataAnnotations;

namespace BigFourApp.Models
{
    public class Notificacion
    {
        [Key]
        public int Id_Notificacion { get; set; }
        public string? Id_Usuario { get; set; }
        public string? Mensaje { get; set; }
        public string? Tipo { get; set; } = "Sin tipo asignado";
        public DateTime fecha { get; set; } 

        public ApplicationUser? Usuario { get; set; } 

    }
}
