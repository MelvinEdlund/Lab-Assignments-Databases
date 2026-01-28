namespace Labb3_Quiz_MongoDB.Models;

// Ett enskilt svarsalternativ till en fråga.
public class AnswerOption
{
    // Texten som visas i UI:t.
    public string Text { get; set; } = "";

    // True om detta är det rätta svaret på frågan.
    public bool IsCorrect { get; set; }
}

