using Banking.Application;
using Banking.Domain.CommandHandlers;
using Banking.Domain.Commands;
using Banking.Domain.Interfaces;
using Banking.Infra.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MSRabbitMQ.Infra.Bus;
using MSRabbitMQ.Infra.IoC;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<BankingDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("BankingDbConnection")));

builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQSettings"));

builder.Services.RegisterServices(builder.Configuration);

builder.Services.AddTransient<IAccountService, AccountService>();
builder.Services.AddTransient<IAccountRepository, AccountRepository>();
builder.Services.AddTransient<BankingDbContext>();
builder.Services.AddTransient<IRequestHandler<CreateTransferCommand, bool>, TransferCommandHandler>(); 

builder.Services.AddCors(opt => opt.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
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

