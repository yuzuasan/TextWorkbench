using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Domain.Models;

/// <summary>
/// Diffセッション（比較開始時点のスナップショット）を表します。
/// </summary>
public sealed class DiffSession
{
    /// <summary>
    /// 比較元の情報を表します。
    /// </summary>
    public sealed class DiffSource
    {
        /// <summary>
        /// ファイルパスを取得します。
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 表示名を取得します。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 文字コードを取得します。
        /// </summary>
        public TextEncoding Encoding { get; }

        /// <summary>
        /// 比較時の内容（LF正規化済み）を取得します。
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// <see cref="DiffSource"/> の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="displayName">表示名</param>
        /// <param name="encoding">文字コード</param>
        /// <param name="content">比較時の内容</param>
        public DiffSource(string filePath, string displayName, TextEncoding encoding, string content)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Encoding = encoding;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }
    }

    /// <summary>
    /// 左側の比較元を取得します。
    /// </summary>
    public DiffSource LeftSource { get; }

    /// <summary>
    /// 右側の比較元を取得します。
    /// </summary>
    public DiffSource RightSource { get; }

    /// <summary>
    /// Diffの結果を取得します。
    /// </summary>
    public DiffResult Result { get; }

    /// <summary>
    /// <see cref="DiffSession"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="leftSource">左側の比較元</param>
    /// <param name="rightSource">右側の比較元</param>
    /// <param name="result">Diffの結果</param>
    public DiffSession(DiffSource leftSource, DiffSource rightSource, DiffResult result)
    {
        LeftSource = leftSource ?? throw new ArgumentNullException(nameof(leftSource));
        RightSource = rightSource ?? throw new ArgumentNullException(nameof(rightSource));
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }
}
