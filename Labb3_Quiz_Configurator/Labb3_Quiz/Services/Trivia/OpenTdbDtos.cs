namespace Labb3_Quiz_MongoDB.Services.Trivia;

// DTO:er för API-responser (JSON → C#-objekt via deserialisering)
public class OpenTdbCategoryListResponse
{
    public List<OpenTdbCategory> Trivia_Categories { get; set; } = new();
}

public class OpenTdbCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// Response-kod: 0=OK, 1=NoResults, 2=InvalidParam, 3=TokenNotFound, 4=TokenEmpty
public class OpenTdbQuestionResponse
{
    public int Response_Code { get; set; }
    public List<OpenTdbQuestion> Results { get; set; } = new();
}

// Text är URL- och HTML-kodad (måste dekodas)
public class OpenTdbQuestion
{
    public string Category { get; set; } = "";
    public string Type { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public string Question { get; set; } = "";
    public string Correct_Answer { get; set; } = "";
    public List<string> Incorrect_Answers { get; set; } = new();
}

