using System.Diagnostics;
using System.Net.Http.Json;
using System.Web;

namespace ResoniteWikiMine.MediaWiki;

/// <summary>
/// Helper functions for working with the MediaWiki API.
/// </summary>
public static class MediawikiApi
{
    /// <summary>
    /// Create a chain of queries that that follow MediaWiki's continuation system to pull in a full response set.
    /// </summary>
    public static async IAsyncEnumerable<T> MakeContinueEnumerable<T>(HttpClient httpClient, string url)
    {
        QueryResponse<T> response;
        var continuedUrl = url;
        do
        {
            var sw = Stopwatch.StartNew();
            response = await httpClient.GetFromJsonAsync<QueryResponse<T>>(continuedUrl) ?? throw new InvalidDataException();
            Console.WriteLine(sw.Elapsed);

            yield return response.Query;

            continuedUrl = AddContinueToUrl(url, response.Continue);
        } while (response.Continue != null);
    }

    public static string AddContinueToUrl(string baseUrl, Dictionary<string, string>? continueProps)
    {
        if (continueProps == null)
            return baseUrl;

        var suffix = string.Join('&',
            continueProps.Select(x => $"{HttpUtility.UrlEncode(x.Key)}={HttpUtility.UrlEncode(x.Value)}"));
        return baseUrl + "&" + suffix;
    }
}

public sealed record QueryResponse<T>(T Query, Dictionary<string, string>? Continue);
