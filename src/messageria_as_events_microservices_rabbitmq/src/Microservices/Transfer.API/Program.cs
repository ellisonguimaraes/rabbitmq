using Microsoft.EntityFrameworkCore;
using MSRabbitMQ.Infra.Bus;
using Transfer.Infra.Data;
using MSRabbitMQ.Infra.IoC;
using MSRabbitMQ.Domain.Core.Bus;
using Transfer.Domain;
using Transfer.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TransferDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("TransferDbConnection")));

builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQSettings"));

builder.Services.RegisterServices(builder.Configuration);

builder.Services.AddTransient<ITransferService, TransferService>();
builder.Services.AddTransient<ITransferRepository, TransferRepository>();
builder.Services.AddTransient<TransferDbContext>();
builder.Services.AddTransient<IEventHandler<TransferCreatedEvent>, TransferEventHandler>();

//Subscriptions
builder.Services.AddTransient<TransferEventHandler>();

builder.Services.AddCors(opt => opt.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

var eventBus = app.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<TransferCreatedEvent, TransferEventHandler>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");

app.MapControllers();

app.UseHttpsRedirection();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
