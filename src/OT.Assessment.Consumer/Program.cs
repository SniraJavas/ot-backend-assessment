using OT.Assessment.Consumer;
using OT.Assessment.Consumer.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddConsumerServices();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();