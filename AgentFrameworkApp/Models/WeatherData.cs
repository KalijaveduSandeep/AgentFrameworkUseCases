namespace AgentFrameworkApp.Models;

/// <summary>
/// Represents weather data returned by the weather function tool.
/// </summary>
public record WeatherData(string City, double TemperatureCelsius, string Condition, int Humidity, double WindSpeedKmh)
{
    public override string ToString() =>
        $"{City}: {TemperatureCelsius}°C, {Condition}, Humidity {Humidity}%, Wind {WindSpeedKmh} km/h";
}

/// <summary>
/// Represents a stock quote returned by the stock function tool.
/// </summary>
public record StockQuote(string Symbol, double Price, double Change, double ChangePercent)
{
    public override string ToString() =>
        $"{Symbol}: ${Price:F2} ({(Change >= 0 ? "+" : "")}{Change:F2}, {ChangePercent:F2}%)";
}
