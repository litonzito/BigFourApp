# BigFourApp — Avance

Resumen corto
-------------
Aplicación ASP.NET Core (MVC) para explorar eventos, seleccionar asientos y comprar entradas. Usa SQLite + Entity Framework Core, carga inicial de eventos desde JSON y generación automática de asientos por recinto.

Estado
------
- Funcionalidad principal: listado de eventos, detalle de evento, selección de asientos, carrito, pago simulado y recibo PDF.
- Persistencia: EF Core con migraciones y seed de prueba automática.
- UI: Bootstrap + vistas Razor y JavaScript cliente para selección de asientos y generación de archivos PDF.

Características clave
---------------------
- Carga inicial de eventos desde JSON: [BigFourApp/Data/JsonLoader.cs](BigFourApp/Data/JsonLoader.cs)
- Seeder que crea asientos según el layout del venue: [BigFourApp/Data/SeatSeeder.cs](BigFourApp/Data/SeatSeeder.cs)
- Contexto EF Core: [`BigFourApp.Persistence.BaseDatos`](BigFourApp/Data/BaseDatos.cs)
- Controladores principales:
  - [`BigFourApp.Controllers.HomeController`](BigFourApp/Controllers/HomeController.cs)
  - [`BigFourApp.Controllers.EventsController`](BigFourApp/Controllers/EventsController.cs)
  - [`BigFourApp.Controllers.SeatController`](BigFourApp/Controllers/SeatController.cs)
  - [`BigFourApp.Controllers.CheckoutController`](BigFourApp/Controllers/CheckoutController.cs)
- ViewModel para selección de asientos: [`BigFourApp.Models.ViewModels.SeatSelectionViewModel`](BigFourApp/Models/SeatSelection/SeatSelectionViewModel.cs)
- Vistas relevantes:
  - [Views/SeatSelection/Seats.cshtml](BigFourApp/Views/SeatSelection/Seats.cshtml)
  - [Views/Checkout/Cart.cshtml](BigFourApp/Views/Checkout/Cart.cshtml)
  - [Views/Checkout/Payment.cshtml](BigFourApp/Views/Checkout/Payment.cshtml)
  - [Views/Checkout/Receipt.cshtml](BigFourApp/Views/Checkout/Receipt.cshtml)
- Archivos JSON de ejemplo:
  - [BigFourApp/eventosDiluidos.json](BigFourApp/eventosDiluidos.json)
  - [BigFourApp/eventos.json](BigFourApp/eventos.json)

Requisitos
---------
- .NET 9 SDK
- dotnet-ef (opcional, para migraciones manuales)

Notas
-----
- La carga de eventos desde JSON y la creación de asientos se realiza automáticamente al iniciar la aplicación por primera vez.
- "Create Account" en la barra superior de la página por el momento no dirige a una página en concreto, actúa como un placeholder.
