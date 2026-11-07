
using OT.Assessment.App.Data;
using OT.Assessment.App.Messaging;
using OT.Assessment.App.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Controllers and API documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// RabbitMQ
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddScoped<
    ICasinoWagerRepository,
    CasinoWagerRepository>();

builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddSingleton<
    IDbConnectionFactory,
    SqlConnectionFactory>();

var app = builder.Build();

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
