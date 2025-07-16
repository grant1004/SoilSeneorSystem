// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using SoilSensorCapture.Services;

public class HomeController : Controller
{
    private readonly SoilSensorService _sensorService;

    public HomeController(SoilSensorService sensorService)
    {
        _sensorService = sensorService;
    }

    public IActionResult Index() => View();
    public IActionResult Privacy() => View();
    public IActionResult Error() => View();

    [HttpGet]
    public async Task<IActionResult> GetData()
    {
        var data = await _sensorService.GetSoilDataAsync();
        return Json(data);
    }

    [HttpPost]
    public async Task<IActionResult> WaterPlant()
    {
        var success = await _sensorService.WaterPlantAsync();
        return Json( new { success });
    }

    [HttpGet]
    public IActionResult GetWateringRecords()
    {
        var records = _sensorService.GetWateringRecords();
        return Json(records);
    }

}