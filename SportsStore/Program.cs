using Serilog;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
);

builder.Services.AddControllersWithViews();

var storeConn = builder.Configuration.GetConnectionString("SportsStoreConnection");
if (string.IsNullOrWhiteSpace(storeConn))
    throw new InvalidOperationException("Missing connection string: SportsStoreConnection");

builder.Services.AddDbContext<StoreDbContext>(opts =>
{
    opts.UseSqlServer(storeConn);
});

builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();

builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddScoped<Cart>(sp => SessionCart.GetCart(sp));
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddServerSideBlazor();

var identityConn = builder.Configuration.GetConnectionString("IdentityConnection");
if (string.IsNullOrWhiteSpace(identityConn))
    throw new InvalidOperationException("Missing connection string: IdentityConnection");

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(identityConn));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppIdentityDbContext>();

var app = builder.Build();

Log.Information("Starting SportsStore in {Environment}", app.Environment.EnvironmentName);

if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/error");
}

app.UseRequestLocalization(opts =>
{
    opts.AddSupportedCultures("en-US")
        .AddSupportedUICultures("en-US")
        .SetDefaultCulture("en-US");
});

app.UseStaticFiles();

app.UseSerilogRequestLogging();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute("catpage",
    "{category}/Page{productPage:int}",
    new { Controller = "Home", action = "Index" });

app.MapControllerRoute("page", "Page{productPage:int}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapControllerRoute("category", "{category}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapControllerRoute("pagination",
    "Products/Page{productPage}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapDefaultControllerRoute();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/admin/{*catchall}", "/Admin/Index");

SeedData.EnsurePopulated(app);
IdentitySeedData.EnsurePopulated(app);

app.Run();