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
// --- BLOQUE TEMPORAL DE TEST: imprimir TODO y validar coherencia ---
{
    using var httpClient = new HttpClient();
    var scraper = new TeamScheduleScraper(httpClient);

    var team = "TOR";
    var year = 2025;

    var r = await scraper.GetTeamScheduleAndSplitsAsync(team, year);

    // 1) RESUMEN
    Console.WriteLine($"== {team} {year} ==");
    Console.WriteLine($"Schedule: {r.Schedule.Count} juegos");
    Console.WriteLine($"MonthlySplits: {r.MonthlySplits.Count} filas");
    Console.WriteLine($"TeamSplits: {r.TeamSplits.Count} filas");
    Console.WriteLine();

    // 2) IMPRIMIR TODO: SCHEDULE COMPLETO
    Console.WriteLine("== SCHEDULE (TODOS) ==");
    foreach (var g in r.Schedule.OrderBy(x => x.GameNumber))
        Console.WriteLine($"{g.GameNumber,3}. {g.Date:yyyy-MM-dd} {g.Opponent}  {g.Score} ({g.Decision})  Record: {g.Record}");
    Console.WriteLine();

    // 3) IMPRIMIR TODO: MONTHLY SPLITS
    Console.WriteLine("== MONTHLY SPLITS (TODOS) ==");
    foreach (var m in r.MonthlySplits)
        Console.WriteLine($"{m.Month,-10}  {m.Games,2} juegos  {m.Won}-{m.Lost}  WP {m.WinPercentage.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    Console.WriteLine();

    // 4) IMPRIMIR TODO: TEAM vs TEAM SPLITS
    Console.WriteLine("== TEAM vs TEAM SPLITS (TODOS) ==");
    foreach (var t in r.TeamSplits.OrderBy(x => x.Opponent))
        Console.WriteLine($"{t.Opponent,-22}  {t.Games,2} juegos  {t.Won}-{t.Lost}  WP {t.WinPercentage.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    Console.WriteLine();

    // 5) CHECKS DE COHERENCIA ÚTILES
    int scheduleGames = r.Schedule.Count;

    int monthlyGames = r.MonthlySplits.Sum(x => x.Games);
    Console.WriteLine($"[CHECK] Suma Monthly (={monthlyGames}) {(monthlyGames == scheduleGames ? "==" : "!=")} Schedule ({scheduleGames})");

    int teamGames = r.TeamSplits.Sum(x => x.Games);
    Console.WriteLine($"[CHECK] Suma Team vs Team (={teamGames}) {(teamGames == scheduleGames ? "==" : "!=")} Schedule ({scheduleGames})");

    // Duplicados en TeamSplits
    var dupOpp = r.TeamSplits.GroupBy(x => x.Opponent).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
    if (dupOpp.Count > 0)
        Console.WriteLine("[WARN] Oponentes duplicados: " + string.Join(", ", dupOpp));

    // Meses inválidos o incoherentes
    var badMonths = r.MonthlySplits.Where(x => x.Won + x.Lost != x.Games).ToList();
    if (badMonths.Count > 0)
        Console.WriteLine("[WARN] Filas mensuales incoherentes: " + string.Join(", ", badMonths.Select(x => x.Month)));

    Console.WriteLine("== FIN TEST ==");
}
// --- FIN BLOQUE TEMPORAL DE TEST ---

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
