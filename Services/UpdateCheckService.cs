using DMInps.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DMInps.Services
{
    /// <summary>
    /// Servizio per il controllo degli aggiornamenti via GitHub API
    /// </summary>
    public class UpdateCheckService
    {
        private static readonly Lazy<UpdateCheckService> _instance =
            new Lazy<UpdateCheckService>(() => new UpdateCheckService());

        public static UpdateCheckService Instance => _instance.Value;

        private readonly HttpClient _httpClient;
        private const string GITHUB_API_URL = "https://api.github.com/repos/zndr/DMInpsPub/releases/latest";

        private UpdateCheckService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DMInps-UpdateChecker");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Controlla se sono disponibili aggiornamenti
        /// </summary>
        /// <param name="currentVersion">Versione corrente dell'applicazione (es. "1.0.8")</param>
        /// <returns>Risultato del controllo</returns>
        public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion)
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = currentVersion
            };

            try
            {
                Debug.WriteLine($"[UpdateCheck] Controllo aggiornamenti per versione {currentVersion}...");

                var response = await _httpClient.GetAsync(GITHUB_API_URL);

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"Errore API GitHub: {response.StatusCode}";
                    Debug.WriteLine($"[UpdateCheck] {result.ErrorMessage}");
                    return result;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var releaseData = JsonSerializer.Deserialize<JsonElement>(jsonString);

                // Estrai tag_name (versione)
                var tagName = releaseData.GetProperty("tag_name").GetString() ?? string.Empty;
                result.LatestVersion = tagName.TrimStart('v', 'V');
                result.ReleasePageUrl = releaseData.GetProperty("html_url").GetString();

                // Estrai body (note release)
                if (releaseData.TryGetProperty("body", out var bodyElement))
                {
                    result.ReleaseNotes = bodyElement.GetString();
                }

                // Cerca l'asset installer (.exe o .msi)
                if (releaseData.TryGetProperty("assets", out var assetsElement))
                {
                    foreach (var asset in assetsElement.EnumerateArray())
                    {
                        var assetName = asset.GetProperty("name").GetString() ?? string.Empty;

                        if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                            assetName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                        {
                            result.DownloadUrl = asset.GetProperty("browser_download_url").GetString();
                            Debug.WriteLine($"[UpdateCheck] Trovato installer: {assetName}");
                            break;
                        }
                    }
                }

                // Confronta versioni
                result.IsUpdateAvailable = CompareVersions(currentVersion, result.LatestVersion) < 0;

                Debug.WriteLine($"[UpdateCheck] Versione corrente: {currentVersion}, Ultima: {result.LatestVersion}, Aggiornamento disponibile: {result.IsUpdateAvailable}");

                return result;
            }
            catch (HttpRequestException ex)
            {
                result.ErrorMessage = $"Errore di rete: {ex.Message}";
                Debug.WriteLine($"[UpdateCheck] {result.ErrorMessage}");
                return result;
            }
            catch (TaskCanceledException)
            {
                result.ErrorMessage = "Timeout durante il controllo aggiornamenti";
                Debug.WriteLine($"[UpdateCheck] {result.ErrorMessage}");
                return result;
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"Errore parsing risposta: {ex.Message}";
                Debug.WriteLine($"[UpdateCheck] {result.ErrorMessage}");
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Errore imprevisto: {ex.Message}";
                Debug.WriteLine($"[UpdateCheck] {result.ErrorMessage}");
                return result;
            }
        }

        /// <summary>
        /// Confronta due versioni semantiche
        /// </summary>
        /// <returns>-1 se v1 minore di v2, 0 se uguali, 1 se v1 maggiore di v2</returns>
        private int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
                var v2Parts = version2.Split('.').Select(int.Parse).ToArray();

                int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

                for (int i = 0; i < maxLength; i++)
                {
                    int v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
                    int v2Part = i < v2Parts.Length ? v2Parts[i] : 0;

                    if (v1Part < v2Part) return -1;
                    if (v1Part > v2Part) return 1;
                }

                return 0;
            }
            catch
            {
                // Fallback: confronto stringa
                return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
