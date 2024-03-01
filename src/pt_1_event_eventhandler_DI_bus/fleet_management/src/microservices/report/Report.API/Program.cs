using System.Reflection;
using Bus.Infra.Bus.RabbitMQ;
using Bus.Infra.IoC;
using MediatR;
using Report.Domain.CommandHandlers;
using Report.Domain.Commands;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddTransient<IRequestHandler<ReportOccurenceCommand>, ReportOcurrenceCommandHandler>();

var settings = builder.Configuration.GetSection("RabbitMQSettings").Get<RabbitMQSettings>();
builder.Services.RegisterRabbitMQBus(settings!);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseHttpsRedirection();

app.Run();