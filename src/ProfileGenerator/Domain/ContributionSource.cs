namespace ProfileGenerator.Domain;

public enum ContributionSource
{
    GraphQl,
    Public,
}

public static class ContributionSourceNames
{
    public static string Format(ContributionSource source) => source switch
    {
        ContributionSource.GraphQl => "graphql",
        ContributionSource.Public => "public",
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };

    public static ContributionSource Parse(string name) => name switch
    {
        "graphql" => ContributionSource.GraphQl,
        "public" => ContributionSource.Public,
        _ => throw new DataException($"unknown source \"{name}\"."),
    };
}
