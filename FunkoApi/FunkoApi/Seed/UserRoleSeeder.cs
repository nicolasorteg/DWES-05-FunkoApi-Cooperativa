using FunkoApi.Models;
using Microsoft.AspNetCore.Identity;

namespace FunkoApi.Seed;

public static class UserRoleSeeder
{
    public static async Task SeedIdentityAsync(IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("Iniciando la siembra de seguridad (Roles y Usuarios)...");

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<long>>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        // crear roles si no existen
        await EnsureRolesExistAsync(roleManager, logger);
        // crea users por defecto
        await EnsureDefaultUsersExistAsync(userManager, logger);
    }

    private static async Task EnsureRolesExistAsync(RoleManager<IdentityRole<long>> roleManager, ILogger logger)
    {
        string[] rolesRequeridos = ["ADMIN", "USER"];

        foreach (var rol in rolesRequeridos)
        {
            if (!await roleManager.RoleExistsAsync(rol)) {
                await roleManager.CreateAsync(new IdentityRole<long>(rol));
            }
        }
    }

    private static async Task EnsureDefaultUsersExistAsync(UserManager<AppUser> userManager, ILogger logger)
    {
        // admin
        await CreateUserIfNotExistsAsync(
            userManager,
            logger,
            email: "admin@admin.com",
            password: "Admin123@",
            nombre: "Administrador",
            apellido: "Principal",
            role: "ADMIN"
        );

        // user normal
        await CreateUserIfNotExistsAsync(
            userManager,
            logger,
            email: "user@user.com",
            password: "User123!",
            nombre: "Usuario",
            apellido: "Estándar",
            role: "USER"
        );
    }

    private static async Task CreateUserIfNotExistsAsync(
        UserManager<AppUser> userManager,
        ILogger logger,
        string email,
        string password,
        string nombre,
        string apellido,
        string role)
    {
        var usuarioExistente = await userManager.FindByEmailAsync(email);
        if (usuarioExistente is not null) return;

        var nuevoUsuario = new AppUser
        {
            UserName = email,
            Email = email,
            Nombre = nombre,
            Apellidos = apellido,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true // Evita bloqueos si la confirmación de email estuviese activa
        };

        var resultadoCreacion = await userManager.CreateAsync(nuevoUsuario, password);

        if (resultadoCreacion.Succeeded) {
            await userManager.AddToRoleAsync(nuevoUsuario, role);
        } else {
            string.Join(" | ", resultadoCreacion.Errors.Select(e => e.Description));
        }
    }
}