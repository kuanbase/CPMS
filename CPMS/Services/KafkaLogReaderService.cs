using Confluent.Kafka;

namespace CPMS.Services;

public class KafkaLogReaderService
{
    private string _bootstrapServers;
    private string _topic;
    
    public KafkaLogReaderService(string bootstrapServers, string topic)
    {
        _bootstrapServers = bootstrapServers;
        _topic = topic;
    }
    
    public List<string> ReadLogs()
    {
        var logs = new List<string>();
        
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
        {
            consumer.Subscribe(_topic);

            try
            {
                while (true)
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1));

                    if (consumeResult == null)
                    {
                        break;
                    }

                    logs.Add(consumeResult.Message.Value);

                    Console.WriteLine($"Received log message: {consumeResult.Message.Value}");
                }
            }
            catch (OperationCanceledException)
            {
                consumer.Close();
            }

            return logs;
        }
    }
    
    // 異步版本
    public async Task<List<string>> ReadAllLogsAsync()
    {
        return await Task.Run(() => ReadLogs());
    }
    
    public async Task<LogResponse> GetRecentLogsAsync(
        int limit = 10, 
        long lastOffset = 0)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest, // 从最新位置开始
            EnableAutoOffsetStore = false,
            EnableAutoCommit = false,
            //MaxPollIntervalMs = 1000,
            //SessionTimeoutMs = 6000  // 会话超时时间
        };

        var logs = new List<string>();
        long nextOffset = 0;

        using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
        {
            consumer.Assign(new TopicPartition(_topic, 0)); // 指定分区
            consumer.Subscribe(_topic);

            if (lastOffset != 0)
            {
                consumer.Seek(new TopicPartitionOffset(
                    _topic,
                    0,
                    lastOffset
                ));
            }

            var timeout = TimeSpan.FromSeconds(30);
            
            for (int i = 0; i < limit; i++)
            {
                var result = consumer.Consume(timeout);
                if (result == null) break;

                logs.Add(result.Message.Value);
                nextOffset = result.Offset.Value;
            }
        }

        return new LogResponse
        {
            Logs = logs,
            NextOffset = nextOffset
        };
    }
    
    // 响应结构
    public class LogResponse
    {
        public List<string>? Logs { get; set; }
        public long NextOffset { get; set; }
    }
}