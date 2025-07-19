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

// 🔧 修復：Railway 動態 PORT 配置
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
var url = $"http://0.0.0.0:{port}";

Console.WriteLine($"🚀 Starting server on {url}");
builder.WebHost.UseUrls(url);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // 🔧 修復：Railway 自動處理 SSL，移除 HSTS
    // app.UseHsts();
}

// 🔧 修復：Railway 自動處理 HTTPS，移除重定向
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 🔧 新增：健康檢查端點 (Railway 必需)
app.MapGet("/", () => "✅ Soil Sensor Monitoring System is running!");
app.MapGet("/health", () => "OK");
app.MapGet("/status", () => new {
    status = "healthy",
    timestamp = DateTime.UtcNow,
    port = port,
    environment = app.Environment.EnvironmentName
});

// 設定 SignalR Hub 路由
app.MapHub<SoilDataHub>("/soilDataHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine("✅ Application configured successfully");
app.Run();