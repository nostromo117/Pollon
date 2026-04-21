using Consul;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Pollon Configuration Seeder starting...");

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var messagingConnString = config.GetConnectionString("messaging") ?? config["ConnectionStrings:messaging"];
var keycloakUrl = config["KEYCLOAK_URL"] ?? "http://localhost:8080";
var consulAddr = config["CONSUL_URL"] ?? config["CONSUL_HTTP_ADDR"] ?? "http://localhost:8500";

if (string.IsNullOrEmpty(messagingConnString))
{
    Console.WriteLine("Warning: RabbitMQ connection string not found. Checked 'ConnectionStrings:messaging'.");
    Console.WriteLine("Dumping all env vars for debug:");
    foreach (var c in config.AsEnumerable())
    {
        Console.WriteLine($"{c.Key} = {c.Value}");
    }
    return;
}

Console.WriteLine($"Seeding Consul at {consulAddr}...");
Console.WriteLine($"Value for pollon/messaging: {messagingConnString}");
Console.WriteLine($"Value for pollon/Keycloak/Url: {keycloakUrl}");

using var client = new ConsulClient(cfg => 
{
    cfg.Address = new Uri(consulAddr);
});

// Retry logic to wait for Consul
int retries = 0;
bool success = false;
while (retries < 10 && !success)
{
    try 
    {
        var putPair = new KVPair("pollon/messaging")
        {
            Value = System.Text.Encoding.UTF8.GetBytes(messagingConnString)
        };
        await client.KV.Put(putPair);

        var keycloakPair = new KVPair("pollon/Keycloak/Url")
        {
            Value = System.Text.Encoding.UTF8.GetBytes(keycloakUrl)
        };
        var putRes = await client.KV.Put(keycloakPair);
        if (putRes.StatusCode == System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine("Consul seeded successfully!");
            success = true;
        }
        else
        {
            Console.WriteLine($"Consul seeding failed with status: {putRes.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        retries++;
        Console.WriteLine($"Attempt {retries}: Consul not ready yet... {ex.Message}");
        await Task.Delay(2000);
    }
}

if (!success)
{
    Console.WriteLine("Failed to seed Consul after multiple attempts.");
    Environment.Exit(1);
}
