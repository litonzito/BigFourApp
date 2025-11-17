using System.ComponentModel.DataAnnotations;

namespace BigFourApp.Models.Event
{
    // Models/Asiento.cs
    public class Asiento
    {
        [Key]
        public int Id_Asiento { get; set; }
        public string? EventId { get; set; }
        public int Numero { get; set; }
        public string? SectionId { get; set; }
        public EstadoAsiento Estado { get; set; } = EstadoAsiento.Disponible;

        public Boleto? Boleto { get; set; }
        public Evento? Event { get; set; } 
    }

}
