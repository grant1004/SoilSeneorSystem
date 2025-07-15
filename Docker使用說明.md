# Docker 使用說明

這份文件詳細說明了土壤感測器系統的 Docker 容器化部署流程。

## Dockerfile 解析

### 多階段建置 (Multi-stage Build)

我們的 Dockerfile 採用多階段建置策略，能夠有效減少最終映像檔的大小：

#### 第一階段：建置環境 (Build Stage)
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
```
- 使用官方 .NET 8.0 SDK 作為建置基底映像
- 設定工作目錄為 `/src`

#### 相依性還原
```dockerfile
COPY ["SoilSensorCapture.csproj", "."]
RUN dotnet restore "SoilSensorCapture.csproj"
```
- 先複製專案檔案，利用 Docker 的層級快取機制
- 執行 `dotnet restore` 還原 NuGet 套件

#### 程式碼建置
```dockerfile
COPY . .
WORKDIR "/src"
RUN dotnet build "SoilSensorCapture.csproj" -c Release -o /app/build
```
- 複製所有原始碼到容器中
- 使用 Release 設定編譯專案

#### 第二階段：發布準備 (Publish Stage)
```dockerfile
FROM build AS publish
RUN dotnet publish "SoilSensorCapture.csproj" -c Release -o /app/publish /p:UseAppHost=false
```
- 建立發布版本，不包含原生執行檔主機

#### 第三階段：執行環境 (Runtime Stage)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
```
- 使用輕量化的 ASP.NET Core 執行時期映像
- 僅複製發布的應用程式檔案

#### 環境設定
```dockerfile
ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE $PORT
ENTRYPOINT ["dotnet", "SoilSensorCapture.dll"]
```
- 設定 ASP.NET Core 監聽所有 IP 位址
- 暴露動態埠號（適用於 Railway 等平台）
- 設定應用程式進入點

## Docker 指令操作

### 建置映像檔

```bash
# 基本建置
docker build -t soil-sensor-app .

# 指定標籤版本
docker build -t soil-sensor-app:v1.0.0 .

# 查看建置進度（詳細模式）
docker build --progress=plain -t soil-sensor-app .
```

### 執行容器

#### 基本執行
```bash
# 使用預設埠號 5000
docker run -p 5000:5000 soil-sensor-app

# 背景執行
docker run -d -p 5000:5000 --name soil-sensor soil-sensor-app
```

#### 環境變數設定
```bash
# 設定自訂埠號
docker run -p 8080:8080 -e PORT=8080 soil-sensor-app

# 設定土壤感測器設備 URL
docker run -p 5000:5000 \
  -e SoilSensor__BaseUrl="http://192.168.1.100:8080" \
  soil-sensor-app
```

#### 完整部署範例
```bash
docker run -d \
  --name soil-sensor-system \
  -p 5000:5000 \
  -e PORT=5000 \
  -e SoilSensor__BaseUrl="http://soil-sensor-pi.local:8080" \
  --restart unless-stopped \
  soil-sensor-app
```

### 容器管理

```bash
# 查看執行中的容器
docker ps

# 查看容器日誌
docker logs soil-sensor

# 即時監控日誌
docker logs -f soil-sensor

# 進入容器內部
docker exec -it soil-sensor /bin/bash

# 停止容器
docker stop soil-sensor

# 重新啟動容器
docker restart soil-sensor

# 移除容器
docker rm soil-sensor
```

## Docker Compose 部署

建立 `docker-compose.yml` 檔案以簡化部署：

```yaml
version: '3.8'

services:
  soil-sensor-web:
    build: .
    ports:
      - "5000:5000"
    environment:
      - PORT=5000
      - SoilSensor__BaseUrl=http://soil-sensor-pi.local:8080
    restart: unless-stopped
    depends_on:
      - soil-sensor-device
    networks:
      - soil-network

networks:
  soil-network:
    driver: bridge
```

使用 Docker Compose：
```bash
# 建置並啟動服務
docker-compose up -d

# 查看服務狀態
docker-compose ps

# 查看日誌
docker-compose logs -f

# 停止服務
docker-compose down
```

## 最佳化建議

### 1. 使用 .dockerignore
建立 `.dockerignore` 檔案以排除不必要的檔案：
```
bin/
obj/
.git/
.vs/
*.md
Dockerfile
.dockerignore
```

### 2. 快取最佳化
- 先複製專案檔案再複製原始碼
- 利用 Docker 的層級快取機制

### 3. 安全性考量
```bash
# 以非特權使用者執行
RUN adduser --disabled-password --gecos '' appuser
USER appuser
```

### 4. 健康檢查
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:$PORT/ || exit 1
```

## 疑難排解

### 常見問題

1. **埠號衝突**
   ```bash
   # 檢查埠號使用情況
   netstat -tulpn | grep :5000
   
   # 使用不同埠號
   docker run -p 8080:5000 soil-sensor-app
   ```

2. **記憶體不足**
   ```bash
   # 限制容器記憶體使用
   docker run -m 512m soil-sensor-app
   ```

3. **網路連線問題**
   ```bash
   # 檢查容器網路
   docker network ls
   docker inspect bridge
   ```

4. **日誌查看**
   ```bash
   # 查看詳細錯誤訊息
   docker logs --details soil-sensor
   ```

## 生產環境部署

### Railway 部署
Railway 會自動識別 Dockerfile 並建置部署：
1. 連接 GitHub 倉庫
2. 設定環境變數
3. 自動部署

### 其他雲端平台
- **Heroku**: 支援 Dockerfile 部署
- **Azure Container Instances**: 直接部署容器映像
- **AWS ECS**: 容器服務部署
- **Google Cloud Run**: 無伺服器容器部署

記得在生產環境中設定適當的環境變數和安全性設定。