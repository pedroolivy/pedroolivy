using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProfileGenerator.Domain;

namespace ProfileGenerator.Data;

public sealed class SnapshotStore(string filePath)
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public ContributionYear? Load()
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var file = JsonSerializer.Deserialize<SnapshotFile>(File.ReadAllText(filePath), ReadOptions)
                       ?? throw new DataException("the file is empty.");

            var days = file.Days.Select((count, index) => new ContributionDay(file.From.AddDays(index), count)).ToList();
            var year = ContributionYear.FromDays(
                file.Login, ContributionSourceNames.Parse(file.Source), days, file.Total);
            if (file.To != year.To)
                throw new DataException("the \"to\" field does not match the list of days.");

            return year;
        }
        catch (DataException ex)
        {
            throw new DataException($"invalid snapshot at {filePath}: {ex.Message} Delete the file to start over.", ex);
        }
        catch (JsonException ex)
        {
            throw new DataException(
                $"invalid snapshot at {filePath}: the JSON is malformed or not in the expected format " +
                $"(technical detail: {ex.Message}) Delete the file to start over.", ex);
        }
    }

    public void Save(ContributionYear year)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, Serialize(year), Utf8WithoutBom);
    }

    private static string Serialize(ContributionYear year)
    {
        var text = new StringBuilder();
        text.Append("{\n");
        text.Append($"  \"login\": {JsonSerializer.Serialize(year.Login)},\n");
        text.Append($"  \"from\": \"{year.From:O}\",\n");
        text.Append($"  \"to\": \"{year.To:O}\",\n");
        text.Append($"  \"source\": \"{ContributionSourceNames.Format(year.Source)}\",\n");
        text.Append($"  \"total\": {year.Total},\n");
        text.Append("  \"days\": [\n");
        text.AppendJoin(",\n", year.Days.Chunk(7).Select(block => "    " + string.Join(", ", block.Select(day => day.Count))));
        text.Append("\n  ]\n}\n");
        return text.ToString();
    }

    private sealed class SnapshotFile
    {
        public required string Login { get; init; }
        public required DateOnly From { get; init; }
        public required DateOnly To { get; init; }
        public required string Source { get; init; }
        public required int Total { get; init; }

        public required List<int> Days { get; init; }
    }
}
