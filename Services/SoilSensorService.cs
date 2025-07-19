using SoilSensorCapture.Models;

namespace SoilSensorCapture.Services
{
    // Services/SoilSensorService.cs - 重構為使用 MQTT
    public class SoilSensorService
    {
        private readonly MqttService _mqttService;
        private readonly ILogger<SoilSensorService> _logger;

        public SoilSensorService(MqttService mqttService, ILogger<SoilSensorService> logger)
        {
            _mqttService = mqttService;
            _logger = logger;
        }

        // 取得最新的土壤數據 (從 MQTT 服務獲取)
        public async Task<SoilData?> GetSoilDataAsync()
        {
            try
            {
                var latestData = _mqttService.GetLatestSoilData();
                if (latestData != null)
                {
                    return latestData;
                }

                // 如果沒有最新數據，請求即時讀數
                await _mqttService.SendCommandAsync("GET_READING");

                // 等待一段時間讓感測器回應
                await Task.Delay(1000);

                return _mqttService.GetLatestSoilData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得土壤數據時發生錯誤");
                return null;
            }
        }

        // GPIO 控制 (透過 MQTT 指令)
        public async Task<bool> ControlGPIOAsync(bool state)
        {
            try
            {
                string command = state ? "GPIO_ON" : "GPIO_OFF";
                return await _mqttService.SendCommandAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GPIO 控制失敗: {state}");
                return false;
            }
        }

        // 澆水操作 (透過 MQTT 服務)
        public async Task<bool> WaterPlantAsync()
        {
            try
            {
                return await _mqttService.WaterPlantAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "澆水操作失敗");
                return false;
            }
        }

        // 取得系統狀態
        public async Task<bool> GetSystemStatusAsync()
        {
            try
            {
                return await _mqttService.SendCommandAsync("GET_STATUS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得系統狀態失敗");
                return false;
            }
        }

        // 取得歷史數據
        public List<SoilData> GetHistoricalData(TimeSpan? timeRange = null)
        {
            try
            {
                return _mqttService.GetHistoricalData(timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得歷史數據失敗");
                return new List<SoilData>();
            }
        }
    }

    // 保留原有的 GPIOResponse 類別以保持相容性
    public class GPIOResponse
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public bool state { get; set; }
    }
}