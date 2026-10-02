using System.Net;
using ProfileGenerator.Domain;

namespace ProfileGenerator.Data;

internal static class HttpRetry
{
    private const int MaxAttempts = 3;

    public static async Task<string> SendForTextAsync(
        HttpClient http,
        Func<HttpRequestMessage> createRequest,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            string failure;
            try
            {
                using var request = createRequest();
                using var response = await http.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync(cancellationToken);

                failure = $"HTTP {(int)response.StatusCode}";
                if (!IsTransient(response.StatusCode))
                    throw new DataException($"the server answered {failure}.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       && !cancellationToken.IsCancellationRequested)
            {
                failure = $"network failure (technical detail: {ex.Message})";
            }

            if (attempt == MaxAttempts)
                throw new DataException($"{failure} after {MaxAttempts} attempts.");

            await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
        }
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests || (int)status >= 500;
}
