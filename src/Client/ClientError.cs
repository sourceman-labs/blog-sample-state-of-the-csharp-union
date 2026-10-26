using System.Text.Json;

namespace Client;

public sealed record ProblemDetailsError(int Status, string Title, IReadOnlyDictionary<string, string[]> Errors);

// Two of the three cases are exception types from the BCL. A union can name types that you do not own; a closed hierarchy cannot.
public union ClientError(HttpRequestException, ProblemDetailsError, JsonException);
