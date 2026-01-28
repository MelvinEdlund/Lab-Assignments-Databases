namespace Labb3_Quiz_MongoDB.Models;

// En enskild fråga i ett QuestionPack.
public class Question
{
    // Själva frågetexten som visas för spelaren.
    public string Text { get; set; } = "";

    // Alla svarsalternativ. Vi förväntar oss normalt exakt 4 alternativ,
    // där exakt ett har IsCorrect = true.
    public List<AnswerOption> Options { get; set; } = new();
}