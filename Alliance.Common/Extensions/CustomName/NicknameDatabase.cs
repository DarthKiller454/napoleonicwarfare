using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Alliance.Common.Utilities.Logger;

public static class NicknameDatabase
{
    private static CancellationTokenSource _refreshCts;
    private static volatile Dictionary<string, string> _nicknameMap = new();
    private static readonly HttpClient _httpClient = new(); 
    private static readonly object _refreshLock = new();

    public static IReadOnlyDictionary<string, string> NicknameMap
    {
        get
        {
            lock (_lock)
                return new Dictionary<string, string>(_nicknameMap);
        }
    }
    private static readonly object _lock = new();

    public static void Initialize()
    {
        try
        {
            var response = _httpClient.GetStringAsync("http://109.230.239.42/username_api/load_usernames.php")
                                 .GetAwaiter().GetResult();

            var nicknames = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

            if (nicknames != null)
            {
                Load(nicknames);
                Log($"[PHP Sync] Loaded {nicknames.Count} nicknames from PHP backend", LogLevel.Information);
            }
            else
            {
                Log("[PHP Sync] Received null when deserializing nickname list", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            Log($"[PHP Sync] Failed to load nickname database: {ex.Message}", LogLevel.Error);
        }
    }

    private static void Load(Dictionary<string, string> newMap)
    {
        var filtered = new Dictionary<string, string>();

        foreach (var kvp in newMap)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                filtered[kvp.Key] = kvp.Value;
        }

        lock (_lock)
        {
            _nicknameMap = filtered;
        }
    }
    public static string GetNickname(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return null;

        lock (_lock)
        {
            return _nicknameMap.TryGetValue(playerId, out var name) ? name : null;
        }
    }
    public static void StartAutoRefresh(TimeSpan interval)
    {
        lock (_refreshLock)
        {
            if (_refreshCts != null)
                return;

            _refreshCts = new CancellationTokenSource();
            var token = _refreshCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        Initialize(); // Refresh the nickname list
                    }
                    catch (Exception ex)
                    {
                        Log($"[PHP Sync] Auto-refresh failed: {ex}", LogLevel.Error); // include full exception
                    }

                    try
                    {
                        await Task.Delay(interval, token); // Wait for next interval
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }
    }

    public static void StopAutoRefresh()
    {
        lock (_refreshLock)
        {
            if (_refreshCts != null)
            {
                _refreshCts.Cancel();
                _refreshCts.Dispose();
                _refreshCts = null;
            }
        }
    }
}