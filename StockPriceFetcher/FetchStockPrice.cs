using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;  // For LINQ methods
using Newtonsoft.Json.Linq; // For parsing JSON
using System;  // For Environment class

public static class FetchStockPrice
{
    private static readonly HttpClient httpClient = new HttpClient();

    [FunctionName("FetchStockPrice")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        string symbol = req.Query["symbol"];
        if (string.IsNullOrEmpty(symbol))
        {
            return new BadRequestObjectResult("Missing 'symbol' query parameter.");
        }

        string apiKey = Environment.GetEnvironmentVariable("ALPHA_VANTAGE_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return new ObjectResult("API key not configured.") { StatusCode = 500 };
        }

        string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval=5min&apikey={apiKey}";
        var response = await httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        JObject json = JObject.Parse(content);
        var timeSeries = json["Time Series (5min)"] as JObject;
        if (timeSeries == null)
            return new BadRequestObjectResult("Invalid response or rate-limited by API.");

        // Get the latest timestamp (first key)
        var latestEntry = timeSeries.Properties().FirstOrDefault(); // Use FirstOrDefault to avoid null reference
        if (latestEntry.Value == null)
            return new BadRequestObjectResult("No valid stock data found.");

        var closePrice = latestEntry.Value["4. close"]?.ToString();

        return new OkObjectResult(new { symbol = symbol, latestPrice = closePrice });
    }
}
