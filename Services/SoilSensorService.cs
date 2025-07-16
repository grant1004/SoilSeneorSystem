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
        private readonly List<WateringRecord> _wateringRecords;
        private SoilData _lastSoilData;
        private bool _autoWateringEnabled = false;
        private DateTime _lastAutoWateringTime = DateTime.MinValue;
        private readonly float _moistureThreshold = 30.0f; // 30% 濕度閾值
        private readonly int _autoWateringCooldownMinutes = 30; // 30分鐘冷卻時間

        public SoilSensorService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _wateringRecords = new List<WateringRecord>();

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
                var currentData = await response.Content.ReadFromJsonAsync<SoilData>();
                
                // 檢測澆水行為
                if (currentData != null)
                {
                    CheckForWateringActivity(currentData);
                    
                    // 檢查是否需要自動澆水
                    await CheckAutoWateringAsync(currentData);
                    
                    _lastSoilData = currentData;
                }
                
                return currentData;
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
                // 記錄澆水前的濕度
                var beforeMoisture = _lastSoilData?.Moisture ?? 0;
                
                // 開啟水閥
                Debug.WriteLine($"開啟水閥");
                await ControlGPIOAsync(true);

                // 等待1秒
                await Task.Delay(1000);

                // 關閉水閥
                var result = await ControlGPIOAsync(false);
                
                // 記錄手動澆水
                if (result)
                {
                    var wateringRecord = new WateringRecord
                    {
                        WateringTime = DateTime.Now,
                        MoistureBefore = beforeMoisture,
                        MoistureAfter = 0, // 將在下次讀取時更新
                        MoistureChange = 0,
                        Type = WateringType.Manual
                    };
                    
                    _wateringRecords.Add(wateringRecord);
                    Debug.WriteLine($"記錄手動澆水: {wateringRecord.WateringTime}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Watering Error: {ex.Message}");
                return false;
            }
        }

        // 檢測澆水活動
        private void CheckForWateringActivity(SoilData currentData)
        {
            if (_lastSoilData == null) return;
            
            var moistureChange = currentData.Moisture - _lastSoilData.Moisture;
            const float wateringThreshold = 10.0f; // 濕度變化閾值
            
            // 如果濕度大幅增加，判定為澆水行為
            if (moistureChange >= wateringThreshold)
            {
                // 檢查是否是手動澆水後的更新
                var lastManualWatering = _wateringRecords
                    .Where(r => r.Type == WateringType.Manual && r.MoistureAfter == 0)
                    .OrderByDescending(r => r.WateringTime)
                    .FirstOrDefault();
                
                if (lastManualWatering != null && 
                    DateTime.Now - lastManualWatering.WateringTime <= TimeSpan.FromMinutes(5))
                {
                    // 更新手動澆水記錄
                    lastManualWatering.MoistureAfter = currentData.Moisture;
                    lastManualWatering.MoistureChange = moistureChange;
                    Debug.WriteLine($"更新手動澆水記錄: 濕度變化 {moistureChange}%");
                }
                else
                {
                    // 新增檢測到的澆水記錄
                    var wateringRecord = new WateringRecord
                    {
                        WateringTime = DateTime.Now,
                        MoistureBefore = _lastSoilData.Moisture,
                        MoistureAfter = currentData.Moisture,
                        MoistureChange = moistureChange,
                        Type = WateringType.Detected
                    };
                    
                    _wateringRecords.Add(wateringRecord);
                    Debug.WriteLine($"檢測到澆水行為: 濕度從 {_lastSoilData.Moisture}% 增加到 {currentData.Moisture}%");
                }
            }
        }

        // 檢查自動澆水
        private async Task CheckAutoWateringAsync(SoilData currentData)
        {
            if (!_autoWateringEnabled) return;
            
            // 檢查濕度是否低於閾值
            if (currentData.Moisture >= _moistureThreshold) return;
            
            // 檢查冷卻時間
            var timeSinceLastWatering = DateTime.Now - _lastAutoWateringTime;
            if (timeSinceLastWatering < TimeSpan.FromMinutes(_autoWateringCooldownMinutes))
            {
                Debug.WriteLine($"自動澆水冷卻中，還需等待 {_autoWateringCooldownMinutes - timeSinceLastWatering.TotalMinutes:F1} 分鐘");
                return;
            }
            
            Debug.WriteLine($"觸發自動澆水: 濕度 {currentData.Moisture}% < {_moistureThreshold}%");
            
            try
            {
                // 執行自動澆水
                var success = await PerformAutoWateringAsync(currentData.Moisture);
                
                if (success)
                {
                    _lastAutoWateringTime = DateTime.Now;
                    Debug.WriteLine($"自動澆水成功，下次可澆水時間: {_lastAutoWateringTime.AddMinutes(_autoWateringCooldownMinutes)}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"自動澆水錯誤: {ex.Message}");
            }
        }

        // 執行自動澆水
        private async Task<bool> PerformAutoWateringAsync(float beforeMoisture)
        {
            try
            {
                // 開啟水閥
                Debug.WriteLine("自動澆水: 開啟水閥");
                await ControlGPIOAsync(true);

                // 等待1秒
                await Task.Delay(1000);

                // 關閉水閥
                var result = await ControlGPIOAsync(false);
                
                // 記錄自動澆水
                if (result)
                {
                    var wateringRecord = new WateringRecord
                    {
                        WateringTime = DateTime.Now,
                        MoistureBefore = beforeMoisture,
                        MoistureAfter = 0, // 將在下次讀取時更新
                        MoistureChange = 0,
                        Type = WateringType.Automatic
                    };
                    
                    _wateringRecords.Add(wateringRecord);
                    Debug.WriteLine($"記錄自動澆水: {wateringRecord.WateringTime}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"自動澆水執行錯誤: {ex.Message}");
                return false;
            }
        }

        // 設定自動澆水狀態
        public void SetAutoWateringEnabled(bool enabled)
        {
            _autoWateringEnabled = enabled;
            Debug.WriteLine($"自動澆水功能: {(enabled ? "開啟" : "關閉")}");
        }

        // 取得自動澆水狀態
        public bool GetAutoWateringEnabled()
        {
            return _autoWateringEnabled;
        }

        // 取得自動澆水設定資訊
        public object GetAutoWateringSettings()
        {
            return new
            {
                enabled = _autoWateringEnabled,
                moistureThreshold = _moistureThreshold,
                cooldownMinutes = _autoWateringCooldownMinutes,
                lastAutoWateringTime = _lastAutoWateringTime,
                nextAvailableTime = _lastAutoWateringTime.AddMinutes(_autoWateringCooldownMinutes)
            };
        }

        // 取得澆水記錄
        public List<WateringRecord> GetWateringRecords()
        {
            return _wateringRecords
                .OrderByDescending(r => r.WateringTime)
                .Take(50) // 只返回最近50筆記錄
                .ToList();
        }

    }
    public class GPIOResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public bool state { get; set; }
    }
}
