using System.Text.Json;
using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// JSON ファイルを使用してルーレットデータを永続化するサービスです。
/// </summary>
public class JsonDataPersistenceService : IDataPersistenceService
{
    private static readonly string DEFAULT_DATA_DIR =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AppRoulette");

    private static readonly JsonSerializerOptions JSON_OPTIONS =
        new() { WriteIndented = true };

    private readonly string _dataDir;
    private readonly string _dataFilePath;

    /// <summary>
    /// 既定のデータディレクトリ（%LOCALAPPDATA%\AppRoulette）を使用して
    /// <see cref="JsonDataPersistenceService"/> を初期化します。
    /// </summary>
    public JsonDataPersistenceService()
        : this(DEFAULT_DATA_DIR) { }

    /// <summary>
    /// 指定したデータディレクトリを使用して
    /// <see cref="JsonDataPersistenceService"/> を初期化します。
    /// テスト時に任意のディレクトリを注入するために使用します。
    /// </summary>
    /// <param name="dataDir">データファイルを格納するディレクトリのパス。</param>
    public JsonDataPersistenceService(string dataDir)
    {
        _dataDir = dataDir;
        _dataFilePath = Path.Combine(_dataDir, "data.json");
    }

    /// <summary>
    /// JSON ファイルから全グループのデータを非同期で読み込みます。
    /// ファイルが存在しない場合はデフォルトのグループ一覧を返します。
    /// </summary>
    /// <returns>グループ一覧。</returns>
    public async Task<IReadOnlyList<RouletteGroup>> LoadGroupsAsync()
    {
        if (!File.Exists(_dataFilePath))
        {
            return CreateDefaultGroups();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_dataFilePath)
                .ConfigureAwait(false);
            var groups = JsonSerializer.Deserialize<List<RouletteGroup>>(
                json, JSON_OPTIONS);
            return groups ?? CreateDefaultGroups();
        }
        catch (JsonException)
        {
            return CreateDefaultGroups();
        }
    }

    /// <summary>
    /// 全グループのデータを JSON ファイルに非同期で保存します。
    /// </summary>
    /// <param name="groups">保存するグループ一覧。</param>
    public async Task SaveGroupsAsync(IReadOnlyList<RouletteGroup> groups)
    {
        Directory.CreateDirectory(_dataDir);

        var json = JsonSerializer.Serialize(groups, JSON_OPTIONS);
        await File.WriteAllTextAsync(_dataFilePath, json)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// デフォルトのグループ一覧（グループ1〜3、アイテムなし）を生成します。
    /// </summary>
    /// <returns>デフォルトのグループ一覧。</returns>
    private static IReadOnlyList<RouletteGroup> CreateDefaultGroups()
    {
        var groups = new List<RouletteGroup>(RouletteGroup.GROUP_COUNT);
        for (var i = 1; i <= RouletteGroup.GROUP_COUNT; i++)
        {
            groups.Add(new RouletteGroup(i, $"グループ{i}"));
        }

        return groups;
    }
}
