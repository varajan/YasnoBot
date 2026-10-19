using Base.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using YasnoLogger.Interfaces;

namespace Base.Services;

public class ApiService(ILogger logger) : IApi
{
    private static readonly HttpClient _httpClient = new();

    public async Task<T?> Get<T>(string url) where T : class
    {
        try
        {
            T? result = await _httpClient.GetFromJsonAsync<T>(url);
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.Log(ex);
            return null;
        }
        catch (JsonException ex)
        {
            logger.Log(ex);
            return null;
        }
    }
}