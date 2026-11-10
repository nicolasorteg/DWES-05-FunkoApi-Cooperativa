using MongoDB.Driver;
using NUnit.Framework;
using Testcontainers.MongoDb;

namespace FunkoApi.Tests.Repositories;

public abstract class MongoTestBase {
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .Build();

    protected MongoClient Client { get; private set; } = null!;
    protected const string DbName = "test_db";

    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        await _mongo.StartAsync();
        Client = new MongoClient(_mongo.GetConnectionString());
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        Client.Dispose();
        await _mongo.DisposeAsync();
    }
}