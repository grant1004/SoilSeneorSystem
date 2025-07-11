using SoilSensorCapture.Models;
using System.Diagnostics;
using System.Text.Json;

namespace SoilSensorCapture.Services
{
    // Services/SoilSensorService.cs
    public class SoilSensorService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _gpioControlUrl;

        public SoilSensorService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();

            // 使用主機名稱而不是 IP
            var baseUrl = configuration["SoilSensor:BaseUrl"] ?? "http://soil-sensor-pi.local:8080";
            _apiUrl = $"{baseUrl}/api/soil-data";
            _gpioControlUrl = $"{baseUrl}/api/soil-data/gpio/control";
        }

        public async Task<SoilData> GetSoilDataAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SoilData>();
            }
            catch (Exception ex)
            {
                // 在實際應用中應該適當處理錯誤
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ControlGPIOAsync(bool state)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { state = state }),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(_gpioControlUrl, content);
                response.EnsureSuccessStatusCode();

                Debug.WriteLine( $"Response Content : {response.Content} ");

                var result = await response.Content.ReadFromJsonAsync<GPIOResponse>();
                return result?.success ?? false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GPIO Control Error: {ex.Message}");
                return false;
            }
        }

        // 新增澆水方法（開啟1秒後關閉）
        public async Task<bool> WaterPlantAsync()
        {
            try
            {
                // 開啟水閥
                Debug.WriteLine($"開啟水閥");
                await ControlGPIOAsync(true);

                // 等待1秒
                await Task.Delay(1000);

                // 關閉水閥
                return await ControlGPIOAsync(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Watering Error: {ex.Message}");
                return false;
            }
        }

    }
    public class GPIOResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public bool state { get; set; }
    }
}
