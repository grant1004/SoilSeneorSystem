using Newtonsoft.Json;

namespace SoilSensorCapture.Models
{
    // 土壤數據模型 - 對應 MQTT JSON 格式
    public class SoilData
    {
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonProperty("voltage")]
        public float Voltage { get; set; }

        [JsonProperty("moisture")]
        public float Moisture { get; set; }

        [JsonProperty("gpio_status")]
        public bool GpioStatus { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        // 轉換為前端需要的格式
        public object ToClientFormat()
        {
            // 將時間戳轉換為 Unix timestamp (秒)
            if (DateTime.TryParse(Timestamp, out DateTime dt))
            {
                long unixTimestamp = ((DateTimeOffset)dt).ToUnixTimeSeconds();
                return new
                {
                    voltage = Voltage,
                    moisture = Moisture,
                    timestamp = unixTimestamp,
                    gpio_status = GpioStatus
                };
            }

            return new
            {
                voltage = Voltage,
                moisture = Moisture,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                gpio_status = GpioStatus
            };
        }
    }

    // 系統狀態模型
    public class SystemStatus
    {
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonProperty("system")]
        public string System { get; set; } = string.Empty;

        [JsonProperty("last_command")]
        public string LastCommand { get; set; } = string.Empty;

        [JsonProperty("uptime")]
        public long Uptime { get; set; }

        [JsonProperty("voltage")]
        public float Voltage { get; set; }

        [JsonProperty("moisture")]
        public float Moisture { get; set; }

        [JsonProperty("gpio_status")]
        public bool GpioStatus { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
    }

    // 指令回應模型
    public class CommandResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}