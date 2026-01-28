namespace Labb3_Quiz_MongoDB.Services.Trivia;

public interface ITriviaService
{
    Task<List<OpenTdbCategory>> GetCategoriesAsync(CancellationToken ct = default);
    Task<OpenTdbQuestionResponse> GetQuestionsAsync(
        int amount, int? categoryId, string? difficulty, CancellationToken ct = default);
}

