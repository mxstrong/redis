using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Redis
{
  class Program
  {
    static async Task Main()
    {

      var config = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddUserSecrets<Program>()
        .Build();
      ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
              EndPoints = { config.GetSection("Redis:Endpoint").Value },
              Password = config.GetSection("Redis:Password").Value,
              AllowAdmin = true
            });
      var samplesDB = new SamplesDB(redis, config);
      var console = new ConsoleInterface(samplesDB);
      await console.ChooseActionAsync();
    }

  }

  public class ConsoleInterface
  {
    private readonly SamplesDB _db;
    public ConsoleInterface(SamplesDB db)
    {
      _db = db;
    }

    public async Task ChooseActionAsync()
    {
      Console.WriteLine("Pasirinkite ką norite daryti įvesdami skaičių:\n"
        + "1 - Užsiregistruoti\n"
        + "2 - Pirkti kreditus\n"
        + "3 - Įkelti sample paketą\n"
        + "4 - Peržiūrėti siūlomus sample paketus\n"
        + "5 - Pirkti sample paketą už kreditus\n"
        + "6 - Peržiūrėti turimus sample paketus\n"
        + "7 - Atšaukti pirkimą\n"
        + "8 - Peržiūrėti visus vartotojus\n"
        + "9 - Išvalyti duomenų bazę\n"
        + "0 - Užbaigti darbą su programa\n"
      );
      try
      {
        var action = Convert.ToInt32(Console.ReadLine());
        switch (action)
        {
          case 0:
            break;
          case 1:
            await RegisterAsync();
            await ChooseActionAsync();
            break;
          case 2:
            await BuyCreditsAsync();
            await ChooseActionAsync();
            break;
          case 3:
            await AddSamplePackAsync();
            await ChooseActionAsync();
            break;
          case 4:
            await ViewAllSamplePacksAsync();
            await ChooseActionAsync ();
            break;
          case 5:
            await BuySamplePackAsync();
            await ChooseActionAsync();
            break;
          case 6:
            await ViewOwnedSamplePacksAsync();
            await ChooseActionAsync();
            break;
          case 7:
            await UndoPurchaseAsync();
            await ChooseActionAsync();
            break;
          case 8:
            await ViewAllUsersAsync();
            await ChooseActionAsync();
            break;
          case 9:
            await ClearDatabase();
            await ChooseActionAsync();
            break;
          default:
            throw new NotImplementedException();
        }
      }
      catch
      {
        Console.WriteLine("Netinkama įvestis\n");
        await ChooseActionAsync();
      }
    }
    public async Task RegisterAsync()
    {
      Console.WriteLine("Įveskite savo vartotojo vardą\n");
      string inputName = Console.ReadLine();
      await _db.CreateUser(inputName);
      Console.WriteLine("Vartotojas sėkmingai pridėtas\n");
    }

    public async Task BuyCreditsAsync()
    {
      Console.WriteLine("Įveskite vartotojo ID\n");
      var userId = Console.ReadLine();
      Console.WriteLine("Įveskite kiek kreditų norite pirkti\n");
      var amount = Convert.ToInt32(Console.ReadLine());
      await _db.AddCredits(amount, userId);
      Console.WriteLine("Kreditai sėkmingai pridėti\n");
    }

    public async Task AddSamplePackAsync()
    {
      Console.WriteLine("Įveskite vartotojo ID\n");
      var userId = Console.ReadLine();
      Console.WriteLine("Įveskite sample paketo pavadinimą\n");
      var sampleName = Console.ReadLine();
      Console.WriteLine("Įveskite norimą paketo kainą kreditais\n");
      var credits = Convert.ToInt32(Console.ReadLine());
      await _db.AddSamplePack(userId, sampleName, credits);
    }

    public async Task ViewAllSamplePacksAsync()
    {
      var packs = await _db.ShowAllSamplePacks();
      packs.ForEach(pack =>
      {
        Console.WriteLine($"ID: {pack.Id}, UserId: {pack.UserId}, {pack.User} - {pack.Name}\n");
      });
      if (packs.Count < 1)
      {
        Console.WriteLine("Šiuo metu duomenų bazėje sample paketų nėra\n");
      }
    }

    public async Task BuySamplePackAsync()
    {
      Console.WriteLine("Įveskite savo vartotojo ID\n");
      var userId = Console.ReadLine();
      Console.WriteLine("Įveskite pardavėjo vartotojo ID\n");
      var sellerId = Console.ReadLine();
      Console.WriteLine("Įveskite norimo sample paketo ID\n");
      var itemId = Console.ReadLine();
      var result = await _db.BuySamplePack(userId, sellerId, itemId);
      if (result)
      {
        Console.WriteLine("Pirkimas sėkmingai įvykdytas\n");
      } else
      {
        Console.WriteLine("Pirkimo įvykdyti nepavyko\n");
      }
    }

    public async Task ViewOwnedSamplePacksAsync()
    {
      Console.WriteLine("Įveskite savo vartotojo ID");
      var userId = Console.ReadLine();
      var packs = await _db.ShowOwnedSamplePacks(userId);
      packs.ForEach(pack =>
      {
        Console.WriteLine($"ID: {pack.Id}, {pack.User} - {pack.Name} (raktas: {pack.Key})\n");
      });
      if (packs.Count < 1)
      {
        Console.WriteLine("Šiuo metu neturite nei vieno įsigyto paketo\n");
      }
    }

    public async Task UndoPurchaseAsync()
    {
      Console.WriteLine("Įveskite savo vartotojo ID\n");
      var userId = Console.ReadLine();
      Console.WriteLine("Įveskite pardavėjo ID\n");
      var sellerId = Console.ReadLine();
      Console.WriteLine("Įveskite nusipirkto paketo kurį norite grąžinti ID\n");
      var packId = Console.ReadLine();
      var result = await _db.UndoPurchase(userId, sellerId, packId);
      if (result)
      {
        Console.WriteLine("Pirkimas sėkmingai atšauktas\n");
      } else
      {
        Console.WriteLine("Pirkimo atšaukti nepavyko\n");
      }
    }
    public async Task ViewAllUsersAsync()
    {
      var users = await _db.ShowAllUsersAsync();
      users.ForEach(user =>
      {
        Console.WriteLine($"ID: {user.Id} Vartotojo vardas: {user.Name}, kreditų skaičius: {user.Credits}\n");
      });
      if (users.Count < 1)
      {
        Console.WriteLine("Šiuo metu DB nėra sukurtų vartotojų\n");
      }
    }

    public async Task ClearDatabase()
    {
      await _db.ClearDatabase();
      Console.WriteLine("Duomenų bazė sėkmingai išvalyta\n");
    }
  }

  public class SamplesDB
  {
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _db;
    private readonly IConfiguration _config;
    public SamplesDB(IConnectionMultiplexer connection, IConfiguration config)
    {
      _connection = connection;
      _config = config;
      _db = _connection.GetDatabase();
    }

    public async Task CreateUser(string name)
    {
      var random = new Random();
      var id = random.Next(1000, 10000).ToString();
      var hashset = new HashEntry[] { new HashEntry("name" , name), new HashEntry("credits", 0)};
      await _db.HashSetAsync($"user-{id}", hashset);
    }

    public async Task AddCredits(int amount, string userId)
    {
      var currentAmount = await _db.HashGetAsync($"user-{userId}", "credits");
      var credits = new HashEntry[] { new HashEntry("credits", currentAmount + amount) };
      await _db.HashSetAsync($"user-{userId}", credits);
    }

    public async Task AddSamplePack(string userId, string sampleName, int creditCost)
    {
      var random = new Random();
      var id = random.Next(1000, 10000).ToString();
      var accessKey = new HashEntry("key", Guid.NewGuid().ToString());
      var userName = await _db.HashGetAsync($"user-{userId}", "name");
      var username = new HashEntry("username", userName);
      var name = new HashEntry("name", sampleName);
      var cost = new HashEntry("cost", creditCost.ToString());
      var hashSet = new HashEntry[] { accessKey, username, name, cost };
      await _db.HashSetAsync($"samples-{userId}-{id}", hashSet);
    }

    public async Task<bool> BuySamplePack(string buyerId, string sellerId, string packId)
    {
      var pack = await _db.HashGetAllAsync($"samples-{sellerId}-{packId}");
      var packDict = pack.ToDictionary();
      var cost = Convert.ToInt32(packDict.GetValueOrDefault("cost"));
      var balance = await _db.HashGetAsync($"user-{buyerId}", "credits");
      var newBalance = Convert.ToInt32(balance) - cost;
      if (newBalance < 0)
      {
        return false;
      }
      var transaction = _db.CreateTransaction();
      transaction.AddCondition(Condition.HashNotExists($"owned-{buyerId}-{packId}", "name"));
      transaction.AddCondition(Condition.HashEqual($"user-{buyerId}", "credits", balance));
      transaction.HashSetAsync($"user-{buyerId}", "credits", newBalance);
      transaction.HashIncrementAsync($"user-{sellerId}", "credits", cost);
      transaction.HashSetAsync($"owned-{buyerId}-{packId}", pack);
      return await transaction.ExecuteAsync();
    }

    public async Task<List<SamplePack>> ShowOwnedSamplePacks(string userId)
    {
      var hostAndPort = _config.GetSection("Redis:Endpoint").Value.Split(':');
      var host = hostAndPort[0];
      var port = Convert.ToInt32(hostAndPort[1]);
      var server = _connection.GetServer(host, port);
      var ownedPacks = new List<SamplePack>();
      foreach (var key in server.Keys(pattern: $"owned-{userId}-*"))
      {
        var id = key.ToString().Split('-')[2];
        var pack = (await _db.HashGetAllAsync(key)).ToDictionary();
        var samplePack = new SamplePack(pack.GetValueOrDefault("username"), pack.GetValueOrDefault("name"), pack.GetValueOrDefault("key"), id, userId);
        ownedPacks.Add(samplePack);
      }
      return ownedPacks;
    }

    public async Task<List<SamplePack>> ShowAllSamplePacks()
    {
      var hostAndPort = _config.GetSection("Redis:Endpoint").Value.Split(':');
      var host = hostAndPort[0];
      var port = Convert.ToInt32(hostAndPort[1]);
      var server = _connection.GetServer(host, port);
      var allPacks = new List<SamplePack>();
      foreach (var key in server.Keys(pattern: $"samples-*"))
      {
        var id = key.ToString().Split('-')[2];
        var userId = key.ToString().Split('-')[1];
        var pack = (await _db.HashGetAllAsync(key)).ToDictionary();
        var samplePack = new SamplePack(pack.GetValueOrDefault("username"), pack.GetValueOrDefault("name"), id, userId);
        allPacks.Add(samplePack);
      }
      return allPacks;
    }

    public async Task<List<User>> ShowAllUsersAsync()
    {
      var hostAndPort = _config.GetSection("Redis:Endpoint").Value.Split(':');
      var host = hostAndPort[0];
      var port = Convert.ToInt32(hostAndPort[1]);
      var server = _connection.GetServer(host, port);
      var users = new List<User>();
      foreach (string key in server.Keys(pattern: $"user-*"))
      {
        var id = key.ToString().Split('-')[1];
        var userHash = await _db.HashGetAllAsync(key);
        var userDict = userHash.ToDictionary();
        users.Add(new User(userDict.GetValueOrDefault("name"), Convert.ToInt32(userDict.GetValueOrDefault("credits")), id));
      }
      return users;
    }

    public async Task<bool> UndoPurchase(string buyerId, string sellerId, string packId)
    {
      var pack = await _db.HashGetAllAsync($"owned-{buyerId}-{packId}");
      var packDict = pack.ToDictionary();
      var cost = Convert.ToInt32(packDict.GetValueOrDefault("cost"));
      var balance = await _db.HashGetAsync($"user-{buyerId}", "credits");
      var newBalance = Convert.ToInt32(balance) - cost;
      var transaction = _db.CreateTransaction();
      transaction.AddCondition(Condition.HashExists($"owned-{buyerId}-{packId}", "name"));
      transaction.AddCondition(Condition.HashEqual($"user-{buyerId}", "credits", balance));
      transaction.HashDecrementAsync($"user-{sellerId}", "credits", cost);
      transaction.HashSetAsync($"user-{buyerId}", "credits", newBalance);
      foreach (string key in packDict.Keys)
      {
        transaction.HashDeleteAsync($"owned-{buyerId}-{packId}", key);
      }
      return await transaction.ExecuteAsync();
    }

    public async Task ClearDatabase()
    {
      var hostAndPort = _config.GetSection("Redis:Endpoint").Value.Split(':');
      var host = hostAndPort[0];
      var port = Convert.ToInt32(hostAndPort[1]);
      var server = _connection.GetServer(host, port);
      await server.FlushDatabaseAsync();
    }
  }

  public class SamplePack {
    public string Id { get; set; }
    public string UserId { get; set; }
    public string User { get; set; }
    public string Name { get; set; }
    public string Key { get; set; }

    public readonly bool owned;

    public SamplePack(string user, string name, string key, string id, string userId)
    {
      User = user;
      Name = name;
      Key = key;
      Id = id;
      UserId = userId;
      owned = true;
    }

    public SamplePack(string user, string name, string id, string userId)
    {
      User = user;
      Name = name;
      Id = id;
      UserId = userId;
      owned = false;
    }
  }

  public class User {
    public string Id { get; set; }
    public string Name { get; set; }
    public int Credits { get; set; }
    public User(string name, int credits, string id)
    {
      Id = id;
      Name = name;
      Credits = credits;
    }
  }
}
