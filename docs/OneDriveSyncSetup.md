# OneDrive フォルダー同期の仕組み（開発者向け）

このドキュメントは、AppRoulette の OneDrive フォルダー同期機能の
仕組みを説明します。

Microsoft Entra ID へのアプリ登録やサインインは**不要**です。
Windows の OneDrive クライアントがファイルをクラウドへ同期します。

## 前提条件

- ユーザーの PC で OneDrive クライアントがセットアップ済みであること
  （環境変数 `%OneDrive%` が設定され、フォルダーが存在すること）

## 保存場所

同期データは以下のパスに JSON ファイルとして保存されます。

```
%OneDrive%\Apps\AppRoulette\groups\{groupId}.json
```

## 動作概要

1. アプリ起動時、`OneDriveFolderSyncService` が `%OneDrive%`
   環境変数から OneDrive フォルダーを自動検出します。
2. 検出できた場合のみ、右パネルに「OneDrive 同期」セクションと
   「今すぐ同期」ボタンが表示されます。
3. アイテム保存時、選択中グループが自動的にファイルへ書き出されます。
4. 「今すぐ同期」ボタンで全グループを双方向同期します。
   各グループはタイムスタンプ（`SyncTimestampStore`）を比較し、
   新しい方（`SyncConflictResolver`）を採用します。
5. ファイルのクラウドへのアップロード・他端末へのダウンロードは
   OneDrive クライアントが自動で行います。

## 主要クラス

| クラス | 役割 |
| --- | --- |
| `OneDriveFolderSyncService` | `%OneDrive%` 検出とファイル読み書き |
| `ICloudSyncService` | 同期操作の抽象化 |
| `SyncGroupData` | 同期用 DTO（JSON シリアライズ対象） |
| `SyncConflictResolver` | タイムスタンプによる競合解決 |
| `SyncTimestampStore` | グループ毎の最終更新時刻の永続化 |

## トラブルシューティング

- **同期セクションが表示されない**:
  OneDrive クライアントが未セットアップです。
  OneDrive にサインインして `%OneDrive%` が設定されているか
  確認してください。
- **他端末に反映されない**:
  OneDrive クライアントの同期状態（タスクトレイのアイコン）を
  確認してください。オフラインの場合は接続後に反映されます。
