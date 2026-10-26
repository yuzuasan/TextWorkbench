using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.Helpers;
using TextWorkbench.Presentation.Views;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// テキスト情報ダイアログのViewModelです。
/// </summary>
public sealed partial class TextInfoDialogViewModel : ObservableObject
{
    private TextInfo? _textInfo;

    /// <summary>
    /// 行数を取得します。
    /// </summary>
    [ObservableProperty]
    private string _lineCountText = string.Empty;

    /// <summary>
    /// 文字数を取得します。
    /// </summary>
    [ObservableProperty]
    private string _characterCountText = string.Empty;

    /// <summary>
    /// バイト数を取得します。
    /// </summary>
    [ObservableProperty]
    private string _byteCountText = string.Empty;

    /// <summary>
    /// 文字コードを取得します。
    /// </summary>
    [ObservableProperty]
    private string _encodingText = string.Empty;

    /// <summary>
    /// 改行コードを取得します。
    /// </summary>
    [ObservableProperty]
    private string _lineEndingText = string.Empty;

    /// <summary>
    /// OKコマンドを取得します。
    /// </summary>
    public IRelayCommand OkCommand { get; }

    /// <summary>
    /// <see cref="TextInfoDialogViewModel"/> の新しいインスタンスを初期化します。
    /// </summary>
    public TextInfoDialogViewModel()
    {
        OkCommand = new RelayCommand(ExecuteOk);
    }

    /// <summary>
    /// テキスト情報を設定します。
    /// </summary>
    /// <param name="textInfo">テキスト情報</param>
    public void SetTextInfo(TextInfo textInfo)
    {
        _textInfo = textInfo ?? throw new ArgumentNullException(nameof(textInfo));

        LineCountText = textInfo.LineCount.ToString("N0");
        CharacterCountText = textInfo.CharacterCount.ToString("N0");
        ByteCountText = textInfo.ByteCount.ToString("N0");
        EncodingText = TextEncodingHelper.ToDisplayString(textInfo.Encoding);
        LineEndingText = LineEndingHelper.ToDisplayString(textInfo.LineEnding);
    }

    /// <summary>
    /// OKボタンをクリックしたときに実行します。
    /// </summary>
    private void ExecuteOk()
    {
        // Windowを閉じる
        if (System.Windows.Application.Current.Windows.OfType<TextInfoDialog>().FirstOrDefault() is TextInfoDialog dialog)
        {
            dialog.DialogResult = true;
            dialog.Close();
        }
    }
}
