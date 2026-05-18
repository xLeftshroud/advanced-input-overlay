using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdvancedInputOverlay.Models;

namespace AdvancedInputOverlay.Services;

/// <summary>
/// Loads / persists <see cref="AppState"/> as <c>config.json</c> next to the running exe.
/// </summary>
public sealed class ConfigStore : IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    private const int DebounceMs = 500;

    private readonly string _path;
    private readonly object _gate = new();
    private readonly System.Threading.Timer _timer;
    private AppState _state = new();
    private bool _disposed;

    public ConfigStore()
        : this(Path.Combine(AppContext.BaseDirectory, "config.json"))
    {
    }

    public ConfigStore(string path)
    {
        _path = path;
        _timer = new System.Threading.Timer(_ => FlushNow(), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
    }

    public AppState State
    {
        get { lock (_gate) return _state; }
    }

    public AppState Load()
    {
        lock (_gate)
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    var loaded = JsonSerializer.Deserialize<AppState>(json, JsonOpts);
                    if (loaded != null)
                    {
                        _state = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                // Corrupt config: keep defaults, surface diagnostic to console (debugger only).
                System.Diagnostics.Debug.WriteLine($"[ConfigStore] Failed to load: {ex.Message}");
            }
            return _state;
        }
    }

    /// <summary>Schedule a save in <see cref="DebounceMs"/>; subsequent calls reset the timer.</summary>
    public void SaveDebounced()
    {
        if (_disposed) return;
        _timer.Change(DebounceMs, System.Threading.Timeout.Infinite);
    }

    /// <summary>Write to disk immediately (e.g., on app exit).</summary>
    public void FlushNow()
    {
        lock (_gate)
        {
            if (_disposed) return;
            try
            {
                var json = JsonSerializer.Serialize(_state, JsonOpts);
                var tmp = _path + ".tmp";
                File.WriteAllText(tmp, json);
                // Atomic-ish replace
                if (File.Exists(_path))
                {
                    File.Replace(tmp, _path, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tmp, _path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigStore] Failed to save: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
        }
        _timer.Dispose();
        FlushNow();
    }
}
