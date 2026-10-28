# ビルド方法（開発者向け）

このドキュメントは、AppRoulette をストア版と exe 版のそれぞれで
ビルドする方法をまとめたものです。

## 2 つの版の違い

| | ストア版 | exe 版 |
| --- | --- | --- |
| 形式 | MSIX パッケージ | パッケージなし（unpackaged） |
| ビルドに使うプロジェクト | `AppRoulette (Package)` | `AppRoulette` |
| 出力 | `.msixupload` | exe を含むフォルダー（約 270 MB） |
| 配布先の PC に必要なもの | なし（ストアからインストール） | なし（.NET と Windows App SDK を同梱） |
| データの保存先 | `%LOCALAPPDATA%\Packages\<パッケージ名>\LocalCache\Local\AppRoulette` | `%LOCALAPPDATA%\AppRoulette` |

どちらもコード上は `%LOCALAPPDATA%\AppRoulette` に保存します。
ストア版は MSIX が書き込み先をパッケージ専用のフォルダーに
リダイレクトするため、2 つの版でデータは共有されません。

## バージョンの上げ方

「このアプリについて」に表示するバージョンは、版によって読み取る場所が違います。
リリースのときは、次の 2 か所を同じバージョンにそろえてください。

| | 読み取る場所 | 書き方の例 |
| --- | --- | --- |
| ストア版 | `AppRoulette (Package)\Package.appxmanifest` の `Identity` の `Version` | `1.1.0.0` |
| exe 版 | `AppRoulette\AppRoulette.csproj` の `<Version>` | `1.1.0` |

- ストアに提出するパッケージは、4 つ目の数字（リビジョン）を 0 にする必要があります。
- 画面では、4 つ目の数字が 0 のときは省いて `1.1.0` のように表示します。

## ストア版のビルド

1. ソリューション エクスプローラーで **AppRoulette (Package)** を右クリックし、
   **公開** → **アプリ パッケージの作成** を選びます
   （英語版は Publish → Create App Packages）。
2. 配布方法で **Microsoft Store (新しいアプリ名を使用)** を選び、
   Partner Center にサインインして、予約済みのアプリ名を選びます。
   - 手元の PC で試すだけなら **サイドローディング** を選びます。
     この場合は証明書による署名が必要です。
3. 作るアーキテクチャ（x64 / x86 / ARM64）にチェックを入れ、
   構成を **Release** にして **作成** を押します。
