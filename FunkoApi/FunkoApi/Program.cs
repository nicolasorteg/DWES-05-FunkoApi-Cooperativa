using FunkoApi.Configuration;
using FunkoApi.Services.Email;

var builder = WebApplication.CreateBuilder(args);

// --- REGISTRO DE SERVICIOS (Antes de builder.Build()) ---
builder.Services.AddOpenApi();

// Configuración y registro del servicio de email
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();

// --- CONSTRUCCIÓN DE LA APLICACIÓN ---
var app = builder.Build();

// --- CONFIGURACIÓN DEL PIPELINE DE HTTP Y ENDPOINTS ---
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[] {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () => {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary) {
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}