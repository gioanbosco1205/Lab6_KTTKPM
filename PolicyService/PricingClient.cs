using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using Steeltoe.Common.Discovery;

public class PricingClient
{
    private readonly HttpClient _client;
    private readonly IAsyncPolicy<string> _retryPolicy;

    public PricingClient(IDiscoveryClient discoveryClient)
    {
        var handler = new DiscoveryHttpClientHandler(discoveryClient);

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://PRICINGSERVICE:8080")
        };

        _retryPolicy = Policy<string>
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                retryAttempt =>
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    Console.WriteLine($"Retry lần {retryAttempt} - chờ {delay.TotalSeconds}s");
                    return delay;
                }
            );
    }

    public async Task<string> GetPrice()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            Console.WriteLine("Đang gọi PricingService...");

            var response = await _client.GetAsync("api/pricing"); // sửa dòng này

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Gọi thành công!");

            return result;
        });
    }
}