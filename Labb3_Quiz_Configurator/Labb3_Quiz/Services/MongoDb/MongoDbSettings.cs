namespace Labb3_Quiz_MongoDB.Services.MongoDb;

// En liten hjälparklass som håller alla inställningar för MongoDB-kopplingen.
// Poängen är att samla connection string + databasnamn på ett ställe,
// och samtidigt kunna läsa från environment variables om man vill.
public class MongoDbSettings
{
    // Default-connection string – samma som docker-compose exponerar.
    public string ConnectionString { get; init; } = "mongodb://localhost:27017";

    // Default-databasnamn (förnamn+efternamn utan mellanslag enligt kurskravet).
    public string DatabaseName { get; init; } = "MelvinEdlund";

    // Skapar ett nytt settings-objekt där ev. environment variables tas med.
    // Om variablerna inte är satta används default-värdena ovan.
    public static MongoDbSettings FromEnvironment()
    {
        var cs = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
        var db = Environment.GetEnvironmentVariable("MONGODB_DATABASE");

        return new MongoDbSettings
        {
            ConnectionString = string.IsNullOrWhiteSpace(cs) ? "mongodb://localhost:27017" : cs,
            DatabaseName = string.IsNullOrWhiteSpace(db) ? "MelvinEdlund" : db
        };
    }
}

