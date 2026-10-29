using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="ErrorLogFile"/> のユニットテストクラスです。
/// </summary>
public class ErrorLogFileTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _logPath;

    /// <summary>
    /// テスト用の一時ディレクトリを初期化します（ディレクトリ自体は作りません）。
    /// </summary>
    public ErrorLogFileTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(),
            "AppRouletteTests",
            Guid.NewGuid().ToString());
        _logPath = Path.Combine(_testDir, "logs", "error.log");
    }

    /// <summary>
    /// テスト後に一時ディレクトリを削除します。
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void Write_フォルダーが無い場合_作成して処理名と例外を記録する()
    {
        var sut = new ErrorLogFile(_logPath);

        sut.Write("アイテム保存", new IOException("ディスクがいっぱいです"));

        var content = File.ReadAllText(_logPath);
        Assert.Contains("[アイテム保存]", content);
        Assert.Contains("System.IO.IOException: ディスクがいっぱいです", content);
    }

    [Fact]
    public void Write_続けて記録した場合_前の内容に追記する()
    {
        var sut = new ErrorLogFile(_logPath);

        sut.Write("1 件目", new IOException("最初のエラー"));
        sut.Write("2 件目", new IOException("次のエラー"));

        var content = File.ReadAllText(_logPath);
        Assert.Contains("最初のエラー", content);
        Assert.Contains("次のエラー", content);
    }

    [Fact]
    public void Write_上限の大きさを超えた場合_1世代前のファイルに移して新しく書き始める()
    {
        var sut = new ErrorLogFile(_logPath, maxFileSize: 1);

        sut.Write("1 件目", new IOException("最初のエラー"));
        sut.Write("2 件目", new IOException("次のエラー"));

        Assert.Equal(Path.Combine(_testDir, "logs", "error.old.log"), sut.OldFilePath);
        var oldContent = File.ReadAllText(sut.OldFilePath);
        var content = File.ReadAllText(_logPath);
        Assert.Contains("最初のエラー", oldContent);
        Assert.DoesNotContain("次のエラー", oldContent);
        Assert.Contains("次のエラー", content);
        Assert.DoesNotContain("最初のエラー", content);
    }

    [Fact]
    public void Write_書き込めない場合_例外を投げない()
    {
        // フォルダーを作るべき場所に同じ名前のファイルを置き、書き込めない状態にする
        Directory.CreateDirectory(_testDir);
        File.WriteAllText(Path.Combine(_testDir, "logs"), string.Empty);
        var sut = new ErrorLogFile(_logPath);

        var exception = Record.Exception(
            () => sut.Write("アイテム保存", new IOException("ディスクがいっぱいです")));

        Assert.Null(exception);
        Assert.False(File.Exists(_logPath));
    }
}
