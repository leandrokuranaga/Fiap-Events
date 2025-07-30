namespace Fiap.Events.Infra.Common.Configuration
{
    public class ServiceBusOptions
    {
        public string QueueName { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; }
    }
}
