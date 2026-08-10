using InMemoryCache.Client;
using InMemoryCache.Core;
using NBomber.CSharp;

var scenario1 = Scenario.Create("SetCommand_NoIntersections", async context =>
{
  var randomChars = "abcdefghijklmnopqrstuvwxyz";
  var random = new Random();
  var key = random.GetString(randomChars, 16);

  var profile = new UserProfile()
  {
    Id = 1000,
    Username = "John Smith",
    CreatedAt = new DateTime(2026, 8, 1),
  };

  var step1 = await Step.Run("Set Command", context, async () =>
  {
    var response = await TcpClient.SetAsync("127.0.0.1", 8080, key, profile);

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

NBomberRunner.RegisterScenarios(scenario1).Run();

var scenario2 = Scenario.Create("SetCommand_WithIntersections", async context =>
{
  var randomChars = "ab";
  var random = new Random();
  var key = random.GetString(randomChars, 2);

  var profile = new UserProfile()
  {
    Id = 1000,
    Username = "John Smith",
    CreatedAt = new DateTime(2026, 8, 1),
  };

  var step1 = await Step.Run("Set Command", context, async () =>
  {
    var response = await TcpClient.SetAsync("127.0.0.1", 8080, key, profile);

    return response.StartsWith("OK") ? Response.Ok() : Response.Fail();
  });

  return Response.Ok();
})
.WithoutWarmUp()
.WithLoadSimulations(
  Simulation.Inject(
    rate: 20,
    interval: TimeSpan.FromSeconds(1),
    during: TimeSpan.FromSeconds(10))
);

var scenario3 = Scenario.Create("DeleteCommand_WithIntersections", async context =>
{
  var randomChars = "ab";
  var random = new Random();
  var key = random.GetString(randomChars, 2);

  var step3 = await Step.Run("Delete Command", context, async () =>
  {
    var response = await TcpClient.DeleteAsync("127.0.0.1", 8080, key);

    return response.StartsWith("OK") ? Response.Ok() : Response.Fail();
  });

  return Response.Ok();
})
.WithoutWarmUp()
.WithLoadSimulations(
  Simulation.Inject(
    rate: 20,
    interval: TimeSpan.FromSeconds(1),
    during: TimeSpan.FromSeconds(10))
);

NBomberRunner.RegisterScenarios(scenario2, scenario3).Run();
