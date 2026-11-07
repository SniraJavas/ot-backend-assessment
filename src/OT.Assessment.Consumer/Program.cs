using OT.Assessment.Consumer;
using OT.Assessment.Consumer.Data;
using OT.Assessment.Consumer.Messaging;
using OT.Assessment.Consumer.Repositories;
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<
    IDbConnectionFactory,
    SqlConnectionFactory>();

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(
        RabbitMqOptions.SectionName));

builder.Services.AddSingleton<
    IDbConnectionFactory,
    SqlConnectionFactory>();

builder.Services.AddScoped<
    IWagerRepository,
    WagerRepository>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();