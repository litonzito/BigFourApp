using BigFourApp.Data;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);




builder.Services.AddDbContext<BaseDatos>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnectionString")));
//builder.Services.AddScoped<IEventRepository, EventRepository>();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddHostedService<NotificationBackgroundService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<BaseDatos>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
});

//i moved it down there, it stopped working on my last test for some reason when it was at the top ?  ? dont touch it anymore 
builder.Services.AddRazorPages();
// Add services to the container.
builder.Services.AddControllersWithViews();


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

async Task SeedRolesAsync(IApplicationBuilder app)
{
    using var scope = app.ApplicationServices.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "User", "EventManager" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

await SeedRolesAsync(app);



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();


app.UseAuthorization();

app.MapRazorPages();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

