# AppRoulette

登録したアイテムの中から、ルーレットを回して 1 つを選ぶ Windows 用のデスクトップアプリです。WinUI 3 と .NET 10 で作っています。Microsoft Store 版と、インストール不要の exe 版があります。

## 機能

- **複数のリスト**: 最初は 9 個のリスト（Roulette1〜Roulette9）があり、追加・削除・複製・並べ替え・名前の変更ができます
- **アイテムの編集**: 1 つのリストに最大 30 個のアイテムを登録できます。表の形で編集する「詳細」タブと、1 行に 1 つずつ入力・貼り付けできる「テキスト」タブがあります
- **重み付け（Weight）**: アイテムごとに 1〜5 の Weight を指定でき、値が大きいほど当たりやすくなります
- **抽選からの除外**: チェックを外したアイテムは抽選から外れます。「当たったら除外」をオンにすると、当たったアイテムを自動で外します
- **回転アニメーション**: 減速しながら止まるルーレットを、Win2D で描画します
- **自動保存**: 変更は自動で SQLite のデータベースに保存されます
- **CSV のインポート・エクスポート**: 選択中のリストのアイテムを、CSV ファイルで読み書きできます
- **バックアップと復元**: すべてのリストとアイテムを 1 つのファイルに保存し、そこから復元できます
- **OneDrive 同期**: OneDrive を使っている PC では、OneDrive フォルダーを通じて、ほかの PC とリストを同期できます
- **ウィンドウ**: 前回のサイズと位置で開きます。常に最前面に表示することもできます
- **エラーの通知**: 保存の失敗や予期しないエラーをウィンドウの上部に表示し、ログファイルに記録します

## 技術スタック

- **フレームワーク**: .NET 10
- **UI フレームワーク**: WinUI 3 (Windows App SDK)
- **描画**: Win2D
- **データ保存**: SQLite (Microsoft.Data.Sqlite)
- **アーキテクチャ**: MVVM (Model-View-ViewModel)
- **Community Toolkit**: MVVM Toolkit によるプロパティバインディングとコマンド
- **テスト**: xUnit
- **配布**: MSIX パッケージ（Microsoft Store）と、.NET と Windows App SDK を同梱した exe

## プロジェクト構造

```
AppRoulette.slnx                  # ソリューション
AppRoulette/
├── AppRoulette/                  # アプリ本体（WinUI 3）
│   ├── App.xaml(.cs)             # 起動処理、予期しない例外の処理
│   ├── MainWindow.xaml(.cs)      # メイン画面
│   ├── Services/                 # ダイアログ、ウィンドウの位置とサイズ、アプリの情報
│   ├── Views/                    # ルーレットの描画（Win2D）
│   └── ThirdPartyNotices.txt     # 使用しているソフトウェアのライセンス表示
├── AppRoulette.Core/             # WinUI に依存しない処理
│   ├── Models/                   # リスト、アイテム、同期・バックアップ用のデータ
│   ├── Services/                 # SQLite への保存、OneDrive 同期、バックアップ、CSV、ログ
│   └── ViewModels/               # 画面の状態と処理、回転アニメーションの計算
├── AppRoulette.Tests/            # ユニットテスト
└── AppRoulette (Package)/        # ストア版の MSIX パッケージ（マニフェストとパッケージ画像）
docs/                             # 開発者向けの文書（ビルド方法、OneDrive 同期の仕組み）
tools/                            # アイコン、パッケージ画像、ライセンス表示を作るスクリプト
```

## 主要コンポーネント

### Models（AppRoulette.Core）
- **RouletteGroup**: リスト。表示名、最大 30 個のアイテム、「当たったら除外」の設定を持ちます
- **RouletteItem**: アイテム。名前、Weight、有効・無効を持ちます

### ViewModels（AppRoulette.Core）
- **MainViewModel**: リストとアイテムの管理、抽選、保存、同期、エラーの通知を担います
- **SpinAnimationViewModel**: 回転アニメーションの状態と、止まる位置の計算を担います

### Services（AppRoulette.Core）
- **SqliteGroupRepository / SqliteItemRepository / SqliteAppSettingsRepository**: SQLite への読み書き
- **DatabaseInitializer**: データベースとテーブルの作成
- **OneDriveFolderSyncService / SyncTimestampStore / SyncConflictResolver**: OneDrive フォルダーを使った同期
- **BackupSerializer / SqliteBackupRepository**: バックアップの作成と復元
- **CsvFileEncoding**: CSV ファイルの文字コードの判別と変換
- **ApplicationLogger / ErrorLogFile**: エラーのログ
- **RandomService**: 抽選に使う乱数

### Views（AppRoulette）
- **MainWindow**: メイン画面
- **RouletteRenderer**: Win2D を使ったルーレットの描画
- **ContentDialogService**: 確認やメッセージのダイアログ
- **WindowPositionService / WindowSizeCalculator**: ウィンドウの位置とサイズの保存と計算

## 使用開始

### 前提条件
- Windows 10 バージョン 1809 以降、または Windows 11
- .NET 10 SDK
- Visual Studio 2026 以降（Community Edition 対応）

### ビルドと実行

1. リポジトリをクローン:
   ```bash
   git clone https://github.com/SystemAzmax/AppRoulette.git
   ```

2. Visual Studio で `AppRoulette.slnx` を開きます

3. **AppRoulette (Package)** をスタートアップ プロジェクトに設定し、F5 で実行します

- `AppRoulette` プロジェクトのビルド出力（`bin\...\AppRoulette.exe`）はパッケージ用のため、直接起動すると落ちます。
- exe 版の発行と、ストアに提出するパッケージの作り方は、[ビルド方法](docs/BuildGuide.md)を参照してください。

