using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Data;
using ConcertTicketsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
    .LogTo(Console.WriteLine, LogLevel.Information));
builder.Services.AddScoped<TicketSalesService>();
builder.Services.AddScoped<ConcertReportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await ConcertSeedData.EnsureSeededAsync(db);
}

app.Run();
