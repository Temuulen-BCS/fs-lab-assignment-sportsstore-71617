using Microsoft.EntityFrameworkCore;
using SportsStore.Models;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SportsStoreConnection")));

var host = builder.Build();
host.Run();