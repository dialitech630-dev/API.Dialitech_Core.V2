using API.Dialitech.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace API.Dialitech.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    static MongoDbContext()
    {
        BsonClassMap.RegisterClassMap<User>(cm =>
        {
            cm.AutoMap();
            cm.IdMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
        });

        BsonClassMap.RegisterClassMap<HealthRecord>(cm =>
        {
            cm.AutoMap();
            cm.IdMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
        });

        BsonClassMap.RegisterClassMap<Alert>(cm =>
        {
            cm.AutoMap();
            cm.IdMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
        });
    }

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<HealthRecord> HealthRecords => _database.GetCollection<HealthRecord>("HealthRecords");
    public IMongoCollection<Alert> Alerts => _database.GetCollection<Alert>("Alerts");
}

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
