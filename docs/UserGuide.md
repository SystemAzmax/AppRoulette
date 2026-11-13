# AppRoulette User Guide

English | [日本語](UserGuide_ja.md)

AppRoulette spins a roulette wheel to pick one item from the items you register.
You can make some items more likely to be picked (weights), and leave picked items out of later draws.

The app's screens are in Japanese only. This guide shows each label in Japanese as it appears on the screen, followed by an English translation.
Windows shows the app as "AppRoulette" when the display language is not Japanese, and as "かんたんルーレット" when it is Japanese.

- [1. Getting Started](#1-getting-started)
- [2. Screens and Features](#2-screens-and-features)
- [3. Troubleshooting](#3-troubleshooting)
- [4. Uninstalling](#4-uninstalling)
- [Contact](#contact)

## 1. Getting Started

### Start the App

Start "AppRoulette" from the Start menu.
The first time you start the app, there are nine empty lists, "Roulette1" to "Roulette9".

The window has three areas, from left to right.

| Area | Contents |
| --- | --- |
| Left | The roulette wheel |
| Center | The items of the selected list (the "詳細" (Details) and "テキスト" (Text) tabs), and the **スタート** (Start) button |
| Right | Selecting and managing lists, item operations, and the "データ" (Data) and "その他" (Other) tabs |

### Spin the Wheel for the First Time

1. Select the "テキスト" (Text) tab in the center.
2. Type the things to choose from, one per line. You can also copy and paste them from another app.

   ```
   Curry
   Ramen
   Sushi
   ```

3. Select **スタート** (Start). The wheel spins, and when it stops, the picked item appears in "🎉 結果" (Result).
4. Select **OK** to close it.

Your changes are saved automatically. There is no save command.

## 2. Screens and Features

### Spin the Wheel

Use any of the following. You can't spin again while the wheel is spinning.

- Select the **スタート** (Start) button
- Click inside the wheel
- Press Space or Enter (except while typing text)

Unchecked items don't appear on the wheel and are left out of the draw.

### Register Items (the "詳細" (Details) Tab)

Edit the items in a table. Each list can have up to 30 items.
The number of items appears to the right of the tabs, such as "5/30".

| Item | Description |
| --- | --- |
| 有効 (Enabled) | Uncheck an item to leave it out of the draw for now. When two or fewer items are enabled, you can't uncheck any more |
| 名前 (Name) | The name shown on the wheel. Items with an empty name are not saved |
| Weight | A number from 1 to 5. Larger values make the item more likely to be picked (the default is 1). An item with a weight of 2 is twice as likely to be picked as an item with a weight of 1 |

Below the table, "最大30件です" (30 items maximum) appears when the list has 30 items, and "重複: ..." (Duplicates) appears when items have the same name.
Items with the same name still work, but you can't tell them apart on the wheel.

### Register Items (the "テキスト" (Text) Tab)

Type one item per line as "name,weight". If you omit the weight, it is 1.

```
Curry,3
Ramen
Sushi,2
```

- A weight outside 1 to 5 is changed to 1 or 5.
- Lines after the 30th are not read.
- When you edit the text, each item keeps the "有効" (Enabled) check of the item with the same name.

### アイテム操作 (Item Operations)

The buttons under "アイテム操作" (Item Operations) on the right work on the table on the "詳細" (Details) tab.

| Button | Description |
| --- | --- |
| アイテム追加 (Add Item) | Adds "新しいアイテム" (New Item) to the end of the table |
| 選択削除 (Delete Selected) | Deletes the item selected in the table |
| 選択を上へ / 選択を下へ (Move Selected Up / Down) | Moves the selected item up or down by one |
| 一覧ソート (Sort List) | Sorts the items by name |
| 一覧シャッフル (Shuffle List) | Puts the items in random order |

### Use Several Lists

Select the list to use in the combo box at the top right. The position of the list appears to its right.
The items and the "当たったら除外" (Exclude on Win) setting are saved for each list.

| Item | Description |
| --- | --- |
| クリア (Clear) | Deletes all items in the selected list after you confirm. You can't undo this |
| (Save status) | Shows "保存中" (Saving), "保存済み" (Saved), and so on. If it shows "保存失敗" (Save failed), see [Troubleshooting](#保存できませんでした-save-failed-appears) |
| 当たったら除外 (Exclude on Win) | When on, the picked item is automatically left out of later draws (its "有効" (Enabled) check is cleared). When only one enabled item is left, no more items are excluded |
| The list name box and **変更** (Change) | Type a name for the list and select **変更** (Change) to save it. Until you do, "未保存" (Not saved) appears |
| リセット (Reset) | Sets the name box to "New Roulette". To save it, select **変更** (Change) |
| リストを追加 (Add List) | Adds a new empty list named "New Roulette" |
| リストを削除 (Delete List) | Deletes the selected list after you confirm. You can't undo this. You can't delete the last list |
| リストを複製 (Duplicate List) | Copies the selected list, including its items |
| リストを上へ / リストを下へ (Move List Up / Down) | Changes the order of the lists |

### データ (Data): CSV, Backup, and OneDrive Sync

These are on the "データ" (Data) tab at the lower right.

**CSV（選択中のリスト） (CSV, selected list)**

| Button | Description |
| --- | --- |
| CSVエクスポート (Export CSV) | Saves the items of the selected list to a CSV file. The first line is "アイテム名,Weight", and you can open the file in Excel and other apps |
| CSVインポート (Import CSV) | Reads a CSV file and replaces the items of the selected list. If the list has items, you confirm before they are replaced |

In a CSV file, write one item per line as "name,weight". The first line, "アイテム名,Weight", is optional.
Files in UTF-8, UTF-16, and Shift_JIS can be read.

**バックアップ（全リスト） (Backup, all lists)**

| Button | Description |
| --- | --- |
| バックアップ保存 (Save Backup) | Saves all lists and items to one file |
| バックアップ復元 (Restore Backup) | Restores all lists and items from a backup file. Your current data is replaced after you confirm. You can't undo this |

Before you replace your PC or reinstall the app, save a backup file with **バックアップ保存** (Save Backup).

**OneDrive 同期 (OneDrive Sync)**

This appears only if you use OneDrive on the PC. It syncs your lists with your other PCs through your OneDrive folder.

- When you change items, that list is saved to your OneDrive folder automatically.
- When the app starts, it reads only the selected list from your OneDrive folder.
- Select **今すぐ同期** (Sync Now) to sync all lists. For each list, the data that was changed more recently is used.
- The list name, the items, and the "当たったら除外" (Exclude on Win) setting are synced. Lists are synced only if they exist on both PCs; lists added on another PC are not added.
- The status shows "同期待機" (Waiting), "同期中" (Syncing), "同期済み" (Synced), or "同期失敗" (Sync failed).

After making changes on another PC, select **今すぐ同期** (Sync Now) before you use the app.

### その他 (Other)

These are on the "その他" (Other) tab at the lower right.

| Button | Description |
| --- | --- |
| ウィンドウサイズをリセット (Reset Window Size) | Returns the window to its default size for your screen |
| 常に最前面表示 (Always on Top) | When on, keeps the window in front of other windows |

The window opens with the same size and position the next time you start the app.

### アプリの情報 (App Information)

These are on the "その他" (Other) tab.

| Item | Description |
| --- | --- |
| このアプリについて (About this app) | Shows the app version and other information |
| ライセンス情報 (License information) | Shows the licenses of the software this app uses |

## 3. Troubleshooting

### I can't select the Start button

You can't select it while the wheel is spinning or when no items are enabled.
Register items, or check "有効" (Enabled) on the "詳細" (Details) tab.

### I can't uncheck an item

When two or fewer items are enabled, you can't uncheck them.
Enable other items or add more items.

### I want to bring back an item excluded by "当たったら除外" (Exclude on Win)

On the "詳細" (Details) tab, check "有効" (Enabled) for that item again.

### I want to register more than 30 items

Each list can have up to 30 items. Split them into several lists.

### "保存できませんでした" (Save failed) appears

A red bar appears at the top of the window and shows what couldn't be saved.
Restart the app and try again.
If it keeps happening, open the log with **ログを開く** (Open Log) on the bar, and let us know through [Contact](#contact) with its contents.

### "OneDrive 同期" (OneDrive Sync) doesn't appear

It appears only if OneDrive is set up on the PC.
Sign in to the OneDrive app in Windows, and then restart AppRoulette.

### My lists don't sync to my other PC

- Check that the OneDrive app has finished syncing, using the OneDrive icon on the taskbar. If you are offline, changes are synced after you connect.
- After the files are synced, select **今すぐ同期** (Sync Now) in AppRoulette.
- If "同期失敗" (Sync failed) appears, wait a moment and select **今すぐ同期** (Sync Now) again.

### I want to move my data to another PC

1. On the old PC, save a file with **バックアップ保存** (Save Backup) on the "データ" (Data) tab.
2. Copy the file to the new PC.
3. On the new PC, select the file with **バックアップ復元** (Restore Backup).

## 4. Uninstalling

In the Start menu, right-click "AppRoulette" and choose "Uninstall".

- Uninstalling deletes the lists and items, settings, and error logs saved on your PC.
- Files in your OneDrive folder (`Apps\AppRoulette`) are not deleted. If you don't need them, delete them in File Explorer.
- CSV files and backup files that you saved are not deleted.

## Contact

To report a bug or make a request, please use [GitHub Issues](https://github.com/SystemAzmax/AppRoulette/issues).
For privacy, see the [Privacy Policy](PrivacyPolicy.md).
