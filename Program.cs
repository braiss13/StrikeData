using Microsoft.EntityFrameworkCore;
using StrikeData.Data;

var builder = WebApplication.CreateBuilder(args);

// Register Razor Pages
builder.Services.AddRazorPages();

// Register EF Core DbContext (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// HTTP request pipeline configuration
if (!app.Environment.IsDevelopment())
{
    // Production-friendly error page + HSTS
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// No explicit auth/identity in this project yet, but keep the middleware in place
app.UseAuthorization();

app.MapRazorPages();

app.Run();
