using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Labb3_Quiz_MongoDB.Models;

// Svårighetsnivå för ett frågepaket.
// Lagrars som int i MongoDB, men läses/skrivs som enum i C#.
public enum PackDifficulty { Easy, Medium, Hard }

// Detta är "root"-modellen som representerar ett helt frågepaket.
// 1 dokument i MongoDB = 1 QuestionPack.
public class QuestionPack
{
    // Primärnyckel i MongoDB. Guid används även i C#-koden.
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Visningsnamn på paketet (t.ex. "C#-frågor").
    public string Name { get; set; } = "New Pack";

    // Vald svårighetsgrad (binds till ComboBox i UI).
    public PackDifficulty Difficulty { get; set; } = PackDifficulty.Medium;

    // Hur många sekunder spelaren får per fråga i detta paket.
    public int TimePerQuestionSeconds { get; set; } = 20;

    // Koppling till kategori (Category.Id) – används för dropdown i PackOptionsDialog.
    public Guid? CategoryId { get; set; }

    // Vi sparar även kategorinamnet "denormaliserat" för enklare visning,
    // så vi slipper alltid join:a mot categories-collection.
    public string? CategoryName { get; set; }

    // Alla frågor som tillhör paketet. Sparas som inbäddad lista i dokumentet.
    public List<Question> Questions { get; set; } = new();
}