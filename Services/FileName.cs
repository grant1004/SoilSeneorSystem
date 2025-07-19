using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Server;
using Newtonsoft.Json;
using SoilSensorCapture.Hubs;
using SoilSensorCapture.Models;
using System.Collections.Generic;
using System.Text;

namespace SoilSensorCapture.Services
{
    public class MqttService : IHostedService, IDisposable
    {
        private readonly ILogger<MqttService> _logger;
        private readonly IHubContext<SoilDataHub> _hubContext;
        private readonly IConfiguration _configuration;
        private IMqttClient? _mqttClient;
        private MqttClientOptions? _options;

        // MQTT 主題常數
        private const string TOPIC_DATA = "soilsensorcapture/data";
        private const string TOPIC_COMMAND = "soilsensorcapture/command";
        private const string TOPIC_STATUS = "soilsensorcapture/status";
        private const string TOPIC_RESPONSE = "soilsensorcapture/response";

        // 最新的土壤數據
        private SoilData? _latestSoilData;
        private readonly object _dataLock = new object();

        public MqttService(
            ILogger<MqttService> logger,
            IHubContext<SoilDataHub> hubContext,
            IConfiguration configuration)
        {
            _logger = logger;
            _hubContext = hubContext;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("正在啟動 MQTT 服務...");

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            // 設定 MQTT 選項
            var brokerHost = _configuration["Mqtt:BrokerHost"] ?? "broker.hivemq.com";
            var brokerPort = _configuration.GetValue<int>("Mqtt:BrokerPort", 1883);
            var clientId = _configuration["Mqtt:ClientId"] ?? "soilsensorcapture_web";

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerHost, brokerPort)
                .WithClientId(clientId)
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .Build();

            // 設定事件處理器
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            try
            {
                await _mqttClient.ConnectAsync(_options, cancellationToken);
                _logger.LogInformation($"✅ 已連接到 MQTT Broker: {brokerHost}:{brokerPort}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 連接 MQTT Broker 失敗");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("正在停止 MQTT 服務...");

            if (_mqttClient?.IsConnected == true)
            {
                await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
            }

            _mqttClient?.Dispose();
            _logger.LogInformation("✅ MQTT 服務已停止");
        }

        private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
        {
            _logger.LogInformation("🔗 MQTT 客戶端已連接");

            // 訂閱主題
            var subscriptions = new[]
            {
                new MqttTopicFilterBuilder().WithTopic(TOPIC_DATA).WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Build(),
                new MqttTopicFilterBuilder().WithTopic(TOPIC_STATUS).WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Build(),
                new MqttTopicFilterBuilder().WithTopic(TOPIC_RESPONSE).WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Build()
            };

            //await _mqttClient!.SubscribeAsync(subscriptions);
            Subscribe(subscriptions);
            _logger.LogInformation($"📡 已訂閱主題: {TOPIC_DATA}, {TOPIC_STATUS}, {TOPIC_RESPONSE}");
        }

        public MqttClientSubscribeResult Subscribe(params MqttTopicFilter[] topicFilters)
        {
            ArgumentNullException.ThrowIfNull(topicFilters, nameof(topicFilters));
            MqttClientSubscribeOptions subscribeOptions = new();
            subscribeOptions.TopicFilters.AddRange(topicFilters);
            return _mqttClient.SubscribeAsync(subscribeOptions).Result;
        }

        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            _logger.LogWarning("🔌 MQTT 客戶端已斷線");

            // 如果不是正常斷線，嘗試重連
            if (!args.ClientWasConnected)
            {
                _logger.LogInformation("🔄 嘗試重新連接...");
                Task.Run(async () =>
                {
                    await Task.Delay(5000); // 等待 5 秒後重連
                    try
                    {
                        if (_mqttClient != null && !_mqttClient.IsConnected)
                        {
                            await _mqttClient.ConnectAsync(_options!);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "重新連接失敗");
                    }
                });
            }

            return Task.CompletedTask;
        }

        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            try
            {
                var topic = args.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

                _logger.LogDebug($"📨 收到 MQTT 訊息 [{topic}]: {payload}");

                switch (topic)
                {
                    case TOPIC_DATA:
                        await HandleSoilDataMessage(payload);
                        break;
                    case TOPIC_STATUS:
                        await HandleStatusMessage(payload);
                        break;
                    case TOPIC_RESPONSE:
                        await HandleResponseMessage(payload);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 MQTT 訊息時發生錯誤");
            }
        }

        private async Task HandleSoilDataMessage(string payload)
        {
            try
            {
                var soilData = JsonConvert.DeserializeObject<SoilData>(payload);
                if (soilData != null)
                {
                    lock (_dataLock)
                    {
                        _latestSoilData = soilData;
                    }

                    // 透過 SignalR 推送到前端
                    await _hubContext.Clients.All.SendAsync("ReceiveSoilData", soilData.ToClientFormat());
                    _logger.LogDebug($"📊 土壤數據已推送: 電壓={soilData.Voltage}V, 濕度={soilData.Moisture}%");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析土壤數據失敗");
            }
        }

        private async Task HandleStatusMessage(string payload)
        {
            try
            {
                var status = JsonConvert.DeserializeObject<SystemStatus>(payload);
                if (status != null)
                {
                    // 透過 SignalR 推送系統狀態
                    await _hubContext.Clients.All.SendAsync("ReceiveSystemStatus", status);
                    _logger.LogDebug($"📈 系統狀態已推送: {status.System}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析系統狀態失敗");
            }
        }

        private async Task HandleResponseMessage(string payload)
        {
            // 處理指令回應
            await _hubContext.Clients.All.SendAsync("ReceiveCommandResponse", payload);
            _logger.LogDebug($"💬 指令回應已推送: {payload}");
        }

        // 發送 MQTT 指令
        public async Task<bool> SendCommandAsync(string command)
        {
            if (_mqttClient?.IsConnected != true)
            {
                _logger.LogWarning("MQTT 客戶端未連接，無法發送指令");
                return false;
            }

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(TOPIC_COMMAND)
                    .WithPayload(command)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message);
                _logger.LogInformation($"📤 已發送 MQTT 指令: {command}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"發送 MQTT 指令失敗: {command}");
                return false;
            }
        }

        // 取得最新的土壤數據
        public SoilData? GetLatestSoilData()
        {
            lock (_dataLock)
            {
                return _latestSoilData;
            }
        }

        // 澆水操作 (開啟1秒後關閉)
        public async Task<bool> WaterPlantAsync()
        {
            try
            {
                _logger.LogInformation("🚿 開始澆水操作");

                // 開啟水閥
                bool onResult = await SendCommandAsync("GPIO_ON");
                if (!onResult) return false;

                // 等待 1 秒
                await Task.Delay(1000);

                // 關閉水閥
                bool offResult = await SendCommandAsync("GPIO_OFF");

                _logger.LogInformation($"🚿 澆水操作完成: 開啟={onResult}, 關閉={offResult}");
                return offResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "澆水操作失敗");
                return false;
            }
        }

        public void Dispose()
        {
            _mqttClient?.Dispose();
        }
    }
}