using OT.Assessment.Consumer;
using OT.Assessment.Consumer.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(
        RabbitMqOptions.SectionName));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();