4. `AppRoulette\AppRoulette (Package)\AppPackages\` にできた
   `.msixupload` を Partner Center にアップロードします。
   最後に出る「Windows アプリ認定キット」も実行しておくと、
   提出前に問題を見つけられます。

## exe 版のビルド

exe 版の設定は、発行プロファイル
`AppRoulette\AppRoulette\Properties\PublishProfiles\exe-win-x64.pubxml`
にまとめています。

### Visual Studio から

1. ソリューション エクスプローラーで **AppRoulette** プロジェクトを右クリックし、
   **発行** を選びます。
2. プロファイルの一覧から **exe-win-x64** を選び、**発行** を押します。
3. `D:\OneDrive\Tool\AppRoulette` に出力されます。

- 出力先の exe を起動中だと上書きに失敗するので、閉じてから発行します。
- 出力先を変えるときは、発行画面でプロファイルの **場所** を編集するか、
  pubxml の `PublishDir` を書き換えます。
- 発行画面ではストア版用の `win-x64` などのプロファイルも並びますが、
  exe 版には **exe-win-x64** を使います。

### コマンドラインから

Visual Studio の **表示** → **ターミナル** などで、
ソリューションのフォルダーから次を実行しても同じものができます。

```powershell
dotnet publish AppRoulette\AppRoulette\AppRoulette.csproj -c Release -p:Platform=x64 -p:PublishProfile=exe-win-x64
```

プロファイルを使わずに、すべてのプロパティを直接指定することもできます。

```powershell
dotnet publish AppRoulette\AppRoulette\AppRoulette.csproj -c Release -r win-x64 -p:Platform=x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:SelfContained=true -p:EnableMsixTooling=true -o D:\OneDrive\Tool\AppRoulette
```

ARM64 版を作るときは `win-x64` / `x64` を `win-arm64` / `ARM64` に、
x86 版を作るときは `win-x86` / `x86` に変えます。

### 各プロパティの役割

ストア版のビルドに影響しないよう、exe 版の設定は csproj に書かず、
発行プロファイル（またはコマンドライン）で指定しています。

| プロパティ | 役割 | 指定しなかったときの症状 |
| --- | --- | --- |
| `WindowsPackageType=None` | MSIX なしで起動できるようにする | 起動直後に「クラスが登録されていません」（REGDB_E_CLASSNOTREG）で落ちる |
| `WindowsAppSDKSelfContained=true` | Windows App SDK のランタイムを同梱する | 配布先の PC に Windows App Runtime のインストールが必要になる |
| `SelfContained=true` | .NET ランタイムを同梱する | 配布先の PC に .NET のインストールが必要になる |
| `EnableMsixTooling=true` | コンパイル済みの XAML（`AppRoulette.pri`）を出力先にコピーする | 起動後に `XamlParseException` で落ちる |

### 配布するとき

- exe 単体では動きません。出力フォルダーごと zip にして配布します。
- 署名していないため、ダウンロードした exe を実行すると
  Windows SmartScreen の警告が出ます。

## CI（GitHub Actions）

`.github/workflows/build.yml` で、次のときに Windows 上でビルドとテストを行います。

- `main` と `product*` ブランチへのプッシュ
- `main` へのプルリクエスト
- Actions 画面からの手動実行（Run workflow）

| ステップ | 内容 |
| --- | --- |
| テスト | `AppRoulette.Tests` をビルドして実行する |
| exe 版の発行 | `exe-win-x64` プロファイルで発行する（出力先は `-o` で上書き） |
| アーティファクトの保存 | 発行した exe 版を `AppRoulette-win-x64` として 14 日間保存する |

CI で作った exe 版は、Actions の実行結果のページにある
**Artifacts** からダウンロードできます。

ストア版のパッケージ（wapproj）は Visual Studio の MSBuild が必要なため、
CI ではビルドしていません。

## デバッグ版のデータを exe 版へコピーする

Visual Studio でデバッグ実行したときのデータは、パッケージ専用のフォルダーにあります。
アプリを閉じてから、PowerShell で次を実行します。

```powershell
$src = "$env:LOCALAPPDATA\Packages\91d0b9ad-65b1-4502-ac9c-d13dc83c4750_zegbcpgrcgmda\LocalCache\Local\AppRoulette"
$dst = "$env:LOCALAPPDATA\AppRoulette"
New-Item -ItemType Directory -Force $dst | Out-Null
Copy-Item "$src\*" $dst -Force
```

- パッケージ フォルダー名は、`Package.appxmanifest` の `Identity` の `Name` で始まります。
  ストアからインストールした版はパッケージ名が変わるので、
  `%LOCALAPPDATA%\Packages` の中で探してください。
- コピー先に既にデータがある場合は上書きされます。必要なら先にバックアップします。
- `Packages\...\LocalState\roulette.db` は以前の保存場所に残った古いファイルなので、
  コピーは不要です。
- コピーは 1 回限りです。その後はそれぞれの版が別々にデータを更新します。

## 注意点

- **trim を有効にしない**:
  `AppRoulette.csproj` では `PublishTrimmed` を `False` にしています。
  trim を有効にすると、起動時のデータ読み込みが動かずデータが空で表示されるほか、
  リフレクションを使う JSON シリアル化が無効になり、ウィンドウ位置の保存や
  OneDrive 同期が動きません。ストア版にも同じ影響があります。
- **既存の発行プロファイルを変更しない**:
  `Properties\PublishProfiles\win-*.pubxml` はストア版のパッケージ作成で使われます。
  exe 版の設定をここに入れないでください。
- **`bin` の exe を直接起動しない**:
  通常のビルド出力（`bin\...\AppRoulette.exe`）はパッケージ用のため、
  直接起動すると「クラスが登録されていません」で落ちます。
  exe 版は上のコマンドで発行したものを使います。
- **版を切り替えたらリビルドする**:
  exe 版とストア版は同じ `bin` / `obj` フォルダーを使います。
  exe 版を発行した後にストア版を作るときは、念のためリビルドしてください。
