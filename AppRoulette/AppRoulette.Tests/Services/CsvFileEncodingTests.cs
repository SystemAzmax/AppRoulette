using System.Text;
using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="CsvFileEncoding"/> のテスト。
/// CSV ファイルの文字コードの判別と、書き出しの文字コードを検証します。
/// </summary>
public class CsvFileEncodingTests
{
    /// <summary>テストに使う CSV の内容（日本語を含む）。</summary>
    private const string CSV_TEXT = "アイテム名,Weight\nりんご,3\nみかん,1";

    /// <summary>UTF-8 の BOM。</summary>
    private static readonly byte[] UTF8_BOM = { 0xEF, 0xBB, 0xBF };

    /// <summary>
    /// BOM 付きの UTF-8 を、BOM を除いて読めることを検証します。
    /// </summary>
    [Fact]
    public void Decode_BOM付きのUTF8_BOMを除いて読める()
    {
        // Arrange
        var bytes = UTF8_BOM.Concat(Encoding.UTF8.GetBytes(CSV_TEXT)).ToArray();

        // Act
        var text = CsvFileEncoding.Decode(bytes);

        // Assert
        Assert.Equal(CSV_TEXT, text);
    }

    /// <summary>
    /// BOM の無い UTF-8 を読めることを検証します。
    /// </summary>
    [Fact]
    public void Decode_BOMなしのUTF8_読める()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(CSV_TEXT);

        // Act
        var text = CsvFileEncoding.Decode(bytes);

        // Assert
        Assert.Equal(CSV_TEXT, text);
    }

    /// <summary>
    /// Excel が保存する Shift_JIS を読めることを検証します。
    /// </summary>
    [Fact]
    public void Decode_ShiftJIS_読める()
    {
        // Arrange
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = Encoding.GetEncoding(932).GetBytes(CSV_TEXT);

        // Act
        var text = CsvFileEncoding.Decode(bytes);

        // Assert
        Assert.Equal(CSV_TEXT, text);
    }

    /// <summary>
    /// BOM 付きの UTF-16 を、BOM を除いて読めることを検証します。
    /// </summary>
    [Fact]
    public void Decode_BOM付きのUTF16_BOMを除いて読める()
    {
        // Arrange
        var bytes = Encoding.Unicode.GetPreamble()
            .Concat(Encoding.Unicode.GetBytes(CSV_TEXT))
            .ToArray();

        // Act
        var text = CsvFileEncoding.Decode(bytes);

        // Assert
        Assert.Equal(CSV_TEXT, text);
    }

    /// <summary>
    /// 空のファイルは空文字になることを検証します。
    /// </summary>
    [Fact]
    public void Decode_空のファイル_空文字を返す()
    {
        // Act
        var text = CsvFileEncoding.Decode(Array.Empty<byte>());

        // Assert
        Assert.Equal(string.Empty, text);
    }

    /// <summary>
    /// 書き出しは BOM 付きの UTF-8 になり、読み込むと元の文字列に戻ることを検証します。
    /// </summary>
    [Fact]
    public void Encode_文字列_BOM付きのUTF8になり読み込むと元に戻る()
    {
        // Act
        var bytes = CsvFileEncoding.Encode(CSV_TEXT);

        // Assert
        Assert.Equal(UTF8_BOM, bytes.Take(UTF8_BOM.Length).ToArray());
        Assert.Equal(CSV_TEXT, CsvFileEncoding.Decode(bytes));
    }
}
