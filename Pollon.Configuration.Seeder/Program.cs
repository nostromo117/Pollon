using Consul;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Pollon Configuration Seeder starting...");

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var messagingConnString = config.GetConnectionString("messaging") ?? config["ConnectionStrings:messaging"];
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
            // We store it as a simple string. 
            // Winton Consul Provider will see "pollon" as prefix, 
            // and "messaging" as the key under it.
            Value = System.Text.Encoding.UTF8.GetBytes(messagingConnString)
        };

        var putRes = await client.KV.Put(putPair);
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
