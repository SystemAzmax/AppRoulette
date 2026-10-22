# 01-prerequisites: SDKとツールチェーンの確認

.NET 10 SDKがマシンにインストールされていることを確認し、global.jsonが存在する場合はSDKバージョンとの互換性を検証する。プロジェクトはWindows Desktop (WinForms/WinUI) をターゲットとしているため、Windows App SDK関連の依存関係も対象TFM (`net10.0-windows10.0.22000.0` / パッケージングプロジェクトは `net10.0-windows10.0.26100.0`) に対応していることを確認する。

**Done when**: .NET 10 SDKのインストールが確認され、global.jsonの互換性検証が完了している。

