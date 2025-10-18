using BigFourApp.Data;
using BigFourApp.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<BaseDatos>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnectionString")));
//builder.Services.AddScoped<IEventRepository, EventRepository>();

var app = builder.Build();
// Crear base de datos automaticamente (solo en desarrollo)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BaseDatos>();

    context.Database.Migrate(); // aplica migraciones pendientes
    if (!context.Events.Any()) // Verifica si la tabla Events esta vacia
    {
        JsonLoader.LoadEvents(context); // Llama al metodo que lee el JSON
    }

    // Genera asientos para los eventos que lo necesiten al arrancar la aplicacion.
    SeatSeeder.EnsureSeats(context);
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

