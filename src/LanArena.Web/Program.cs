using LanArena.Application.Features.PublicSite.GetPublicTournament;
using LanArena.Application.Features.PublicSite.RegisterCod4Team;
using LanArena.Application.Features.PublicSite.RegisterNfsRacer;
using LanArena.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/admin/login";
    options.Cookie.Name = "SHArena.Admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<GetPublicTournamentHandler>();
builder.Services.AddScoped<RegisterCod4TeamHandler>();
builder.Services.AddScoped<RegisterNfsRacerHandler>();
var connectionString = builder.Configuration.GetConnectionString("SHArena");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:SHArena using user-secrets or the ConnectionStrings__SHArena environment variable.");
builder.Services.AddLanArenaInfrastructure(connectionString);

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<SqlPublicTournamentStore>();
    await store.InitializeAsync(Path.Combine(app.Environment.ContentRootPath, "App_Data", "tournament-public.json"));
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute("default", "{controller=PublicSite}/{action=Index}/{id?}").WithStaticAssets();
app.Run();
