using QuranVideoMaker.Properties;
using System.Net.Http;
using System.Net.Http.Json;

namespace QuranVideoMaker.Utilities
{
    public static class UpdateChecker
    {
        public static Version GetCurrentVersion()
        {
            return Version.Parse(Resources.VERSION.Trim());
        }

        public static async Task<UpdateCheckResult> CheckForUpdates()
        {
            var result = new UpdateCheckResult();

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "QuranVideoMaker");
                    var latestRelease = await client.GetFromJsonAsync<LatestReleaseInfo>("https://api.github.com/repos/QuranVideoMaker/QuranVideoMaker/releases/latest");

                    if (latestRelease != null)
                    {
                        result.LatestVersion = Version.Parse(latestRelease.TagName.Replace("v", string.Empty));
                        result.ReleaseInfo = latestRelease;
                    }
                }
            }
            catch (Exception ex)
            {
                // we don't care about errors here
            }

            return result;
        }
    }
}