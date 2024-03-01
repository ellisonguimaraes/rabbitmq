using Bus.Domain;
using Bus.Domain.Events;
using Bus.Infra.Bus.RabbitMQ;
using Bus.Infra.IoC;
using Ocurrences.Domain.EventHandlers;
using Ocurrences.Domain.Events;
using Ocurrences.Infra.Data;
using Ocurrences.Infra.Data.Context;
using Ocurrences.Infra.Data.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.RegisterRabbitMQBus(builder.Configuration.GetSection("RabbitMQSettings").Get<RabbitMQSettings>()!);

builder.Services.AddTransient<IEventHandler<ReportOccurenceEvent>, ReportOccurenceEventHandler>();

//Subscriptions
builder.Services.AddTransient<ReportOccurenceEventHandler>();

// Database
builder.Services.AddSqlite<ApplicationContext>(builder.Configuration.GetConnectionString("OcurrenceDb"));
builder.Services.AddTransient<IOcurrenceRepository, OcurrenceRepository>();

var app = builder.Build();

var bus = app.Services.GetRequiredService<IBus>();
bus.Subscribe<ReportOccurenceEvent, ReportOccurenceEventHandler>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseHttpsRedirection();

app.Run();

