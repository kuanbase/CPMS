using MongoDB.Driver;

namespace CPMS.Services;

public class MongoDBService
{
    private readonly IMongoDatabase _database;
    public MongoDBService(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName) where T: class
    {
        return _database.GetCollection<T>(collectionName);
    }
}
