# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8.0 ASP.NET Core MVC web application for monitoring soil sensor data and controlling plant watering systems. The application serves as a web interface to communicate with a remote Raspberry Pi soil sensor device.

## Architecture

The application follows a standard ASP.NET Core MVC pattern:

- **Controllers/HomeController.cs**: Main controller handling web requests and API endpoints
- **Services/SoilSensorService.cs**: Service layer for communicating with the remote soil sensor device via HTTP API
- **Models/SoilData.cs**: Data model representing soil sensor readings (voltage, moisture, timestamp)
- **Views/**: Razor views for the web interface

The application communicates with a remote soil sensor device (typically a Raspberry Pi) via HTTP APIs to:
- Retrieve soil moisture data
- Control GPIO pins for water valve operation
- Perform automated watering operations

## Development Commands

### Build and Run
```bash
# Build the application
dotnet build

# Run in development mode
dotnet run

# Run with hot reload
dotnet watch run
```

### Docker
```bash
# Build Docker image
docker build -t soil-sensor-app .

# Run container
docker run -p 5000:5000 soil-sensor-app
```

### Testing
```bash
# Run tests (if test project exists)
dotnet test
```

## Configuration

The application uses standard ASP.NET Core configuration:

- **appsettings.json**: Base configuration including SoilSensor:BaseUrl
- **appsettings.Development.json**: Development-specific overrides
- **Environment Variables**: PORT for Railway deployment, SoilSensor:BaseUrl can be overridden

## Key Components

### SoilSensorService
- Handles HTTP communication with the remote soil sensor device
- Default endpoint: `http://soil-sensor-pi.local:8080`
- Provides methods for data retrieval and GPIO control
- Includes automated watering functionality (1-second valve operation)

### API Endpoints
- `GET /Home/GetData`: Returns current soil sensor data as JSON
- `POST /Home/WaterPlant`: Triggers automated watering sequence

## Deployment

The application is configured for Railway deployment with:
- Dockerfile for containerization
- railway.json for deployment configuration
- PORT environment variable support
- Health check endpoint at root path

## External Dependencies

The application depends on a remote soil sensor device (Raspberry Pi) that should provide:
- `/api/soil-data` endpoint for sensor readings
- `/api/soil-data/gpio/control` endpoint for GPIO control

Connection is configured via the `SoilSensor:BaseUrl` configuration setting.