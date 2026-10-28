using System.Text;

namespace AppRoulette.Services;

/// <summary>
/// CSV ファイルの文字コードの変換を担うクラスです。
/// 読み込みでは UTF-8・UTF-16・Shift_JIS を判別し、
/// 書き出しでは Excel で文字化けしないよう BOM 付きの UTF-8 を使います。
/// </summary>
public static class CsvFileEncoding
{
    /// <summary>Shift_JIS（Windows の日本語の文字コード）のコードページ。</summary>
    private const int SHIFT_JIS_CODE_PAGE = 932;

    private static readonly byte[] UTF8_BOM = { 0xEF, 0xBB, 0xBF };

    private static readonly byte[] UTF16_LE_BOM = { 0xFF, 0xFE };

    private static readonly byte[] UTF16_BE_BOM = { 0xFE, 0xFF };

    /// <summary>不正なバイト列で例外を投げる、BOM なしの UTF-8。</summary>
    private static readonly UTF8Encoding STRICT_UTF8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    static CsvFileEncoding()
    {
        // .NET では Shift_JIS を使うためにコードページの登録が必要
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// CSV ファイルの内容を文字列に変換します。
    /// BOM がある場合はその文字コードで読み、BOM が無い場合は UTF-8 として読みます。
    /// UTF-8 として正しくない場合は、Excel が保存する Shift_JIS として読みます。
    /// </summary>
    /// <param name="bytes">CSV ファイルの内容。</param>
    /// <returns>BOM を除いた文字列。</returns>
    public static string Decode(byte[] bytes)
    {
        if (StartsWith(bytes, UTF8_BOM))
        {
            return Encoding.UTF8.GetString(bytes, UTF8_BOM.Length, bytes.Length - UTF8_BOM.Length);
        }

        if (StartsWith(bytes, UTF16_LE_BOM))
        {
            return Encoding.Unicode.GetString(bytes, UTF16_LE_BOM.Length, bytes.Length - UTF16_LE_BOM.Length);
        }

        if (StartsWith(bytes, UTF16_BE_BOM))
        {
            return Encoding.BigEndianUnicode.GetString(bytes, UTF16_BE_BOM.Length, bytes.Length - UTF16_BE_BOM.Length);
        }

        try
        {
            return STRICT_UTF8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(SHIFT_JIS_CODE_PAGE).GetString(bytes);
        }
    }

    /// <summary>
    /// 文字列を、CSV ファイルに書き出す BOM 付きの UTF-8 に変換します。
    /// </summary>
    /// <param name="text">書き出す文字列。</param>
    /// <returns>BOM 付きの UTF-8 のバイト列。</returns>
    public static byte[] Encode(string text) =>
        UTF8_BOM.Concat(Encoding.UTF8.GetBytes(text)).ToArray();

    /// <summary>
    /// バイト列が指定した BOM で始まるかどうかを返します。
    /// </summary>
    /// <param name="bytes">調べるバイト列。</param>
    /// <param name="bom">BOM のバイト列。</param>
    /// <returns>BOM で始まる場合は <c>true</c>。</returns>
    private static bool StartsWith(byte[] bytes, byte[] bom) =>
        bytes.AsSpan().StartsWith(bom);
}
