using FunkoApi.Configuration;
using FunkoApi.Entity;
using FunkoApi.Hubs;
using FunkoApi.Infrastructures;
using FunkoApi.Seed;
using FunkoApi.Services.Email;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Registro de todos los servicios (BD, Identity/JWT, caché, repos, servicios, validadores, Swagger)
builder.Services.AddAllDependencies(builder.Configuration, builder.Environment);

// Controladores
builder.Services.AddControllers();

// Configuración y registro del servicio de email
builder.Services.AddSignalR();
builder.Services.AddGraphQLConfiguration();

var app = builder.Build();

// Swagger UI solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<FunkosHub>("/hubs/funkos").RequireAuthorization();
app.MapControllers();
app.MapGraphQL();

// Seed inicial de datos (categorías, proveedores, funkos, roles y usuarios por defecto)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mongo = scope.ServiceProvider.GetRequiredService<AppMongoDbContext>();

        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db, mongo, logger);
        await UserRoleSeeder.SeedIdentityAsync(scope.ServiceProvider, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo inicializar la base de datos. Revisa la conexión a PostgreSQL/MongoDB.");
    }
}

app.Run();