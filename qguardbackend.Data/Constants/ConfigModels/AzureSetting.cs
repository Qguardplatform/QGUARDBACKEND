namespace SISService.Core.ConfigModels
{
    public class AzureSetting
    {
        public string BlobContainerName { get; set; }
        public string BlobConnectionString { get; set; }
        public string CDN { get; set; }
        public string QueueConnectionString { get; set; }
        public string ExamEmailQueue { get; set; }
    }
    public class AzureQueueSettings
    {
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
        public string DeadLetterQueueName { get; set; }
        public string EventCallBackKey { get; set; }
    }
}