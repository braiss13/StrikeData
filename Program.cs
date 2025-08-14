using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Services.TeamData;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// TODO: Borrar o comentar este bloque de código una vez que se haya validado el scraper.
// --- Bloque de prueba temporal para el scraper ---
// Solo se ejecutará una vez al arrancar la aplicación.
// Puedes eliminarlo o comentarlo cuando hayas validado los resultados.
{
    using var httpClient = new HttpClient();
    var scraper = new TeamScheduleScraper(httpClient);

    try
    {
        // Cambia "BAL" y 2024 por el equipo y año que desees probar
        var result = await scraper.GetTeamScheduleAndSplitsAsync("TOR", 2024);

        Console.WriteLine("Primeros 5 partidos del calendario:");
        foreach (var game in result.Schedule.Take(5))
        {
            Console.WriteLine($"{game.GameNumber}. {game.Date:yyyy-MM-dd} vs {game.Opponent} " + $"{game.Score} ({game.Decision}) Record: {game.Record}");
        }

        Console.WriteLine("\nSplits mensuales:");
        foreach (var ms in result.MonthlySplits)
        {
            Console.WriteLine($"{ms.Month}: {ms.Games} juegos, {ms.Won}-{ms.Lost}, WP {ms.WinPercentage}");
        }

        Console.WriteLine("\nTeam vs Team (primeros 5):");
        foreach (var vs in result.TeamSplits.Take(5))
        {
            Console.WriteLine($"{vs.Opponent}: {vs.Games} juegos, {vs.Won}-{vs.Lost}, WP {vs.WinPercentage}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error al ejecutar el scraper: " + ex.Message);
    }
}
// --- Fin del bloque de prueba temporal ---

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
