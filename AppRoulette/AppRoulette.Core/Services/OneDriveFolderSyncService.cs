using System.Text.Json;
using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// ローカルの OneDrive フォルダーへファイルを読み書きして
/// ルーレットグループを同期するサービスです。
/// クラウドへの同期は Windows の OneDrive クライアントが行います。
/// ファイルは {ベースフォルダー}/groups/{groupId}.json として保存します。
/// </summary>
public class OneDriveFolderSyncService : ICloudSyncService
{
    /// <summary>OneDrive フォルダーを示す環境変数名。</summary>
    private const string ONEDRIVE_ENV_VAR = "OneDrive";

    /// <summary>OneDrive 内のアプリ用フォルダーの相対パス。</summary>
    private const string APP_FOLDER_RELATIVE_PATH = @"Apps\AppRoulette";

    /// <summary>同期ファイル格納フォルダー名。</summary>
    private const string GROUPS_FOLDER = "groups";

    private static readonly JsonSerializerOptions JSON_OPTIONS =
        new() { WriteIndented = true };

    private readonly string? _groupsDir;

    /// <summary>
    /// %OneDrive% 環境変数からベースフォルダーを自動検出して
    /// <see cref="OneDriveFolderSyncService"/> を初期化します。
    /// </summary>
    public OneDriveFolderSyncService()
        : this(DetectOneDriveBaseDir()) { }

    /// <summary>
    /// 指定したベースフォルダーで
    /// <see cref="OneDriveFolderSyncService"/> を初期化します。
    /// テスト時に任意のフォルダーを注入するために使用します。
    /// </summary>
    /// <param name="baseDir">
    /// アプリ用フォルダーのパス。null の場合は同期無効。
    /// </param>
    public OneDriveFolderSyncService(string? baseDir)
    {
        _groupsDir = baseDir is null
            ? null
            : Path.Combine(baseDir, GROUPS_FOLDER);
    }

    /// <inheritdoc/>
    public bool IsAvailable => _groupsDir is not null;

    /// <inheritdoc/>
    public async Task<bool> UploadGroupAsync(SyncGroupData group)
    {
        if (_groupsDir is null)
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(_groupsDir);
            var json = JsonSerializer.Serialize(group, JSON_OPTIONS);
            await File.WriteAllTextAsync(
                    GetGroupFilePath(group.GroupId), json)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (IsSyncException(ex))
        {
            ApplicationLogger.LogError("グループ書き込み", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<SyncGroupData?> DownloadGroupAsync(int groupId)
    {
        if (_groupsDir is null)
        {
            return null;
        }

        var filePath = GetGroupFilePath(groupId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        // 読み込めない場合は、ファイルが無い場合と区別できるよう例外をそのまま投げる
        var json = await File.ReadAllTextAsync(filePath)
            .ConfigureAwait(false);
        return JsonSerializer.Deserialize<SyncGroupData>(
            json, JSON_OPTIONS);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncGroupData>> DownloadAllGroupsAsync()
    {
        var results = new List<SyncGroupData>();
        if (_groupsDir is null || !Directory.Exists(_groupsDir))
        {
            return results;
        }

        try
        {
            foreach (var filePath in
                Directory.EnumerateFiles(_groupsDir, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(filePath)
                        .ConfigureAwait(false);
                    var data = JsonSerializer
                        .Deserialize<SyncGroupData>(json, JSON_OPTIONS);
                    if (data is not null)
                    {
                        results.Add(data);
                    }
                }
                catch (Exception ex) when (IsSyncException(ex))
                {
                    ApplicationLogger.LogError("グループ読み込み", ex);
                }
            }
        }
        catch (Exception ex) when (IsSyncException(ex))
        {
            ApplicationLogger.LogError("グループ一覧取得", ex);
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<bool> UploadAllGroupsAsync(
        IReadOnlyList<SyncGroupData> groups)
    {
        var allSucceeded = true;
        foreach (var group in groups)
        {
            if (!await UploadGroupAsync(group).ConfigureAwait(false))
            {
                allSucceeded = false;
            }
        }

        return allSucceeded;
    }

    /// <summary>
    /// %OneDrive% 環境変数からアプリ用フォルダーのパスを検出します。
    /// </summary>
    /// <returns>
    /// アプリ用フォルダーのパス。OneDrive 未検出時は null。
    /// </returns>
    private static string? DetectOneDriveBaseDir()
    {
        var oneDriveDir =
            Environment.GetEnvironmentVariable(ONEDRIVE_ENV_VAR);
        if (string.IsNullOrEmpty(oneDriveDir)
            || !Directory.Exists(oneDriveDir))
        {
            return null;
        }

        return Path.Combine(oneDriveDir, APP_FOLDER_RELATIVE_PATH);
    }

    /// <summary>
    /// 指定グループの同期ファイルパスを取得します。
    /// </summary>
    /// <param name="groupId">グループID。</param>
    /// <returns>同期ファイルのフルパス。</returns>
    private string GetGroupFilePath(int groupId)
    {
        return Path.Combine(_groupsDir!, $"{groupId}.json");
    }

    /// <summary>
    /// 同期処理で失敗として扱う例外かどうかを判定します。
    /// </summary>
    /// <param name="ex">判定する例外。</param>
    /// <returns>同期失敗として扱う例外なら true。</returns>
    private static bool IsSyncException(Exception ex)
    {
        return ex is IOException
            or UnauthorizedAccessException
            or JsonException;
    }
}
