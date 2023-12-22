using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NitroGenerator
{
	public class Program
	{
		static string PartnerId = "";
		static List<WebProxy> Proxies = new();

		static void Main(string[] args)
		{

			if(!File.Exists("./partnerid.txt"))
			{
				Console.Write("Enter partner-id: ");
				string? pid = Console.ReadLine();
				if(pid is null)
				{
					Console.WriteLine("Partner-Id is null.");
					return;
                }

                File.WriteAllText("./partnerid.txt", pid);
            }

			if(!File.Exists("proxies.txt"))
			{
				Console.WriteLine("No proxies found. Enter your proxies in the 'proxies.txt' file. (Format: 'http(s)://ip:port')");
				File.WriteAllText("proxies.txt", "# Format: 'http(s)://ip:port'");
				return;
			}
			Console.WriteLine("Loading proxies...");
			string[] proxies = File.ReadAllLines("proxies.txt");
			foreach(string proxyAddress in proxies)
			{
				if (proxyAddress.StartsWith('#') || string.IsNullOrWhiteSpace(proxyAddress)) continue;
				try
				{

                    WebProxy p = new()
                    {
                        Address = new Uri(proxyAddress),
                        BypassProxyOnLocal = false
                    };
                    Proxies.Add(p);
                }
				catch(Exception ex)
				{
					Console.WriteLine($"Could not load proxy '{proxyAddress}': {ex.Message}");
				}

			}

			if(Proxies.Count <= 0)
            {
                Console.WriteLine("No proxies found. Enter your proxies in the 'proxies.txt' file. (Format: 'http(s)://ip:port')");
                return;
            }

			PartnerId = File.ReadAllText("./partnerid.txt");

            Console.WriteLine("Generating nitro...");

			WriteLinks();
		}

		static void WriteLinks()
		{
            int linksGenerated = 0;
            while (true)
            {
				GenerateTokenAsync((t) =>
				{
					File.AppendAllText("./nitro.txt", "https://discord.com/billing/partner-promotions/1180231712274387115/" + t + "\r\n");
					linksGenerated++;
					Console.WriteLine(linksGenerated + " Links generated.");
				});
            }
        }

		static async void GenerateTokenAsync(Action<string> onCompleted)
        {
			HttpClient? client = null;
			try
            {
                client = RandomProxy();
                HttpResponseMessage resp = await client.PostAsync("https://api.discord.gx.games/v1/direct-fulfillment", new StringContent("{\"partnerUserId\":\"" + PartnerId + "\"}", Encoding.UTF8, "application/json"));
                client?.Dispose();
				string token = JsonSerializer.Deserialize<TokenResponse>(await resp.Content.ReadAsStringAsync())!.Token;
				onCompleted(token);
            }
			catch
            {
                client?.Dispose();
			}
        }

		static HttpClient RandomProxy()
		{
            HttpClientHandler httpClientHandler = new()
            {
                Proxy = Proxies[Random.Shared.Next(0, Proxies.Count)],
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            return new HttpClient(handler: httpClientHandler, disposeHandler: true);

        }

        class TokenResponse
		{
			[JsonPropertyName("token")]
			public string Token { get; set; }

			public TokenResponse()
			{
				Token = string.Empty;
			}
		}
	}
}