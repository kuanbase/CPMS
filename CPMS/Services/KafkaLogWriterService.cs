using Confluent.Kafka;
using Serilog;

namespace CPMS.Services;

public class KafkaLogWriterService
{
    private string _bootstrapServers;
    private string _topic;
    
    public KafkaLogWriterService(string bootstrapServers, string topic)
    {
        _bootstrapServers = bootstrapServers;
        _topic = topic;
    }

    public async Task<bool> WriteToKafkaLog(string message)
    {
        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                ClientId = $"{_topic}-{Guid.NewGuid().ToString()}"
            };

            using (var producer = new ProducerBuilder<Null, string>(config).Build())
            {
                var result = await producer.ProduceAsync(_topic, new Message<Null, string> { Value = message, Timestamp = Timestamp.Default});
                // 檢查消息傳遞狀態
                if (result.Status == PersistenceStatus.Persisted)
                {
                    Log.Information($"Message sent to {result.Topic} partition {result.Partition}");
                    return true;
                }
                else
                {
                    Log.Error($"Message not persisted. Status: {result.Status}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"{ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
}