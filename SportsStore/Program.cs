using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;
using SportsStore.Models;
using SportsStore.Services;
using SportsStore.Services.Messaging;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddHostedService<RabbitMQConsumer>();
builder.Services.AddSingleton<RabbitMQService>();
builder.Services.AddSingleton<OrderMemoryStore>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<PaymentworkflowService>();
builder.Services.AddScoped<ShippingService>();

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7201",
                "http://localhost:7201",
                "https://localhost:7200",
                "http://localhost:7200",
                "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var storeConn = builder.Configuration.GetConnectionString("SportsStoreConnection");
if (string.IsNullOrWhiteSpace(storeConn))
    throw new InvalidOperationException("Missing connection string: SportsStoreConnection");

builder.Services.AddDbContext<StoreDbContext>(opts =>
    opts.UseSqlServer(storeConn));

var identityConn = builder.Configuration.GetConnectionString("IdentityConnection");
if (string.IsNullOrWhiteSpace(identityConn))
    throw new InvalidOperationException("Missing connection string: IdentityConnection");

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(identityConn));

builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddScoped<Cart>(sp => SessionCart.GetCart(sp));
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppIdentityDbContext>();

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<IPaymentService, StripePaymentService>();

var stripeKey = builder.Configuration["Stripe:SecretKey"];
if (string.IsNullOrWhiteSpace(stripeKey))
    throw new InvalidOperationException("Missing Stripe:SecretKey (use user-secrets or environment variable).");

StripeConfiguration.ApiKey = stripeKey;

var app = builder.Build();

Log.Information("Starting SportsStore in {Environment}", app.Environment.EnvironmentName);

if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Error");
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

app.UseRouting();

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute("catpage", "{category}/Page{productPage:int}",
    new { Controller = "Home", action = "Index" });

app.MapControllerRoute("page", "Page{productPage:int}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapControllerRoute("category", "{category}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapControllerRoute("pagination", "Products/Page{productPage}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapDefaultControllerRoute();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/admin/{*catchall}", "/Admin/Index");

SeedData.EnsurePopulated(app);
IdentitySeedData.EnsurePopulated(app);

app.Run();
