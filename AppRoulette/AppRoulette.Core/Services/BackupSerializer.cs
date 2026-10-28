using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// 全リストのバックアップファイル（JSON）の作成と読み込みを担うクラスです。
/// </summary>
public static class BackupSerializer
{
    /// <summary>このアプリのバックアップであることを示す識別名。</summary>
    public const string APP_NAME = "AppRoulette";

    /// <summary>バックアップファイルの形式の現在のバージョン。</summary>
    public const int FORMAT_VERSION = 1;

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        // 日本語をエスケープせずに書き出し、バックアップファイルを人が読めるようにする
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    /// <summary>
    /// 全リストのバックアップファイルの内容を作成します。
    /// </summary>
    /// <param name="groups">バックアップするリスト一覧（表示順）。</param>
    /// <param name="createdUtc">バックアップの作成日時（UTC）。</param>
    /// <returns>バックアップファイルの内容（JSON）。</returns>
    public static string Serialize(IEnumerable<RouletteGroup> groups, DateTime createdUtc)
    {
        var backup = new BackupData
        {
            App = APP_NAME,
            FormatVersion = FORMAT_VERSION,
            CreatedUtc = createdUtc,
            Groups = groups.Select(BackupGroupData.FromGroup).ToList(),
        };

        return JsonSerializer.Serialize(backup, JSON_OPTIONS);
    }

    /// <summary>
    /// バックアップファイルの内容を読み込み、リスト一覧を作成します。
    /// リストの識別子は、表示順に 1 から振り直します。
    /// </summary>
    /// <param name="json">バックアップファイルの内容（JSON）。</param>
    /// <returns>復元するリスト一覧（表示順）。</returns>
    /// <exception cref="BackupFormatException">
    /// このアプリのバックアップではない場合や、内容が正しくない場合。
    /// </exception>
    public static List<RouletteGroup> Deserialize(string json)
    {
        BackupData? backup;
        try
        {
            backup = JsonSerializer.Deserialize<BackupData>(json, JSON_OPTIONS);
        }
        catch (JsonException ex)
        {
            throw new BackupFormatException(
                "バックアップファイルの形式が正しくありません。", ex);
        }

        if (backup is null || backup.App != APP_NAME)
        {
            throw new BackupFormatException(
                "AppRoulette のバックアップファイルではありません。");
        }

        if (backup.FormatVersion > FORMAT_VERSION)
        {
            throw new BackupFormatException(
                "新しいバージョンの AppRoulette で作られたバックアップです。"
                + "アプリを更新してから復元してください。");
        }

        if (backup.FormatVersion < 1)
        {
            throw new BackupFormatException(
                "バックアップファイルの形式が正しくありません。");
        }

        if (backup.Groups is null || backup.Groups.Count == 0)
        {
            throw new BackupFormatException("バックアップにリストがありません。");
        }

        return backup.Groups
            .Where(group => group is not null)
            .Select((group, index) => group.ToGroup(index + 1))
            .ToList();
    }
}
