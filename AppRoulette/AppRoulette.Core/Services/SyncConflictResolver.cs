using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// ローカルとクラウドの同期データの競合を
/// タイムスタンプ比較で解決するヘルパークラスです。
/// </summary>
public static class SyncConflictResolver
{
    /// <summary>
    /// ローカルとクラウドのグループデータを比較し、
    /// 最終更新日時が新しい方を返します。
    /// </summary>
    /// <param name="local">ローカルのデータ。null 可。</param>
    /// <param name="remote">クラウドのデータ。null 可。</param>
    /// <returns>
    /// 新しい方のデータ。両方 null の場合は null。
    /// 同時刻の場合はローカルを優先します。
    /// </returns>
    public static SyncGroupData? ChooseNewer(
        SyncGroupData? local, SyncGroupData? remote)
    {
        if (local is null)
        {
            return remote;
        }

        if (remote is null)
        {
            return local;
        }

        return remote.LastModifiedUtc > local.LastModifiedUtc
            ? remote
            : local;
    }

    /// <summary>
    /// クラウドのデータがローカルより新しいかどうかを判定します。
    /// </summary>
    /// <param name="local">ローカルのデータ。null 可。</param>
    /// <param name="remote">クラウドのデータ。null 可。</param>
    /// <returns>クラウドを採用すべき場合は true。</returns>
    public static bool ShouldApplyRemote(
        SyncGroupData? local, SyncGroupData? remote)
    {
        if (remote is null)
        {
            return false;
        }

        return local is null
            || remote.LastModifiedUtc > local.LastModifiedUtc;
    }
}
