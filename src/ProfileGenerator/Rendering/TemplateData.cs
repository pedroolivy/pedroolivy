using System.Globalization;
using ProfileGenerator.Domain;

namespace ProfileGenerator.Rendering;

public static class TemplateData
{
    public static IReadOnlyDictionary<string, string> From(ContributionYear year) =>
        new Dictionary<string, string>
        {
            ["asOf"] = year.To.ToString("O", CultureInfo.InvariantCulture),
        };
}
