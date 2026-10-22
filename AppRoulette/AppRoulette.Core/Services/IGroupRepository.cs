namespace AppRoulette.Services;

using AppRoulette.Models;

/// <summary>
/// ルーレットグループへのデータアクセスを定義するインターフェースです。
/// </summary>
public interface IGroupRepository
{
    /// <summary>
    /// 有効なグループ一覧を表示順で非同期に取得します。
    /// </summary>
    /// <returns>有効なグループ一覧。</returns>
    Task<IReadOnlyList<RouletteGroup>> GetGroupsAsync();

    /// <summary>
    /// 既定グループが未登録の場合に補完します。
    /// </summary>
    /// <param name="groups">補完対象の既定グループ一覧。</param>
    Task EnsureDefaultGroupsAsync(IReadOnlyList<RouletteGroup> groups);

    /// <summary>
    /// 保存済みのグループ名を非同期で取得します。
    /// </summary>
    /// <returns>グループIDと表示名の対応表。</returns>
    Task<IReadOnlyDictionary<int, string>> GetGroupNamesAsync();

    /// <summary>
    /// 指定されたグループ名を非同期で保存します。
    /// </summary>
    /// <param name="groupId">保存対象グループの識別子。</param>
    /// <param name="displayName">保存する表示名。</param>
    Task SaveGroupNameAsync(int groupId, string displayName);

    /// <summary>
    /// 指定されたグループの「当たったら除外」設定を非同期で保存します。
    /// </summary>
    /// <param name="groupId">保存対象グループの識別子。</param>
    /// <param name="excludeOnWin">「当たったら除外」の有効状態。</param>
    Task SaveGroupExcludeOnWinAsync(int groupId, bool excludeOnWin);

    /// <summary>
    /// 新しいグループを非同期で追加します。
    /// </summary>
    /// <param name="displayName">追加するグループの表示名。</param>
    /// <param name="sortOrder">追加するグループの表示順。</param>
    /// <returns>追加されたグループ。</returns>
    Task<RouletteGroup> AddGroupAsync(string displayName, int sortOrder);

    /// <summary>
    /// 指定されたグループを非同期で削除済みにします。
    /// </summary>
    /// <param name="groupId">削除対象グループの識別子。</param>
    Task DeleteGroupAsync(int groupId);

    /// <summary>
    /// グループ一覧の表示順を非同期で保存します。
    /// </summary>
    /// <param name="groups">表示順を保存するグループ一覧。</param>
    Task SaveGroupOrderAsync(IReadOnlyList<RouletteGroup> groups);
}
