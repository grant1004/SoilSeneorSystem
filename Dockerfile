# 使用官方 .NET 8.0 SDK 作為建置映像
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 複製專案檔並還原依賴項
COPY ["SoilSensorCapture.csproj", "."]
RUN dotnet restore "SoilSensorCapture.csproj"

# 複製所有原始碼並建置應用程式
COPY . .
WORKDIR "/src"
RUN dotnet build "SoilSensorCapture.csproj" -c Release -o /app/build

# 發佈應用程式
FROM build AS publish
RUN dotnet publish "SoilSensorCapture.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 使用官方 .NET 8.0 執行階段作為最終映像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 設定埠號環境變數
ENV ASPNETCORE_URLS=http://+:$PORT

# 暴露埠號
EXPOSE $PORT

ENTRYPOINT ["dotnet", "SoilSensorCapture.dll"]