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
        BsonClassMap.RegisterClassMap<Caregiver>(cm =>
        {
            cm.AutoMap();
            cm.IdMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
        });

        BsonClassMap.RegisterClassMap<Patient>(cm =>
        {
            cm.AutoMap();
            cm.IdMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
        });

        BsonClassMap.RegisterClassMap<Device>(cm =>
        {
            cm.AutoMap();
            cm.IdMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
        });

        BsonClassMap.RegisterClassMap<Alert>(cm =>
        {
            cm.AutoMap();
            cm.IdMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
        });

        BsonClassMap.RegisterClassMap<Reading>(cm =>
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

    public IMongoCollection<Caregiver> Caregivers => _database.GetCollection<Caregiver>("Caregivers");
    public IMongoCollection<Patient> Patients => _database.GetCollection<Patient>("Patients");
    public IMongoCollection<Device> Devices => _database.GetCollection<Device>("Devices");
    public IMongoCollection<Alert> Alerts => _database.GetCollection<Alert>("Alerts");
    public IMongoCollection<Reading> Readings => _database.GetCollection<Reading>("Readings");
}

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
