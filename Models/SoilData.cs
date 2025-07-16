namespace SoilSensorCapture.Models
{
    // Models/SoilData.cs
    public class SoilData
    {
        public float Voltage { get; set; }
        public float Moisture { get; set; }
        public long Timestamp { get; set; }
    }

    // Models/WateringRecord.cs
    public class WateringRecord
    {
        public DateTime WateringTime { get; set; }
        public float MoistureBefore { get; set; }
        public float MoistureAfter { get; set; }
        public float MoistureChange { get; set; }
        public WateringType Type { get; set; }
    }

    public enum WateringType
    {
        Automatic,    // 自動澆水
        Manual,       // 手動澆水
        Detected      // 檢測到的澆水
    }
}