## 使用方法

1. **リストを選ぶ**: 右上のコンボボックスで、使うリストを選びます
2. **アイテムを登録する**: 「詳細」タブの **アイテム追加** で追加し、名前と Weight を入力します。「テキスト」タブでは、1 行に 1 つずつ「名前,Weight」の形で入力・貼り付けできます（Weight を省略すると 1）
3. **ルーレットを回す**: **スタート** ボタン、ルーレットのクリック、または Space / Enter キー（テキストの入力中を除く）で回します
4. **当たったアイテムを外す**: 次の抽選から外したいときは、**当たったら除外** をオンにします
5. **保存**: 変更は自動で保存されます

## アーキテクチャの詳細

### MVVM パターン
- **Model**: `RouletteGroup` と `RouletteItem` がデータを表します
- **View**: `MainWindow.xaml` で画面を定義し、`x:Bind` で ViewModel と結び付けます
- **ViewModel**: `MainViewModel` が画面の状態と処理を持ちます

### プロジェクトの分け方
- WinUI に依存しない処理（ViewModel、保存、同期など）は `AppRoulette.Core` に置き、`AppRoulette.Tests` からテストします
- 画面や Windows の機能に依存する処理（ダイアログ、ウィンドウ、描画）は `AppRoulette` に置きます

### サービスの受け渡し
- `MainWindow` で各サービスを作り、`MainViewModel` のコンストラクターに渡します（DI コンテナーは使っていません）
- サービスはインターフェースで受け取るので、テストでは偽の実装（`AppRoulette.Tests/Fakes`）に差し替えます

## コーディング規約

詳しくは [.github/copilot-instructions.md](.github/copilot-instructions.md) を参照してください。

- **コメント**: メソッドには XML ドキュメントコメントを付けること
- **命名**: クラス/メソッドは PascalCase、変数は camelCase
- **定数**: 定数値は UPPER_SNAKE_CASE
- **プライベートフィールド**: アンダースコア接頭辞を使用（`_fieldName`）
- **フォーマット**: 80 文字行制限、スペースベースインデント

## テスト

プロジェクトには `AppRoulette.Tests` プロジェクト内にユニットテストが含まれます。以下でテストを実行:

```bash
dotnet test AppRoulette/AppRoulette.Tests/AppRoulette.Tests.csproj -c Release -p:Platform=x64
```

GitHub Actions（`.github/workflows/build.yml`）でも、`main` と `product*` ブランチへのプッシュなどのときに、テストと exe 版の発行を行います。

## データとファイル

### 保存場所

| | ストア版 | exe 版 |
| --- | --- | --- |
| データ（SQLite） | `%LOCALAPPDATA%\Packages\<パッケージ名>\LocalCache\Local\AppRoulette\roulette.db` | `%LOCALAPPDATA%\AppRoulette\roulette.db` |
| エラーのログ | `%LOCALAPPDATA%\Packages\<パッケージ名>\LocalState\logs\error.log` | `%LOCALAPPDATA%\AppRoulette\logs\error.log` |
| OneDrive 同期 | `%OneDrive%\Apps\AppRoulette\groups\<リストの ID>.json` | 同左 |

### ファイル形式
- **CSV**: 1 行目は見出し（`アイテム名,Weight`）、2 行目から 1 行に 1 アイテム。書き出しは BOM 付き UTF-8、読み込みは UTF-8・UTF-16・Shift_JIS に対応
- **バックアップ**: すべてのリストとアイテムを含む JSON ファイル
- **OneDrive 同期**: リストごとの JSON ファイル（仕組みは [OneDrive フォルダー同期の仕組み](docs/OneDriveSyncSetup.md) を参照）

### アイコン
- exe とウィンドウのアイコン: `AppRoulette/AppRoulette/Copilot_20260614_202528.ico`（16〜256px の各サイズを含む ICO）
- ストア版のパッケージ画像: `AppRoulette/AppRoulette (Package)/Images/`
- 元画像: `docs/Copilot_20260614_202528.png`（作り直し方は[ビルド方法](docs/BuildGuide.md)を参照）

## パフォーマンスの考慮事項

- **ルーレット アニメーション**: 約 60fps（16 ミリ秒ごと）で描画
- **自動保存**: 短い間に続いた変更は、まとめて 1 回で保存
- **配布物の大きさ**: Windows App SDK は使う部品だけを参照し、使わない機能の DLL を含めない

## セキュリティの考慮事項

- **通信**: ネットワーク通信は行いません。OneDrive 同期も PC の OneDrive フォルダーへの読み書きだけで、クラウドとの同期は OneDrive のクライアントが行います
- **データの保存先**: データはユーザーごとのフォルダー（`%LOCALAPPDATA%`）に保存します
- **データベース**: 入力された値は、パラメーター付きのクエリーで SQLite に渡します

## 貢献

貢献を歓迎します。[.github/copilot-instructions.md](.github/copilot-instructions.md) で指定されているコーディング規約に従ってください。

## ライセンス

[ライセンス情報をここに記載してください]

使用しているソフトウェアのライセンスは、`AppRoulette/AppRoulette/ThirdPartyNotices.txt`（アプリの「ライセンス情報」ボタンでも開けます）を参照してください。

## サポート

問題、質問、提案については [GitHub Issues](https://github.com/SystemAzmax/AppRoulette/issues) ページをご覧ください。

## ロードマップ

- [ ] 多言語対応 (i18n)
- [ ] リストごとのカスタムカラーテーマ
- [x] インポート/エクスポート機能（CSV、バックアップ）
- [ ] アニメーション カスタマイズオプション
- [ ] スピン中の音声効果

---

❤️ .NET 10 と WinUI 3 で構築
