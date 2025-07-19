using SoilSensorCapture.Hubs;
using SoilSensorCapture.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 新增 SignalR 服務
builder.Services.AddSignalR();

// 註冊 MQTT 服務為 Hosted Service
builder.Services.AddSingleton<MqttService>();
builder.Services.AddHostedService<MqttService>(provider => provider.GetService<MqttService>()!);

// 註冊 SoilSensorService
builder.Services.AddScoped<SoilSensorService>();

// Railway 動態 PORT 配置
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
var url = $"http://0.0.0.0:{port}";

Console.WriteLine($"🚀 Starting server on {url}");
builder.WebHost.UseUrls(url);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 🔧 只保留健康檢查端點，不覆蓋根路徑
app.MapGet("/health", () => "OK");
app.MapGet("/api/status", () => new {
    status = "healthy",
    timestamp = DateTime.UtcNow,
    port = port,
    environment = app.Environment.EnvironmentName
});

// 設定 SignalR Hub 路由
app.MapHub<SoilDataHub>("/soilDataHub");

// MVC 路由 - 這會處理根路徑 "/"
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine("✅ Application configured successfully");
app.Run();