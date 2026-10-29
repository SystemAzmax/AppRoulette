using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// OneDrive（App Folder）へのルーレットグループ同期を担う
/// サービスのインターフェース。
/// </summary>
public interface ICloudSyncService
{
    /// <summary>
    /// 同期先のフォルダーが検出され、同期が利用可能かどうかを
    /// 取得します。
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 1グループ分のデータをクラウドへアップロードします。
    /// </summary>
    /// <param name="group">アップロードするグループデータ。</param>
    /// <returns>成功した場合は true。</returns>
    Task<bool> UploadGroupAsync(SyncGroupData group);

    /// <summary>
    /// 指定グループのデータをクラウドからダウンロードします。
    /// </summary>
    /// <param name="groupId">グループID。</param>
    /// <returns>グループデータ。存在しない場合は null。</returns>
    Task<SyncGroupData?> DownloadGroupAsync(int groupId);

    /// <summary>
    /// クラウド上の全グループデータをダウンロードします。
    /// </summary>
    /// <returns>グループデータの一覧。存在しない場合は空。</returns>
    Task<IReadOnlyList<SyncGroupData>> DownloadAllGroupsAsync();

    /// <summary>
    /// 全グループのデータをクラウドへ一括アップロードします。
    /// </summary>
    /// <param name="groups">アップロードするグループデータ一覧。</param>
    /// <returns>成功した場合は true。</returns>
    Task<bool> UploadAllGroupsAsync(IReadOnlyList<SyncGroupData> groups);
}
