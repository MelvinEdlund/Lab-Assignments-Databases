using Labb3_Quiz_MongoDB.Models;

namespace Labb3_Quiz_MongoDB.Services;

// Abstraktion för all beständig lagring av quiz-data.
// I detta projekt implementeras den av MongoStorageService,
// men du kan enkelt byta till t.ex. fil- eller SQL-lagring i ett annat projekt.
public interface IStorageService
{
    // ----- Question Packs (CRUD) -----

    // Läs in alla frågepaket från databasen.
    Task<List<QuestionPack>> GetAllPacksAsync();

    // Skapa ett nytt frågepaket (INSERT).
    Task CreatePackAsync(QuestionPack pack);

    // Uppdatera befintligt frågepaket (REPLACE/UPSERT).
    Task UpdatePackAsync(QuestionPack pack);

    // Ta bort ett frågepaket helt.
    Task DeletePackAsync(Guid packId);


    // ----- Categories (CRUD) -----

    // Hämta alla kategorier som finns definierade.
    Task<List<Category>> GetAllCategoriesAsync();

    // Skapa en ny kategori.
    Task CreateCategoryAsync(Category category);

    // Ta bort en kategori och rensa kopplingar från QuestionPacks.
    Task DeleteCategoryAsync(Guid categoryId);
}
