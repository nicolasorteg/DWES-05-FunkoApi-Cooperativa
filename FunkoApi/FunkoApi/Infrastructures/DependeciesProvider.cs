using System.Text;
using FluentValidation;
using FunkoApi.Configuration;
using FunkoApi.Entity;
using FunkoApi.GraphQL;
using FunkoApi.Models;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Repositories.Proveedores;
using FunkoApi.Services.Auth;
using FunkoApi.Services.Background;
using FunkoApi.Services.Cache;
using FunkoApi.Services.Categoria;
using FunkoApi.Services.Email;
using FunkoApi.Services.Funkos;
using FunkoApi.Services.Proveedor;
using FunkoApi.Services.Proveedores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Driver;
using Path = System.IO.Path;
using HotChocolate.AspNetCore;

namespace FunkoApi.Infrastructures;

public static class DependenciesProvider
{
    public static IServiceCollection AddAllDependencies(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.AddDatabaseConfiguration(configuration);
        services.AddIdentityAndSecurity(configuration);
        services.AddCacheConfiguration(configuration, environment);
        services.AddApplicationServices(configuration);
        services.AddSwaggerDocumentation();

        return services;
    }

    // 1. Bases de datos (PostgreSQL con EF Core + MongoDB)
    private static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL + EF Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgresConnection")));

        // MongoDB client y database Context
        // MongoDB client y database Context
        services.AddSingleton<IMongoClient>(_ =>
            new MongoClient(configuration.GetConnectionString("MongoConnection")));

        var mongoDbName = configuration["MongoDatabase:DatabaseName"] ?? "funkos_mongo_db";

        services.AddDbContext<AppMongoDbContext>((sp, options) =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            options.UseMongoDB(client, mongoDbName);
        });

        return services;
    }

    // 2. Identity + Autenticación JWT
    private static IServiceCollection AddIdentityAndSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // ASP.NET Core Identity
        services.AddIdentity<AppUser, IdentityRole<long>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // Configuración JWT
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ValidateLifetime = true,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };

            options.Events = new JwtBearerEvents {
                OnMessageReceived = context => {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path; 
                    // Extrae el token si la petición va dirigida a un Hub de SignalR
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs")) {
                        context.Token = accessToken;
                    } 
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
        return services;
    }

    // 3. Caché (MemoryCache en desarrollo, Redis en producción)
    private static IServiceCollection AddCacheConfiguration(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:ConnectionString"] ?? "localhost:6379";
            });
            services.AddSingleton<ICacheService, RedisCacheService>();
        }

        return services;
    }

    // 4. Repositorios, Servicios y Validadores
    private static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositorios
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IFunkosRepository, FunkosRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();

        // Servicios de Dominio
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IFunkoService, FunkoService>();
        services.AddScoped<IProveedorService, ProveedorService>();
        services.AddScoped<IAuthService, AuthService>();
        
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailService, EmailService>();

        // Validadores de FluentValidation
        services.AddValidatorsFromAssemblyContaining<CategoriaService>();
        
        // tareas en segundo plano
        services.AddHostedService<GreetingBackgroundService>();

        return services;
    }

    // 5. Swagger / OpenAPI con soporte para el botón de Authorization (Bearer Jwt)
    private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddCors(options => { 
            options.AddDefaultPolicy(policy => { policy.SetIsOriginAllowed(_ => true) 
                // Permite conexioneslocales
                .AllowAnyHeader() 
                .AllowAnyMethod()
                .AllowCredentials(); // ¡Obligatorio para SignalR!
            }); 
        });
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Funko API",
                Version = "v1",
                Description = "API REST de Gestión de Funkos, Categorías y Proveedores"
            });

            // Configurar el botón "Authorize" para pegar el Token JWT en Swagger UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Introduce el token JWT obtenido en /api/auth/login"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

            // Cargar comentarios XML si existen
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
    
    public static IServiceCollection AddGraphQLConfiguration(this IServiceCollection services) { 
        services .AddGraphQLServer() 
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddAuthorization()
            .AddFiltering()
            .AddSorting();

        return services;
    }
}