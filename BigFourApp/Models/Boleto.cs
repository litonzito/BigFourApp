using BigFourApp.Models.Event;
using System.ComponentModel.DataAnnotations;

namespace BigFourApp.Models
{
    public class Boleto
    {
        [Key]
        public int Id_Boleto { get; set; }
        public int? Id_Asiento { get; set; }
        public int? Id_DetalleVenta { get; set; }
        public string Tipo { get; set; } = "Sin tipo asignado";
        public bool Notificar { get; set; }

        public Asiento? Asiento { get; set; } 
        public DetalleVenta? DetalleVenta { get; set; }

    }
}
