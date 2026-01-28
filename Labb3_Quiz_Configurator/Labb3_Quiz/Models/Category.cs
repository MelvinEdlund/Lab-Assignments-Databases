using MongoDB.Bson.Serialization.Attributes;

namespace Labb3_Quiz_MongoDB.Models;

// Kategori som ett QuestionPack kan tillhöra.
// Ligger i egen MongoDB-collection (categories).
public class Category
{
    // Primärnyckel i MongoDB.
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Namnet som användaren ser i dropdowns ("Programming", "Movies", ...).
    public string Name { get; set; } = "";
}
