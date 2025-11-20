using System;
using System.Collections.Generic;
using BigFourApp.Models;


namespace BigFourApp.Models.ViewModels
{
    /// <summary>
    /// Conjunto de view‑models (SeatSelectionViewModel, SectionVM, RowVM, SeatVM, CartItemVM) usados por la vista de selección para renderizar secciones, filas, asientos y el carrito.
    /// </summary>
    public class SeatSelectionViewModel
    {
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

    public class SectionVM
    {
        public string SectionId { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal BasePrice { get; set; }
        public int SeatsPerRow { get; set; }
        public IReadOnlyList<RowVM> Rows { get; set; } = new List<RowVM>();
    }

    public class RowVM
    {
        public string RowId { get; set; } = "";
        public string Name { get; set; } = "";
        public IReadOnlyList<SeatVM> Seats { get; set; } = new List<SeatVM>();
    }

    public class SeatVM
    {
        public string SeatId { get; set; } = "";
        public string Label { get; set; } = "";
        public int SeatNumber { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
        public EstadoAsiento State { get; set; } = EstadoAsiento.Disponible;
        public string SectionId { get; set; } = "";
        public string RowId { get; set; } = "";
    }

    public class CartItemVM
    {
        public string SeatId { get; set; } = "";
        public string Label { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class SoldOutViewModel
    {
        public string EventName { get; set; } = "";
        public DateTime EventDate { get; set; }
        public string? VenueName { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
    }
}
