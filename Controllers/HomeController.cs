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

                return Json(new { error = "無法取得數據" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetData 發生錯誤");
                return Json(new { error = "系統錯誤" });
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
                _logger.LogError(ex, "WaterPlant 發生錯誤");
                return Json(new { success = false, error = "澆水失敗" });
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
                _logger.LogError(ex, "ControlGPIO 發生錯誤");
                return Json(new { success = false, error = "GPIO 控制失敗" });
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
                _logger.LogError(ex, "GetStatus 發生錯誤");
                return Json(new { success = false, error = "取得狀態失敗" });
            }
        }
    }

    public class ControlRequest
    {
        public bool State { get; set; }
    }
}