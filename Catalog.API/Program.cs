using Catalog.API.Data;
using Catalog.API.Integration;
using Catalog.API.Integration.ItemEvents;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CatalogContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<RabbitMqExchange>(builder.Configuration.GetSection("RabbitMqExchange"));
builder.Services.AddSingleton<IEventsPublisher, EventsPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

try
{

    //registering the publisher
    app.Lifetime.ApplicationStarted.Register(() => {
        var eventsPublisher = app.Services.GetService<IEventsPublisher>();
        eventsPublisher.Start();
    });
    app.Lifetime.ApplicationStopping.Register(() => {
        var eventsPublisher = app.Services.GetService<IEventsPublisher>();
        eventsPublisher.Stop();
    });

}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error while connecting to RabbitMq.");

}

app.Run();
