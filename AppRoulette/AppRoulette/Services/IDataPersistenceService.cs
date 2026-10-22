using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// JSON ファイル形式の旧ルーレットデータ永続化を担うサービスのインターフェース。
/// 現行アプリ本体のグループ・アイテム・設定値は SQLite リポジトリで永続化します。
/// </summary>
public interface IDataPersistenceService
{
    /// <summary>
    /// 全グループのデータを非同期で読み込みます。
    /// データが存在しない場合はデフォルト値を返します。
    /// </summary>
    /// <returns>グループ一覧。</returns>
    Task<IReadOnlyList<RouletteGroup>> LoadGroupsAsync();

    /// <summary>
    /// 全グループのデータを非同期で保存します。
    /// </summary>
    /// <param name="groups">保存するグループ一覧。</param>
    Task SaveGroupsAsync(IReadOnlyList<RouletteGroup> groups);

    /// <summary>
    /// 最後に選択されたグループIDを非同期で取得します。
    /// データが存在しない場合は 0 を返します。
    /// </summary>
    /// <returns>グループID（1～9）。データなしの場合は0。</returns>
    Task<int> GetLastSelectedGroupIdAsync();

    /// <summary>
    /// 最後に選択されたグループIDを非同期で保存します。
    /// </summary>
    /// <param name="groupId">グループID（1～9）。</param>
    Task SaveLastSelectedGroupIdAsync(int groupId);
}
