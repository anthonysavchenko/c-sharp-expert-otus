using InMemoryCache.Client;
using InMemoryCache.Core;
using NBomber.CSharp;

var scenario1 = Scenario.Create("SetCommand_NoIntersections", async context =>
{
  var randomChars = "abcdefghijklmnopqrstuvwxyz";
  var random = new Random();
  var key = random.GetString(randomChars, 16);
  var value = new UserProfile() { Username = "John Smith" };

  var step1 = await Step.Run("Set Command", context, async () =>
  {
    using var client = new TcpClient();

    await client.ConnectAsync("127.0.0.1", 8080);

    var response = await client.SetAsync(key, value);

    await client.DisconnectAsync();

    return response.StartsWith("OK") ? Response.Ok() : Response.Fail();
  });

  return Response.Ok();
})
.WithWarmUpDuration(TimeSpan.FromSeconds(10))
.WithLoadSimulations(
  Simulation.Inject(
    rate: 100,
    interval: TimeSpan.FromSeconds(1),
    during: TimeSpan.FromSeconds(30))
);

var scenario2 = Scenario.Create("SetGetDeleteCommand_WithIntersections", async context =>
{
  var randomChars = "abc";
  var random = new Random();
  var key = random.GetString(randomChars, 3);
  var value = new UserProfile() { Username = "John Smith" };

  var step1 = await Step.Run("Set Command", context, async () =>
  {
    using var client = new TcpClient();

    await client.ConnectAsync("127.0.0.1", 8080);

    var response = await client.SetAsync(key, value);

    await client.DisconnectAsync();

    return response.StartsWith("OK") ? Response.Ok() : Response.Fail();
  });

  var step2 = await Step.Run("Get Command", context, async () =>
  {
    using var client = new TcpClient();

    await client.ConnectAsync("127.0.0.1", 8080);

    var response = await client.GetAsync(key);

    await client.DisconnectAsync();

    return response != null && response.Username == value.Username ? Response.Ok() : Response.Fail();
  });

  var step3 = await Step.Run("Delete Command", context, async () =>
  {
    using var client = new TcpClient();

    await client.ConnectAsync("127.0.0.1", 8080);

    var response = await client.DeleteAsync(key);

    await client.DisconnectAsync();

    return response.StartsWith("OK") ? Response.Ok() : Response.Fail();
  });

  return Response.Ok();
})
.WithWarmUpDuration(TimeSpan.FromSeconds(10))
.WithLoadSimulations(
  Simulation.Inject(
    rate: 100,
    interval: TimeSpan.FromSeconds(1),
    during: TimeSpan.FromSeconds(30))
);


NBomberRunner.RegisterScenarios(scenario1, scenario2).Run();
