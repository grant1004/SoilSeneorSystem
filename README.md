# Soil Sensor System

A .NET 8.0 ASP.NET Core MVC web application for monitoring soil sensor data and controlling automated plant watering systems.

## Features

- **Real-time Soil Monitoring**: View live soil moisture and voltage readings
- **Remote GPIO Control**: Control water valves via GPIO pins on connected devices
- **Automated Watering**: One-click watering with automatic 1-second valve operation
- **Web Interface**: Clean, responsive web UI for monitoring and control
- **Docker Support**: Containerized deployment ready
- **Railway Deployment**: Pre-configured for Railway platform

## Architecture

The application communicates with a remote soil sensor device (typically a Raspberry Pi) via HTTP APIs to retrieve sensor data and control watering systems.

### Components

- **Web Application**: ASP.NET Core MVC frontend
- **Service Layer**: HTTP client for remote sensor communication
- **Data Models**: Soil sensor data representation
- **Remote Device**: Raspberry Pi with soil sensors and GPIO control

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Remote soil sensor device with API endpoints

### Local Development

```bash
# Clone the repository
git clone <repository-url>
cd SoilSeneorSystem

# Build the application
dotnet build

# Run in development mode
dotnet run
```

The application will be available at `http://localhost:5000`

### Docker Deployment

```bash
# Build Docker image
docker build -t soil-sensor-app .

# Run container
docker run -p 5000:5000 soil-sensor-app
```

## Configuration

Configure the soil sensor device endpoint in `appsettings.json`:

```json
{
  "SoilSensor": {
    "BaseUrl": "http://soil-sensor-pi.local:8080"
  }
}
```

Or set via environment variable:
```bash
export SoilSensor__BaseUrl="http://your-sensor-device:8080"
```

## API Endpoints

- `GET /Home/GetData` - Retrieve current soil sensor data
- `POST /Home/WaterPlant` - Trigger automated watering sequence

## Remote Device Requirements

The application expects a remote device providing these HTTP endpoints:

- `GET /api/soil-data` - Returns soil sensor readings
- `POST /api/soil-data/gpio/control` - Controls GPIO pins for water valve

### Expected Data Format

```json
{
  "voltage": 3.3,
  "moisture": 65.5,
  "timestamp": 1642608000000
}
```

## Deployment

### Railway

The application is pre-configured for Railway deployment with:

- `Dockerfile` for containerization
- `railway.json` for deployment settings
- Automatic PORT environment variable handling

### Environment Variables

- `PORT` - Server port (default: 5000)
- `SoilSensor__BaseUrl` - Remote sensor device URL

## Development

### Project Structure

```
SoilSeneorSystem/
├── Controllers/         # MVC controllers
├── Models/             # Data models
├── Services/           # Service layer
├── Views/              # Razor views
├── wwwroot/            # Static files
├── appsettings.json    # Configuration
└── Dockerfile          # Container configuration
```

### Key Files

- `Program.cs` - Application entry point and configuration
- `Services/SoilSensorService.cs` - HTTP client for remote device communication
- `Models/SoilData.cs` - Data model for sensor readings
- `Controllers/HomeController.cs` - Main controller with API endpoints

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `dotnet test`
5. Submit a pull request

## License

This project is licensed under the MIT License.