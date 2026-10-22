using System.Text.Json;

namespace AppRoulette.Services;

/// <summary>
/// グループごとのローカル最終更新日時（UTC）をJSONファイルに
/// 永続化するストアです。クラウド同期の競合解決に使用します。
/// </summary>
public class SyncTimestampStore
{
    private static readonly string DEFAULT_FILE_PATH =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "AppRoulette",
            "syncTimestamps.json");

    private readonly string _filePath;

    private Dictionary<int, DateTime>? _cache;

    /// <summary>
    /// 既定のファイルパスで <see cref="SyncTimestampStore"/> を
    /// 初期化します。
    /// </summary>
    public SyncTimestampStore()
        : this(DEFAULT_FILE_PATH) { }

    /// <summary>
    /// 指定したファイルパスで <see cref="SyncTimestampStore"/> を
    /// 初期化します。テスト時に使用します。
    /// </summary>
    /// <param name="filePath">タイムスタンプ保存ファイルのパス。</param>
    public SyncTimestampStore(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// 指定グループのローカル最終更新日時を取得します。
    /// </summary>
    /// <param name="groupId">グループID。</param>
    /// <returns>最終更新日時（UTC）。記録がない場合は null。</returns>
    public async Task<DateTime?> GetAsync(int groupId)
    {
        var cache = await LoadAsync().ConfigureAwait(false);
        return cache.TryGetValue(groupId, out var value) ? value : null;
    }

    /// <summary>
    /// 指定グループのローカル最終更新日時を保存します。
    /// </summary>
    /// <param name="groupId">グループID。</param>
    /// <param name="timestampUtc">最終更新日時（UTC）。</param>
    public async Task SetAsync(int groupId, DateTime timestampUtc)
    {
        var cache = await LoadAsync().ConfigureAwait(false);
        cache[groupId] = timestampUtc;
        await SaveAsync(cache).ConfigureAwait(false);
    }

    /// <summary>
    /// タイムスタンプ一覧をファイルから読み込みます。
    /// 読み込み済みの場合はキャッシュを返します。
    /// </summary>
    /// <returns>グループIDと最終更新日時の辞書。</returns>
    private async Task<Dictionary<int, DateTime>> LoadAsync()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        if (!File.Exists(_filePath))
        {
            _cache = new Dictionary<int, DateTime>();
            return _cache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath)
                .ConfigureAwait(false);
            _cache = JsonSerializer
                .Deserialize<Dictionary<int, DateTime>>(json)
                ?? new Dictionary<int, DateTime>();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            _cache = new Dictionary<int, DateTime>();
        }

        return _cache;
    }

    /// <summary>
    /// タイムスタンプ一覧をファイルに保存します。
    /// </summary>
    /// <param name="cache">保存する辞書。</param>
    private async Task SaveAsync(Dictionary<int, DateTime> cache)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(cache);
            await File.WriteAllTextAsync(_filePath, json)
                .ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            ApplicationLogger.LogError("同期タイムスタンプ保存", ex);
        }
    }
}
