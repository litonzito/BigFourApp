
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace BigFourApp.Models
{
    public class Usuario
    {
        public string Id_Usuario { get; set; }
        public int ventaId { get; set; }
        [Required]
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = "";
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Telefono { get; set; } = null!;

        public List<Venta> Ventas { get; set; }

    }
}
