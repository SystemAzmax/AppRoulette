# AppRoulette

Windows デスクトップ用のルーレット選択アプリケーション。WinUI 3 と .NET 8 で構築されており、ルーレットグループの作成・編集やアイテムの追加、ルーレットの実行などの機能を提供します。

## 機能

- **複数グループ管理**: 最大 3 つのルーレットグループを独立して管理
- **アイテム管理**: グループあたり最大 30 個のアイテムを直感的に追加・編集
- **アニメーションルーレット**: スムーズな回転アニメーションと現実的な減速効果
- **永続化ストレージ**: グループとアイテムを JSON 形式で自動保存
- **MVVM アーキテクチャ**: ViewModel ベースの関心の分離
- **ダイアミックアイコン生成**: アプリケーション用のアイコンを動的に生成
- **ウィンドウ状態管理**: ウィンドウサイズと位置をセッション間で保存

## 技術スタック

- **フレームワーク**: .NET 8
- **UI フレームワーク**: WinUI 3 (Windows App SDK)
- **アーキテクチャ**: MVVM (Model-View-ViewModel)
- **データ永続化**: JSON ベースのファイルストレージ
- **Community Toolkit**: MVVM Toolkit によるプロパティバインディング

## プロジェクト構造

```
AppRoulette/
├── Models/
│   ├── RouletteItem.cs       # ルーレットの1アイテムを表現
│   └── RouletteGroup.cs      # ルーレットグループ (最大30アイテム)
├── ViewModels/
│   └── MainViewModel.cs      # メインアプリケーションロジック
├── Services/
│   ├── IDataPersistenceService.cs        # データ永続化インターフェース
│   ├── JsonDataPersistenceService.cs    # JSON ストレージ実装
│   ├── IRandomService.cs                # ランダム生成インターフェース
│   ├── RandomService.cs                 # ランダム生成実装
│   ├── IWindowPositionService.cs        # ウィンドウ状態管理インターフェース
│   ├── WindowPositionService.cs         # ウィンドウ状態管理実装
│   └── AppIconService.cs                # アプリケーション アイコン生成
├── Views/
│   └── RouletteRenderer.cs   # ルーレット描画ロジック
├── MainWindow.xaml          # メイン UI
├── MainWindow.xaml.cs       # メインウィンドウコードビハインド
└── App.xaml                 # アプリケーションリソース定義
```

## 主要コンポーネント

### Models
- **RouletteItem**: ルーレットグループ内のアイテムを表現（表示名を保持）
- **RouletteGroup**: 最大 30 個の RouletteItem を含有するグループ（最大 3 グループ、各グループに ID と表示名を持つ）

### ViewModels
- **MainViewModel**: グループ/アイテム管理、ルーレット選択ロジック、状態永続化を処理

### Services
- **JsonDataPersistenceService**: グループとアイテムを JSON 形式でロード/セーブ
- **RandomService**: 公正な選択のためのランダム数生成
- **WindowPositionService**: アプリケーションウィンドウサイズと位置を管理
- **AppIconService**: ICO 形式のアプリケーションアイコンを生成

### Views
- **RouletteRenderer**: グラフィックス API を使用したアニメーション ルーレット描画

## 使用開始

### 前提条件
- Windows 10/11 以降
- .NET 8 SDK
- Visual Studio 2022 以降（Community Edition 対応）

### ビルド

1. リポジトリをクローン:
   ```bash
   git clone https://github.com/SystemAzmax/AppRoulette.git
   ```

2. Visual Studio でソリューションファイルを開く:
   ```bash
   AppRoulette.slnx
   ```

3. ソリューションをビルド:
   ```bash
   dotnet build
   ```

4. アプリケーションを実行:
   ```bash
   dotnet run
   ```

## 使用方法

1. **グループの作成**: アプリケーションは 3 つの事前定義されたルーレットグループで起動
2. **アイテムの追加**: グループを選択して入力フィールドを使用してアイテムを追加
3. **アイテムの削除**: アイテムを選択して削除ボタンで削除
4. **ルーレットの実行**: スピンボタンをクリックしてアクティブなグループからアイテムをランダムに選択
5. **データ永続化**: すべての変更は自動的にローカルストレージに保存

## アーキテクチャの詳細

### MVVM パターン
アプリケーションは MVVM パターンに従い、以下の役割分担を実施:
- **Model**: `RouletteItem` および `RouletteGroup` クラスがデータを管理
- **View**: `MainWindow.xaml` で定義された XAML UI
- **ViewModel**: `MainViewModel` がすべてのビジネスロジックとステート管理を処理

### 双方向データバインディング
- View は ViewModels とのデータバインディングで通信
- ViewModel のデータ変更時に View が自動更新
- View のユーザー入力が ViewModel メソッドをトリガー

### サービス注入
- 依存性注入によるサービス管理
- インターフェースによる拡張性と テスト容易性
- 横断的関心事（永続化、ランダム化など）の処理

## コーディング規約

- **コメント**: すべてのパブリックメソッドには XML ドキュメントコメントを付けること
- **命名**: クラス/メソッド は PascalCase、変数は camelCase
- **定数**: 定数値は UPPER_SNAKE_CASE
- **プライベートフィールド**: アンダースコア接頭辞を使用 (_fieldName)
- **フォーマット**: 80 文字行制限、スペースベースインデント

## テスト

プロジェクトには `AppRoulette.Tests` プロジェクト内にユニットテストが含まれます。以下でテストを実行:

```bash
dotnet test
```

## ファイル形式

### データストレージ
- 形式: JSON
- 保存場所: ローカルアプリケーションディレクトリ
- 内容: シリアル化された `RouletteGroup` オブジェクトとそのアイテム

### アイコン
- 形式: ICO (Windows アイコン)
- `AppIconService` により動的に生成
- アプリケーション ブランディングに使用

## パフォーマンスの考慮事項

- **ルーレット アニメーション**: 60fps レンダリングで最適化
- **データ永続化**: キャッシング機能により効率化
- **メモリ管理**: 最大アイテム/グループ数制限による効率的なリスト使用

## セキュリティの考慮事項

- **ファイルアクセス**: 標準 Windows ファイル API と適切な権限管理
- **ユーザー入力**: ストレージ前のアイテムテキスト検証
- **最小依存関係**: 攻撃対象領域を縮小

## 貢献

貢献を歓迎します。プロジェクトの `copilot-instructions.md` で指定されているコーディング規約に従ってください。

## ライセンス

[ライセンス情報をここに記載してください]

## サポート

問題、質問、提案については [GitHub Issues](https://github.com/SystemAzmax/AppRoulette/issues) ページをご覧ください。

## ロードマップ

- [ ] 多言語対応 (i18n)
- [ ] ルーレットグループのカスタムカラーテーマ
- [ ] インポート/エクスポート機能
- [ ] アニメーション カスタマイズオプション
- [ ] スピン中の音声効果

---

❤️ .NET 8 と WinUI 3 で構築
