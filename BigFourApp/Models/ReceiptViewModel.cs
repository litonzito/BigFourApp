using System;
using System.Collections.Generic;
using BigFourApp.Models.ViewModels;


namespace BigFourApp.Models.ViewModels
{
   
    public class ReceiptViewModel
    {
        public string nombre { get; set; } = "";
        public string apellido { get; set; } = "";
        public string? metodoPago { get; set; }
        public string EventId { get; set; } = "";
        public string EventName { get; set; } = "";
        public string? VenueName { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? SeatmapUrl { get; set; }
        public IReadOnlyList<SectionVM> Sections { get; set; } = new List<SectionVM>();
        public IReadOnlyList<CartItemVM> CartItems { get; set; } = new List<CartItemVM>();
        public decimal Subtotal { get; set; } = 0m;

    }
}
   