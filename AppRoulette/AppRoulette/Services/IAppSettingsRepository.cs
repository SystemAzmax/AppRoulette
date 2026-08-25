namespace AppRoulette.Services;

/// <summary>
/// アプリケーション設定値へのデータアクセスを定義するインターフェースです。
/// </summary>
public interface IAppSettingsRepository
{
    /// <summary>
    /// 最後に選択されたグループ ID を非同期で取得します。
    /// </summary>
    /// <returns>グループ ID。未保存の場合は 0。</returns>
    Task<int> GetLastSelectedGroupIdAsync();

    /// <summary>
    /// 最後に選択されたグループ ID を非同期で保存します。
    /// </summary>
    /// <param name="groupId">保存するグループ ID。</param>
    Task SaveLastSelectedGroupIdAsync(int groupId);
}
