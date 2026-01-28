using System.IO;
using System.Text.Json;
using Labb3_Quiz_MongoDB.Models;
using MongoDB.Driver;

namespace Labb3_Quiz_MongoDB.Services.MongoDb;

// Den konkreta implementationen av IStorageService som pratar med MongoDB.
// All databaslogik (collections, index, CRUD, seedning) ligger här,
// så ViewModels slipper bry sig om hur datan faktiskt lagras.
public class MongoStorageService : IStorageService
{
    // Namn på collections i databasen.
    private const string PacksCollectionName = "questionPacks";
    private const string CategoriesCollectionName = "categories";

    // MongoDB-objekt som håller kopplingen.
    private readonly IMongoDatabase _db;
    private readonly IMongoCollection<QuestionPack> _packs;
    private readonly IMongoCollection<Category> _categories;

    // Konstruktorn skapar en klient, hämtar rätt databas och samlar collektions-referenser.
    // Den anropar också EnsureDatabaseInitialized så att allt som krävs finns.
    public MongoStorageService(MongoDbSettings? settings = null)
    {
        // Läs inställningar (ev. från environment).
        settings ??= MongoDbSettings.FromEnvironment();

        // Skapa MongoDB-klient och koppla mot rätt databas.
        var client = new MongoClient(settings.ConnectionString);
        _db = client.GetDatabase(settings.DatabaseName);

        // Hämta (eller skapa) collections.
        _packs = _db.GetCollection<QuestionPack>(PacksCollectionName);
        _categories = _db.GetCollection<Category>(CategoriesCollectionName);

        // Appen ansvarar för att skapa det som behövs i MongoDB:
        // - Skapa collections (via CreateCollection om de saknas)
        // - Sätta index
        // - Seeda startdata
        EnsureDatabaseInitialized();
    }

    // Säkerställer att collections, index och seed-data finns.
    private void EnsureDatabaseInitialized()
    {
        var existing = _db.ListCollectionNames().ToList();

        // Skapa collections om de inte redan finns.
        if (!existing.Contains(PacksCollectionName))
            _db.CreateCollection(PacksCollectionName);
        if (!existing.Contains(CategoriesCollectionName))
            _db.CreateCollection(CategoriesCollectionName);

        // Skapa unikt index på Category.Name så att vi inte kan ha dubbletter.
        var nameIndex = new CreateIndexModel<Category>(
            Builders<Category>.IndexKeys.Ascending(x => x.Name),
            new CreateIndexOptions { Unique = true, Name = "uniq_category_name" }
        );
        _categories.Indexes.CreateOne(nameIndex);

        // Seed: migrera in initiala packs från Resources/packs.json om DB är tom.
        if (_packs.EstimatedDocumentCount() == 0)
        {
            var seeded = TryLoadInitialPacksFromResources();
            if (seeded.Count > 0)
                _packs.InsertMany(seeded);
        }

        // Seed: lägg in några standardkategorier om inga finns.
        if (_categories.EstimatedDocumentCount() == 0)
        {
            var defaults = new[]
            {
                new Category { Name = "General" },
                new Category { Name = "Programming" },
                new Category { Name = "Movies" },
                new Category { Name = "Sports" }
            };

            try
            {
                _categories.InsertMany(defaults);
            }
            catch
            {
                // Ignorera fel här – t.ex. om någon annan redan skapat dem.
            }
        }
    }

    // Försöker läsa in start-packs från Resources/packs.json (samma format som i labb 3).
    private static List<QuestionPack> TryLoadInitialPacksFromResources()
    {
        try
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var resourcesPath = Path.Combine(appDirectory, "Resources", "packs.json");
            if (!File.Exists(resourcesPath))
                return new List<QuestionPack>();

            var json = File.ReadAllText(resourcesPath);
            var packs = JsonSerializer.Deserialize<List<QuestionPack>>(json);
            return packs ?? new List<QuestionPack>();
        }
        catch
        {
            // Vid fel (t.ex. trasig JSON) hoppar vi bara över seedning.
            return new List<QuestionPack>();
        }
    }

    // ----- QuestionPack-CRUD -----

    // Hämtar alla frågepaket (SELECT * FROM questionPacks).
    public async Task<List<QuestionPack>> GetAllPacksAsync()
    {
        return await _packs.Find(Builders<QuestionPack>.Filter.Empty).ToListAsync();
    }

    // Skapar ett nytt frågepaket (INSERT).
    public async Task CreatePackAsync(QuestionPack pack)
    {
        await _packs.InsertOneAsync(pack);
    }

    // Uppdaterar ett befintligt frågepaket (REPLACE med upsert=true).
    public async Task UpdatePackAsync(QuestionPack pack)
    {
        var filter = Builders<QuestionPack>.Filter.Eq(x => x.Id, pack.Id);
        await _packs.ReplaceOneAsync(filter, pack, new ReplaceOptions { IsUpsert = true });
    }

    // Tar bort ett frågepaket helt.
    public async Task DeletePackAsync(Guid packId)
    {
        var filter = Builders<QuestionPack>.Filter.Eq(x => x.Id, packId);
        await _packs.DeleteOneAsync(filter);
    }


    // ----- Category-CRUD -----

    // Hämtar alla kategorier (sorterade på namn).
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _categories.Find(Builders<Category>.Filter.Empty)
            .SortBy(x => x.Name)
            .ToListAsync();
    }

    // Skapar en ny kategori.
    public async Task CreateCategoryAsync(Category category)
    {
        category.Name = category.Name.Trim();
        await _categories.InsertOneAsync(category);
    }

    // Tar bort en kategori och rensar CategoryId/CategoryName på alla QuestionPacks
    // som tidigare pekade på denna kategori.
    public async Task DeleteCategoryAsync(Guid categoryId)
    {
        var filter = Builders<Category>.Filter.Eq(x => x.Id, categoryId);
        await _categories.DeleteOneAsync(filter);

        // Rensa koppling från packs som pekar på borttagen kategori
        var packFilter = Builders<QuestionPack>.Filter.Eq(x => x.CategoryId, categoryId);
        var update = Builders<QuestionPack>.Update
            .Set(x => x.CategoryId, null)
            .Set(x => x.CategoryName, null);
        await _packs.UpdateManyAsync(packFilter, update);
    }
}

