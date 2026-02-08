using System.Text.Json;
using AgentFrameworkApp.Models;

namespace AgentFrameworkApp.Services;

/// <summary>
/// Dispatches function tool calls to local implementations and returns JSON results.
/// </summary>
public static class FunctionDispatcher
{
    /// <summary>
    /// Dispatches a function call by name and returns the result as a JSON string.
    /// </summary>
    public static string Dispatch(string functionName, string argumentsJson)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  [Tool call]: {functionName}({argumentsJson})");
        Console.ResetColor();

        return functionName switch
        {
            "get_weather" => HandleGetWeather(argumentsJson),
            "get_stock_price" => HandleGetStockPrice(argumentsJson),
            "search_knowledge_base" => HandleSearchKnowledgeBase(argumentsJson),
            "calculate" => HandleCalculate(argumentsJson),
            _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
        };
    }

    private static string HandleGetWeather(string argumentsJson)
    {
        var args = JsonDocument.Parse(argumentsJson);
        string city = args.RootElement.GetProperty("city").GetString() ?? "Unknown";

        // Simulated weather data
        var random = new Random(city.GetHashCode());
        var weather = new WeatherData(
            City: city,
            TemperatureCelsius: Math.Round(random.NextDouble() * 35 + 5, 1),
            Condition: new[] { "Sunny", "Cloudy", "Rainy", "Partly Cloudy", "Snowy" }[random.Next(5)],
            Humidity: random.Next(30, 90),
            WindSpeedKmh: Math.Round(random.NextDouble() * 30, 1));

        return JsonSerializer.Serialize(weather);
    }

    private static string HandleGetStockPrice(string argumentsJson)
    {
        var args = JsonDocument.Parse(argumentsJson);
        string symbol = args.RootElement.GetProperty("symbol").GetString()?.ToUpper() ?? "UNKNOWN";

        // Simulated stock data
        var prices = new Dictionary<string, double>
        {
            ["MSFT"] = 425.52, ["AAPL"] = 237.40, ["GOOGL"] = 183.75,
            ["AMZN"] = 205.30, ["TSLA"] = 248.15, ["META"] = 595.50,
            ["NVDA"] = 135.60
        };

        double price = prices.GetValueOrDefault(symbol, 100 + new Random(symbol.GetHashCode()).NextDouble() * 200);
        double change = Math.Round((new Random().NextDouble() - 0.5) * 10, 2);
        var quote = new StockQuote(symbol, Math.Round(price, 2), change, Math.Round(change / price * 100, 2));

        return JsonSerializer.Serialize(quote);
    }

    private static string HandleSearchKnowledgeBase(string argumentsJson)
    {
        var args = JsonDocument.Parse(argumentsJson);
        string query = args.RootElement.GetProperty("query").GetString() ?? "";

        // Simulated knowledge base (RAG pattern demo)
        var knowledgeBase = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["refund policy"] = "Our refund policy allows returns within 30 days of purchase. Items must be in original condition. Digital products can be refunded within 14 days if unused. Contact support@company.com for refund requests.",
            ["shipping"] = "Standard shipping takes 5-7 business days. Express shipping is 2-3 business days. Free shipping on orders over $50. International shipping available to 40+ countries.",
            ["pricing plans"] = "We offer three plans: Basic ($9.99/mo) with 5 users, Professional ($29.99/mo) with 25 users and priority support, and Enterprise (custom pricing) with unlimited users, SSO, and dedicated account manager.",
            ["api limits"] = "Free tier: 1,000 requests/day. Pro tier: 50,000 requests/day. Enterprise: unlimited. Rate limiting applies at 100 requests/minute for all tiers.",
            ["security"] = "We use AES-256 encryption at rest and TLS 1.3 in transit. SOC 2 Type II certified. GDPR compliant. Two-factor authentication available. Regular penetration testing performed quarterly.",
            ["contact"] = "Support hours: Mon-Fri 9am-6pm EST. Email: support@company.com. Phone: 1-800-555-0199. Live chat available on our website during business hours."
        };

        var results = knowledgeBase
            .Where(kvp => kvp.Key.Contains(query, StringComparison.OrdinalIgnoreCase)
                          || kvp.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new { topic = kvp.Key, content = kvp.Value })
            .ToList();

        if (results.Count == 0)
            return JsonSerializer.Serialize(new { message = "No relevant information found.", query });

        return JsonSerializer.Serialize(new { results, query });
    }

    private static string HandleCalculate(string argumentsJson)
    {
        var args = JsonDocument.Parse(argumentsJson);
        string expression = args.RootElement.GetProperty("expression").GetString() ?? "";

        try
        {
            // Simple expression evaluator for demo purposes
            var result = new System.Data.DataTable().Compute(expression, null);
            return JsonSerializer.Serialize(new { expression, result = result?.ToString() });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { expression, error = ex.Message });
        }
    }
}
