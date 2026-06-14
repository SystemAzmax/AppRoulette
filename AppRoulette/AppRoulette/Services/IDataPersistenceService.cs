using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// ルーレットデータの永続化を担うサービスのインターフェース。
/// 将来的な DB 化に備えて抽象化しています。
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
}
