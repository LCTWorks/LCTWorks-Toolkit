using ColorCode;
using LCTWorks.Core.Extensions;
using LCTWorks.WinUI.Controls.Internal;
using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace LCTWorks.Workshop.Controls;

[TemplatePart(Name = DescriptionTextPresenterPartName, Type = typeof(TextBlock))]
[TemplatePart(Name = CodeSelectorPartName, Type = typeof(SelectorBar))]
[TemplatePart(Name = ExpanderPartName, Type = typeof(Expander))]
[TemplatePart(Name = LoadingBorderPartName, Type = typeof(Border))]
[TemplatePart(Name = CodeContainerPartName, Type = typeof(Grid))]
[TemplatePart(Name = CodeRichTextBlockPartName, Type = typeof(RichTextBlock))]
public partial class SampleCodePresenter : Control
{
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(SampleCodePresenter),
            new PropertyMetadata(default));

    public static readonly DependencyProperty CSharpCodeFilePathProperty =
        DependencyProperty.Register(nameof(CSharpCodeFilePath), typeof(string), typeof(SampleCodePresenter),
            new PropertyMetadata(default, OnCodePropertyChanged));

    public static readonly DependencyProperty DescriptionProperty =
                DependencyProperty.Register(nameof(Description), typeof(string), typeof(SampleCodePresenter),
            new PropertyMetadata(default, OnDescriptionChanged));

    public static readonly DependencyProperty ExpanderHeaderProperty =
        DependencyProperty.Register(nameof(ExpanderHeader), typeof(string), typeof(SampleCodePresenter),
            new PropertyMetadata(default));

    public static readonly DependencyProperty HeaderProperty =
                    DependencyProperty.Register(nameof(Header), typeof(string), typeof(SampleCodePresenter),
            new PropertyMetadata(default));

    public static readonly DependencyProperty MinOptionsPaneWidthProperty =
        DependencyProperty.Register(nameof(MinOptionsPaneWidth), typeof(double), typeof(SampleCodePresenter),
            new PropertyMetadata(0));

    public static readonly DependencyProperty OptionsPaneContentProperty =
            DependencyProperty.Register(nameof(OptionsPaneContent), typeof(object), typeof(SampleCodePresenter),
            new PropertyMetadata(default));

    public static readonly DependencyProperty PlainTextFilePathProperty =
        DependencyProperty.Register(nameof(PlainTextFilePath), typeof(string), typeof(SampleCodePresenter),
            new PropertyMetadata(default, OnCodePropertyChanged));

    public static readonly DependencyProperty PlainTextHeaderTextProperty =
        DependencyProperty.Register(nameof(PlainTextHeaderText), typeof(string), typeof(SampleCodePresenter),
            new PropertyMetadata(default));

    public static readonly DependencyProperty XamlCodeFilePathProperty =
                DependencyProperty.Register(nameof(XamlCodeFilePath), typeof(string), typeof(SampleCodePresenter),
            new PropertyMetadata(default, OnCodePropertyChanged));

    private const string CodeContainerPartName = "CodeContainer";

    private const string CodeRichTextBlockPartName = "CodeRichTextBlock";

    private const string CodeSelectorPartName = "CodeSelector";

    private const string DescriptionTextPresenterPartName = "DescriptionTextPresenter";

    private const string ExpanderPartName = "Expander";

    private const string LoadingBorderPartName = "LoadingBorder";

    private static readonly string CodeFontFamily = "Consolas, Cascadia Code";

    private static readonly string TextFontFamily = "Segoe UI Variable Display, Segoe UI";

    private readonly Dictionary<string, string> codeSnippets = [];

    private SelectorBar? _codeSelector;

    private Expander? _expander;
    private RichTextBlock? _richTextBlock;

    public SampleCodePresenter()
    {
        DefaultStyleKey = typeof(SampleCodePresenter);
        Loaded += ControlLoaded;
    }

    public object Content
    {
        get => (object)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public string CSharpCodeFilePath
    {
        get => (string)GetValue(CSharpCodeFilePathProperty);
        set => SetValue(CSharpCodeFilePathProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string ExpanderHeader
    {
        get => (string)GetValue(ExpanderHeaderProperty);
        set => SetValue(ExpanderHeaderProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public double MinOptionsPaneWidth
    {
        get => (double)GetValue(MinOptionsPaneWidthProperty);
        set => SetValue(MinOptionsPaneWidthProperty, value);
    }

    public object OptionsPaneContent
    {
        get => (object)GetValue(OptionsPaneContentProperty);
        set => SetValue(OptionsPaneContentProperty, value);
    }

    public string PlainTextFilePath
    {
        get => (string)GetValue(PlainTextFilePathProperty);
        set => SetValue(PlainTextFilePathProperty, value);
    }

    public string PlainTextHeaderText
    {
        get => (string)GetValue(PlainTextHeaderTextProperty);
        set => SetValue(PlainTextHeaderTextProperty, value);
    }

    public string XamlCodeFilePath
    {
        get => (string)GetValue(XamlCodeFilePathProperty);
        set => SetValue(XamlCodeFilePathProperty, value);
    }

    public static string GetEnumDescription(Enum value)
    {
        var type = value.GetType();
        var memberInfo = type.GetMember(value.ToString()).FirstOrDefault();
        if (memberInfo != null)
        {
            if (memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() is DescriptionAttribute descriptionAttribute)
            {
                return descriptionAttribute.Description;
            }
        }
        return value.ToString();
    }

    private static void OnCodePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SampleCodePresenter control)
        {
            control.ReloadCodeSnippetsVisualsAsync();
        }
    }

    private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SampleCodePresenter control)
        {
            control.SetDescriptionVisibility();
        }
    }

    private void CodeSelectorSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var selectedItem = sender.SelectedItem;
        if (selectedItem == null)
        {
            return;
        }
        if (_richTextBlock == null)
        {
            return;
        }
        var codeType = (SampleCodeType)selectedItem.Tag;
        var formatter = new RichTextBlockFormatter(ActualTheme);
        string codeSnippet = string.Empty;
        string fontFamily = TextFontFamily;
        ILanguage richLanguage = Languages.Markdown;

        switch (codeType)
        {
            case SampleCodeType.Xaml:
                codeSnippet = codeSnippets.TryGetValue(XamlCodeFilePath, out var xamlCode) ? xamlCode : string.Empty;
                richLanguage = Languages.Xml;
                fontFamily = CodeFontFamily;
                break;

            case SampleCodeType.CSharp:
                codeSnippet = codeSnippets.TryGetValue(CSharpCodeFilePath, out var csCode) ? csCode : string.Empty;
                richLanguage = Languages.CSharp;
                fontFamily = CodeFontFamily;
                break;

            case SampleCodeType.PlainText:
                codeSnippet = codeSnippets.TryGetValue(PlainTextFilePath, out var plainText) ? plainText : string.Empty;
                richLanguage = Languages.Markdown;
                break;

            default:
                break;
        }
        _richTextBlock.Blocks.Clear();
        _richTextBlock.FontFamily = new FontFamily(fontFamily);
        formatter.FormatRichTextBlock(codeSnippet, richLanguage, _richTextBlock);
    }

    private async Task LoadCodeSnippetsAsync()
    {
        var pathsToLoad = new[] { XamlCodeFilePath, CSharpCodeFilePath, PlainTextFilePath }.Where(v => v != null).ToList();
        var pruneItems = codeSnippets.Keys.Where(k => !pathsToLoad.Contains(k)).ToList();
        foreach (var pruneItem in pruneItems)
        {
            codeSnippets.Remove(pruneItem);
        }
        if (pathsToLoad.Count > 0)
        {
            SetLoadingState(true);
            foreach (var path in pathsToLoad)
            {
                if (!codeSnippets.ContainsKey(path))
                {
                    var content = await AppStorageHelper.ReadTextOnStorageFileAsync(path);
                    if (content != null)
                    {
                        codeSnippets[path] = content;
                    }
                }
            }
            SetLoadingState(false);
        }
    }

    #region Initial events

    private void ControlLoaded(object sender, RoutedEventArgs e)
    {
        _expander = GetTemplateChild(ExpanderPartName) as Expander;
        _richTextBlock = GetTemplateChild(CodeRichTextBlockPartName) as RichTextBlock;
        _expander?.Expanding += Expander_Expanding;
        SetDescriptionVisibility();
        SetExpanderVisibility();
        Loaded -= ControlLoaded;
    }

    private void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        var selector = GetTemplateChild(CodeSelectorPartName) as SelectorBar;
        if (selector != null)
        {
            _codeSelector = selector;
            _codeSelector.SelectionChanged += CodeSelectorSelectionChanged;

            _expander?.Expanding -= Expander_Expanding;
            ReloadCodeSnippetsVisualsAsync();
        }
    }

    #endregion Initial events

    private async void ReloadCodeSnippetsVisualsAsync()
    {
        if (_codeSelector == null)
        {
            return;
        }
        var selectorItems = new List<SelectorBarItem>();

        await LoadCodeSnippetsAsync();

        if (!string.IsNullOrEmpty(XamlCodeFilePath))
        {
            selectorItems.Add(new SelectorBarItem { Text = GetEnumDescription(SampleCodeType.Xaml), Tag = SampleCodeType.Xaml });
        }
        if (!string.IsNullOrEmpty(CSharpCodeFilePath))
        {
            selectorItems.Add(new SelectorBarItem { Text = GetEnumDescription(SampleCodeType.CSharp), Tag = SampleCodeType.CSharp });
        }
        if (!string.IsNullOrEmpty(PlainTextFilePath))
        {
            var header = string.IsNullOrWhiteSpace(PlainTextHeaderText) ? GetEnumDescription(SampleCodeType.PlainText) : PlainTextHeaderText.Trim();
            selectorItems.Add(new SelectorBarItem { Text = header, Tag = SampleCodeType.PlainText });
        }
        _codeSelector.Items.Clear();
        bool hasItems = selectorItems.Count > 0;
        _expander?.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
        if (hasItems)
        {
            _codeSelector.Items.AddRange(selectorItems);
            _codeSelector.SelectedItem = selectorItems[0];
        }
    }

    private void SetDescriptionVisibility()
    {
        var descriptionTextPresenter = GetTemplateChild(DescriptionTextPresenterPartName) as TextBlock;
        descriptionTextPresenter?.Visibility = string.IsNullOrEmpty(Description)
                ? Visibility.Collapsed
                : Visibility.Visible;
    }

    private void SetExpanderVisibility()
    {
        var pathsToLoad = new[] { XamlCodeFilePath, CSharpCodeFilePath, PlainTextFilePath }.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        _expander?.Visibility = pathsToLoad.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetLoadingState(bool isLoading)
    {
        if (GetTemplateChild(LoadingBorderPartName) is Border loadingBorder)
        {
            loadingBorder.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }
        if (GetTemplateChild(CodeContainerPartName) is Grid codeContainer)
        {
            codeContainer.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}