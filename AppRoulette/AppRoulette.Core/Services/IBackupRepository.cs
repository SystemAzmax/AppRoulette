using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// バックアップからの復元で、全データを置き換えるリポジトリのインターフェースです。
/// </summary>
public interface IBackupRepository
{
    /// <summary>
    /// 全リストとアイテムを、指定したリスト一覧で置き換えます。
    /// 置き換えは 1 つのトランザクションで行い、失敗した場合は何も変更しません。
    /// </summary>
    /// <param name="groups">置き換え後のリスト一覧（表示順）。</param>
    Task ReplaceAllAsync(IReadOnlyList<RouletteGroup> groups);
}
