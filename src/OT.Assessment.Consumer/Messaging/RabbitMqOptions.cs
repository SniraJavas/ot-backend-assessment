namespace OT.Assessment.Consumer.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "otuser";
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public string QueueName { get; set; } = "ot.assessment.queue";
}