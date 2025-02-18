using CounterStrikeSharp.API;
using StackExchange.Redis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CS2_SimpleAdmin_RedisInform;

public class RedisSubscriber(string serverIdentifier)
{
    private ConnectionMultiplexer? _connection;
    private ISubscriber? _subscriber;

    public bool IsRunning;

    private async Task InitializeAsync()
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { CS2_SimpleAdmin_RedisInform.Instance.Config.RedisConnectionString },
            Password = CS2_SimpleAdmin_RedisInform.Instance.Config.RedisPassword,
            AbortOnConnectFail = false,
            ConnectRetry = 5, 
            ReconnectRetryPolicy = new LinearRetry(1000)
        };
        
        _connection = await ConnectionMultiplexer.ConnectAsync(options);
        _subscriber = _connection.GetSubscriber();
    }

    private async Task StartSubscriberAsync()
    {
        if (_subscriber == null)
            throw new InvalidOperationException("Failed to initialize redis subscriber.");
        
        IsRunning = true;
        
        try
        {
            await _subscriber.SubscribeAsync(RedisChannel.Literal("cs2-simpleadmin_events1"),
                (_, message) =>
                {
                    var parts = message.ToString().Split([':'], 2);
                    if (parts.Length < 2)
                        return;
                    
                    var senderMachineId = parts[0];
                    if (senderMachineId == serverIdentifier)
                        return;
                    
                    var deserializedMessage = JsonConvert.DeserializeObject<dynamic>(parts[1]);
                    if (deserializedMessage == null)
                        return;
                    
                    Server.NextFrame(() => CS2_SimpleAdmin_RedisInform.SharedApi?.ShowAdminActivity(deserializedMessage.MessageKey.ToString(), deserializedMessage.CallerName.ToString(), true, GetMessageArgsAsStringArray(deserializedMessage.MessageArgs)));
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task StopSubscriberAsync()
    {
        if (IsRunning && _subscriber != null) 
        {
            await _subscriber.UnsubscribeAllAsync();
            IsRunning = false;
        }
    }
    
    public async Task PublishMessageAsync(string message)
    {
        if (_subscriber == null)
            throw new InvalidOperationException("Subscriber not initialized.");

        var messageWithId = $"{serverIdentifier}:{message}";
        await _subscriber.PublishAsync(RedisChannel.Literal("cs2-simpleadmin_events"), messageWithId);
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await StopSubscriberAsync();
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
    
    public void Start()
    {
        Task.Run(async () =>
        {
            await InitializeAsync();
            await StartSubscriberAsync();
        });
    }
    
    private static string[]? GetMessageArgsAsStringArray(dynamic messageArgs)
    {
        if (messageArgs == null)
            return [];  // Return empty array if null

        return messageArgs switch
        {
            JArray jArray => jArray.ToObject<string[]>(),
            IEnumerable<object> enumerable => enumerable.Select(arg => arg?.ToString() ?? string.Empty).ToArray(),
            _ => [messageArgs.ToString()]
        };
    }
}