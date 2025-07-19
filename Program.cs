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

// 設定應用程式監聽所有介面
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 設定 SignalR Hub 路由
app.MapHub<SoilDataHub>("/soilDataHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();