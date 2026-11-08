using System.Net.Http;
using System.Text.Json;

namespace LiveCaptionsTranslator.utils
{
    public static class UpdateUtil
    {
        public const string GitHubRepoUrl = "https://github.com/Courtoisrl/LiveCaptions-Translator";
        public const string GitHubReleasesUrl = "https://github.com/Courtoisrl/LiveCaptions-Translator/releases";
        public const string GitHubLatestReleaseApi = "https://api.github.com/repos/Courtoisrl/LiveCaptions-Translator/releases/latest";

        public static async Task<string> GetLatestVersion()
        {
            string apiUrl = GitHubLatestReleaseApi;

            using var client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LiveCaptions-Translator");
            var response = await client.GetStringAsync(apiUrl);
            using var doc = JsonDocument.Parse(response);
            var latestVersionRaw = doc.RootElement.GetProperty("tag_name").GetString();
            var latestVersion = string.IsNullOrEmpty(latestVersionRaw)
                ? String.Empty
                : RegexPatterns.VersionNumber().Replace(latestVersionRaw, "");
            return NormalizeVersion(latestVersion);
        }

        public static string NormalizeVersion(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return string.Empty;

            var text = version.Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(1);

            return Version.TryParse(text, out var parsed)
                ? parsed.Revision > 0
                    ? parsed.ToString()
                    : parsed.Build > 0
                        ? parsed.ToString(3)
                        : parsed.ToString(2)
                : text;
        }


    }
}
