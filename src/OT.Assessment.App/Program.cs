using OT.Assessment.App.Messaging;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddControllers();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var rabbitConfig = builder.Configuration.GetSection("RabbitMq");

Console.WriteLine($"RabbitMQ Host: {rabbitConfig["HostName"]}");
Console.WriteLine($"RabbitMQ Port: {rabbitConfig["Port"]}");
Console.WriteLine($"RabbitMQ User: {rabbitConfig["UserName"]}");
Console.WriteLine($"RabbitMQ VHost: {rabbitConfig["VirtualHost"]}");

var app = builder.Build();

// Configure the HTTP request pipeline.
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
