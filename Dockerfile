# �ϥΩx�� .NET 8.0 SDK �@���ظm�M��
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# �ƻs�M���ɨ��٭�̿ඵ
COPY ["SoilSensorCapture.csproj", "."]
RUN dotnet restore "SoilSensorCapture.csproj"

# �ƻs�Ҧ���l�X�ëظm���ε{��
COPY . .
WORKDIR "/src"
RUN dotnet build "SoilSensorCapture.csproj" -c Release -o /app/build

# �o�G���ε{��
FROM build AS publish
RUN dotnet publish "SoilSensorCapture.csproj" -c Release -o /app/publish /p:UseAppHost=false

# �ϥΩx�� .NET 8.0 ���涥�q�@���̲׬M��
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# �]�w�������ܼ�
ENV ASPNETCORE_URLS=http://+:$PORT

# ���S��
EXPOSE $PORT

ENTRYPOINT ["dotnet", "SoilSensorCapture.dll"]