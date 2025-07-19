using Microsoft.AspNetCore.Mvc;
using SoilSensorCapture.Services;

namespace SoilSensorCapture.Controllers
{
    public class HomeController : Controller
    {
        private readonly SoilSensorService _sensorService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(SoilSensorService sensorService, ILogger<HomeController> logger)
        {
            _sensorService = sensorService;
            _logger = logger;
        }

        public IActionResult Index() => View();
        public IActionResult Privacy() => View();
        public IActionResult Error() => View();

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var data = await _sensorService.GetSoilDataAsync();
                if (data != null)
                {
                    return Json(data.ToClientFormat());
                }

                return Json(new { error = "�L�k���o�ƾ�" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetData �o�Ϳ��~");
                return Json(new { error = "�t�ο��~" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> WaterPlant()
        {
            try
            {
                var success = await _sensorService.WaterPlantAsync();
                return Json(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WaterPlant �o�Ϳ��~");
                return Json(new { success = false, error = "�������" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ControlGPIO([FromBody] ControlRequest request)
        {
            try
            {
                var success = await _sensorService.ControlGPIOAsync(request.State);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ControlGPIO �o�Ϳ��~");
                return Json(new { success = false, error = "GPIO �����" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var success = await _sensorService.GetSystemStatusAsync();
                return Json(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStatus �o�Ϳ��~");
                return Json(new { success = false, error = "���o���A����" });
            }
        }

        [HttpGet]
        public IActionResult GetHistoricalData([FromQuery] int hours = 12)
        {
            try
            {
                var timeRange = TimeSpan.FromHours(Math.Min(hours, 24)); // 限制最大24小時
                var historicalData = _sensorService.GetHistoricalData(timeRange);
                
                var clientData = historicalData.Select(data => data.ToClientFormat()).ToList();
                
                return Json(new { 
                    success = true, 
                    data = clientData,
                    count = clientData.Count,
                    timeRange = hours
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHistoricalData 發生錯誤");
                return Json(new { success = false, error = "獲取歷史數據失敗" });
            }
        }
    }

    public class ControlRequest
    {
        public bool State { get; set; }
    }
}