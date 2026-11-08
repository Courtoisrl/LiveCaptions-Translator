using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls;

using LiveCaptionsTranslator.apis;
using LiveCaptionsTranslator.Utils;
using Button = Wpf.Ui.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorEnum = LiveCaptionsTranslator.Utils.Color;
using TextBlock = System.Windows.Controls.TextBlock;

namespace LiveCaptionsTranslator
{
    public partial class OverlayWindow : Window
    {
        private readonly Dictionary<ColorEnum, SolidColorBrush> colorMap = new()
        {
            {ColorEnum.White, Brushes.White},
            {ColorEnum.Yellow, Brushes.Yellow},
            {ColorEnum.LimeGreen, Brushes.LimeGreen},
            {ColorEnum.Aqua, Brushes.Aqua},
            {ColorEnum.Blue, Brushes.Blue},
            {ColorEnum.DeepPink, Brushes.DeepPink},
            {ColorEnum.Red, Brushes.Red},
            {ColorEnum.Black, Brushes.Black},
        };
        private const int MaxCaptionLines = 3;
        private CaptionVisible onlyMode = CaptionVisible.Both;
        private readonly double bothCaptionsMinHeight;
        private static TextBlock? measureBlock;
        private bool isRefreshingCaptionText;
        private string lastCaptionSignature = string.Empty;

        public CaptionVisible OnlyMode
        {
            get => onlyMode;
            set
            {
                if (onlyMode == value)
                    return;
                onlyMode = value;
                ResizeForOnlyMode();
            }
        }
        public CaptionLocation SwitchMode { get; set; } = CaptionLocation.TranslationTop;

        public OverlayWindow()
        {
            InitializeComponent();
            bothCaptionsMinHeight = MinHeight;
            DataContext = Translator.Caption;

            Loaded += (s, e) => Translator.Caption.PropertyChanged += TranslatedChanged;
            Unloaded += (s, e) => Translator.Caption.PropertyChanged -= TranslatedChanged;

            OriginalCaption.FontWeight = Translator.Setting.OverlayWindow.FontBold == Utils.FontBold.Both ?
                FontWeights.Bold : FontWeights.Regular;
            TranslatedCaption.FontWeight = Translator.Setting.OverlayWindow.FontBold >= Utils.FontBold.TranslationOnly ?
                FontWeights.Bold : FontWeights.Regular;

            OriginalCaptionDecorator.StrokeThickness = Translator.Setting.OverlayWindow.FontStroke;
            TranslatedCaptionDecorator.StrokeThickness = Translator.Setting.OverlayWindow.FontStroke;

            OriginalCaption.Foreground = colorMap[Translator.Setting.OverlayWindow.FontColor];
            UpdateTranslationColor(colorMap[Translator.Setting.OverlayWindow.FontColor]);

            BorderBackground.Background = colorMap[Translator.Setting.OverlayWindow.BackgroundColor];
            BorderBackground.Opacity = Translator.Setting.OverlayWindow.Opacity;

            ApplyFontSize();
            ApplyBackgroundOpacity();

            Loaded += (s, e) => RefreshCaptionText();
            SizeChanged += (s, e) => RefreshCaptionText();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void TopThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = this.Height - e.VerticalChange;

            if (newHeight >= this.MinHeight)
            {
                this.Top += e.VerticalChange;
                this.Height = newHeight;
            }
        }

        private void BottomThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = this.Height + e.VerticalChange;

            if (newHeight >= this.MinHeight)
            {
                this.Height = newHeight;
            }
        }

        private void LeftThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = this.Width - e.HorizontalChange;

            if (newWidth >= this.MinWidth)
            {
                this.Left += e.HorizontalChange;
                this.Width = newWidth;
            }
        }

        private void RightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = this.Width + e.HorizontalChange;

            if (newWidth >= this.MinWidth)
            {
                this.Width = newWidth;
            }
        }

        private void TopLeftThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            TopThumb_OnDragDelta(sender, e);
            LeftThumb_OnDragDelta(sender, e);
        }

        private void TopRightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            TopThumb_OnDragDelta(sender, e);
            RightThumb_OnDragDelta(sender, e);
        }

        private void BottomLeftThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            BottomThumb_OnDragDelta(sender, e);
            LeftThumb_OnDragDelta(sender, e);
        }

        private void BottomRightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            BottomThumb_OnDragDelta(sender, e);
            RightThumb_OnDragDelta(sender, e);
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            // The caption raises PropertyChanged for many members (latency, history, ...) on every
            // translator loop tick; only these properties affect the rendered caption lines.
            switch (e.PropertyName)
            {
                case "OverlayOriginalCaption":
                case "OverlayNoticePrefix":
                case "OverlayCurrentTranslation":
                case "OverlayPreviousTranslation":
                case "":
                case null:
                    RefreshCaptionText();
                    break;
            }
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            ControlPanel.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            ControlPanel.Visibility = Visibility.Hidden;
        }

        private void FontIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.FontSize + StyleConsts.DELTA_FONT_SIZE < StyleConsts.MAX_FONT_SIZE)
            {
                Translator.Setting.OverlayWindow.FontSize += StyleConsts.DELTA_FONT_SIZE;
                ApplyFontSize();
            }
        }

        private void FontDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.FontSize - StyleConsts.DELTA_FONT_SIZE > StyleConsts.MIN_FONT_SIZE)
            {
                Translator.Setting.OverlayWindow.FontSize -= StyleConsts.DELTA_FONT_SIZE;
                ApplyFontSize();
            }
        }

        private void FontBold_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.OverlayWindow.FontBold++;
            if (Translator.Setting.OverlayWindow.FontBold > Utils.FontBold.Both)
                Translator.Setting.OverlayWindow.FontBold = Utils.FontBold.None;
            switch (Translator.Setting.OverlayWindow.FontBold)
            {
                case Utils.FontBold.None:
                    OriginalCaption.FontWeight = FontWeights.Regular;
                    TranslatedCaption.FontWeight = FontWeights.Regular;
                    break;
                case Utils.FontBold.TranslationOnly:
                    OriginalCaption.FontWeight = FontWeights.Regular;
                    TranslatedCaption.FontWeight = FontWeights.Bold;
                    break;
                case Utils.FontBold.SubtitleOnly:
                    OriginalCaption.FontWeight = FontWeights.Bold;
                    TranslatedCaption.FontWeight = FontWeights.Regular;
                    break;
                case Utils.FontBold.Both:
                    OriginalCaption.FontWeight = FontWeights.Bold;
                    TranslatedCaption.FontWeight = FontWeights.Bold;
                    break;
            }

            RefreshCaptionText();
        }

        private void FontStrokeIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.FontStroke + StyleConsts.DELTA_STROKE > StyleConsts.MAX_STROKE)
                return;
            Translator.Setting.OverlayWindow.FontStroke += StyleConsts.DELTA_STROKE;
            ApplyFontStroke();
        }

        private void FontStrokeDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.FontStroke - StyleConsts.DELTA_STROKE < StyleConsts.MIN_STROKE)
                return;
            Translator.Setting.OverlayWindow.FontStroke -= StyleConsts.DELTA_STROKE;
            ApplyFontStroke();
        }

        private void FontColorCycle_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.OverlayWindow.FontColor++;
            if (Translator.Setting.OverlayWindow.FontColor > ColorEnum.Black)
                Translator.Setting.OverlayWindow.FontColor = ColorEnum.White;
            OriginalCaption.Foreground = colorMap[Translator.Setting.OverlayWindow.FontColor];
            TranslatedCaption.Foreground = colorMap[Translator.Setting.OverlayWindow.FontColor];
            UpdateTranslationColor(colorMap[Translator.Setting.OverlayWindow.FontColor]);
        }

        private void BackgroundOpacityIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.Opacity + StyleConsts.DELTA_OPACITY < StyleConsts.MAX_OPACITY)
                Translator.Setting.OverlayWindow.Opacity += StyleConsts.DELTA_OPACITY;
            else
                Translator.Setting.OverlayWindow.Opacity = StyleConsts.MAX_OPACITY;
            ApplyBackgroundOpacity();
        }

        private void BackgroundOpacityDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.Opacity - StyleConsts.DELTA_OPACITY > StyleConsts.MIN_OPACITY)
                Translator.Setting.OverlayWindow.Opacity -= StyleConsts.DELTA_OPACITY;
            else
                Translator.Setting.OverlayWindow.Opacity = StyleConsts.MIN_OPACITY;
            ApplyBackgroundOpacity();
        }

        private void BackgroundColorCycle_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.OverlayWindow.BackgroundColor++;
            if (Translator.Setting.OverlayWindow.BackgroundColor > ColorEnum.Black)
                Translator.Setting.OverlayWindow.BackgroundColor = ColorEnum.White;
            BorderBackground.Background = colorMap[Translator.Setting.OverlayWindow.BackgroundColor];

            BorderBackground.Opacity = Translator.Setting.OverlayWindow.Opacity;
            ApplyBackgroundOpacity();
        }

        private void OnlyModeButton_Click(object sender, RoutedEventArgs e)
        {
            OnlyMode = onlyMode switch
            {
                CaptionVisible.SubtitleOnly => CaptionVisible.Both,
                CaptionVisible.Both => CaptionVisible.TranslationOnly,
                _ => CaptionVisible.SubtitleOnly
            };
        }

        private void SwitchModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SwitchMode == CaptionLocation.TranslationTop)
            {
                Grid.SetRow(TranslatedCaptionCard, 1);
                Grid.SetRow(OriginalCaptionCard, 0);
                SwitchMode = CaptionLocation.SubtitleTop;
            }
            else
            {
                Grid.SetRow(TranslatedCaptionCard, 0);
                Grid.SetRow(OriginalCaptionCard, 1);
                SwitchMode = CaptionLocation.TranslationTop;
            }
        }

        private void ClickThrough_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var extendedStyle = WindowsAPI.GetWindowLong(hwnd, WindowsAPI.GWL_EXSTYLE);
            WindowsAPI.SetWindowLong(hwnd, WindowsAPI.GWL_EXSTYLE, extendedStyle | WindowsAPI.WS_EX_TRANSPARENT);
            ControlPanel.Visibility = Visibility.Collapsed;
        }

        public void ResizeForOnlyMode()
        {
            bool showTranslated = onlyMode != CaptionVisible.SubtitleOnly;
            bool showOriginal = onlyMode != CaptionVisible.TranslationOnly;

            TranslatedCaptionCard.Visibility = showTranslated ? Visibility.Visible : Visibility.Collapsed;
            OriginalCaptionCard.Visibility = showOriginal ? Visibility.Visible : Visibility.Collapsed;

            // The window is sized by its content (SizeToContent="Height"), so only the minimum height
            // has to follow the number of visible captions.
            int visibleCaptions = (showTranslated ? 1 : 0) + (showOriginal ? 1 : 0);
            MinHeight = bothCaptionsMinHeight - (2 - visibleCaptions) * StyleConsts.DELTA_OVERLAY_HEIGHT;

            if (OnlyModeButton.Icon is SymbolIcon onlyModeIcon)
            {
                onlyModeIcon.Symbol = onlyMode switch
                {
                    CaptionVisible.SubtitleOnly => SymbolRegular.PanelTopContract20,
                    CaptionVisible.TranslationOnly => SymbolRegular.PanelTopExpand20,
                    _ => SymbolRegular.PanelBottom20
                };
            }
        }

        /// <summary>
        /// Caps each caption card to <see cref="MaxCaptionLines"/> lines. When the text grows past
        /// the limit, the oldest lines are dropped so the newest lines stay visible (the captions
        /// scroll upwards).
        /// </summary>
        private void RefreshCaptionText()
        {
            if (!Dispatcher.CheckAccess())
            {
                // Caption properties are updated from background threads; WPF elements must be
                // touched on the UI thread.
                Dispatcher.BeginInvoke(new Action(RefreshCaptionText), DispatcherPriority.Background);
                return;
            }

            if (isRefreshingCaptionText || !IsLoaded)
                return;

            isRefreshingCaptionText = true;
            try
            {
                var caption = Translator.Caption;

                double originalWidth = GetCaptionWidth(OriginalCaption);
                double translatedWidth = GetCaptionWidth(TranslatedCaption);

                string originalText = caption.OverlayOriginalCaption ?? string.Empty;
                string prefix = caption.OverlayNoticePrefix ?? string.Empty;
                string previous = caption.OverlayPreviousTranslation ?? string.Empty;
                string current = caption.OverlayCurrentTranslation ?? string.Empty;

                // Caption notifications arrive on every translator loop tick, while the rendered
                // text only changes a few times per second: skip the expensive measuring pass
                // when nothing relevant changed.
                string signature = string.Join('\u0001', originalText, prefix, previous, current)
                    + '\u0001' + originalWidth.ToString("F1") + '\u0001' + translatedWidth.ToString("F1");
                if (signature == lastCaptionSignature)
                    return;
                lastCaptionSignature = signature;

                string original = TrimToLines(originalText, OriginalCaption, originalWidth, out _);
                if (OriginalCaption.Text != original)
                    OriginalCaption.Text = original;

                string translated = prefix + previous + current;

                TrimToLines(translated, TranslatedCaption, translatedWidth, out int cut);

                int prefixEnd = prefix.Length;
                int previousEnd = prefixEnd + previous.Length;
                SetRunText(NoticePrefixRun, Slice(translated, cut, 0, prefixEnd));
                SetRunText(PreviousTranslationRun, Slice(translated, cut, prefixEnd, previousEnd));
                SetRunText(CurrentTranslationRun, Slice(translated, cut, previousEnd, translated.Length));

                OriginalCaptionDecorator.InvalidateVisual();
                TranslatedCaptionDecorator.InvalidateVisual();
            }
            finally
            {
                isRefreshingCaptionText = false;
            }
        }

        private double GetCaptionWidth(TextBlock prototype)
        {
            if (prototype.ActualWidth > 0)
                return prototype.ActualWidth;

            // Before the first layout pass, or while a caption card is collapsed, the text has no
            // width of its own: fall back to the card width (background border minus card margins
            // and padding), then to the window width.
            if (BorderBackground.ActualWidth > 0)
                return BorderBackground.ActualWidth - 26;

            return Math.Max(100, Width - 48);
        }

        private static string TrimToLines(string text, TextBlock prototype, double width, out int cut)
        {
            cut = 0;
            if (string.IsNullOrEmpty(text) || width <= 0)
                return text;

            double lineHeight = MeasureTextHeight("M", prototype, width);
            if (lineHeight <= 0)
                return text;

            double maxHeight = lineHeight * MaxCaptionLines + 0.5;
            if (MeasureTextHeight(text, prototype, width) <= maxHeight)
                return text;

            // Find the smallest number of leading characters to drop so the rest fits the limit.
            int low = 0, high = text.Length;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (MeasureTextHeight(text.Substring(mid), prototype, width) <= maxHeight)
                    high = mid;
                else
                    low = mid + 1;
            }

            cut = SnapToWordBoundary(text, low);
            return text.Substring(cut);
        }

        private static int SnapToWordBoundary(string text, int cut)
        {
            if (cut <= 0 || cut >= text.Length)
                return cut;

            int i = cut;
            int limit = Math.Min(text.Length, cut + 24);
            while (i < limit && !char.IsWhiteSpace(text[i]) && !IsCjk(text[i]))
                i++;

            if (i >= limit)
                return cut;

            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;

            return i;
        }

        private static bool IsCjk(char c) => c >= 0x2E80;

        private static double MeasureTextHeight(string text, TextBlock prototype, double width)
        {
            measureBlock ??= new TextBlock { TextWrapping = TextWrapping.Wrap };
            measureBlock.FontFamily = prototype.FontFamily;
            measureBlock.FontSize = prototype.FontSize;
            measureBlock.FontStyle = prototype.FontStyle;
            measureBlock.FontWeight = prototype.FontWeight;
            measureBlock.FontStretch = prototype.FontStretch;
            measureBlock.TextWrapping = TextWrapping.Wrap;
            measureBlock.Text = text;
            measureBlock.Measure(new Size(width, double.PositiveInfinity));
            return measureBlock.DesiredSize.Height;
        }

        private static string Slice(string text, int cut, int start, int end)
        {
            int from = Math.Max(start, cut);
            int to = Math.Min(end, text.Length);
            return to > from ? text.Substring(from, to - from) : string.Empty;
        }

        private static void SetRunText(Run run, string text)
        {
            if (run.Text != text)
                run.Text = text;
        }

        public void ApplyFontSize()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                OriginalCaption.FontSize = Translator.Setting.OverlayWindow.FontSize;
                TranslatedCaption.FontSize = (int)(OriginalCaption.FontSize * 1.25);

                RefreshCaptionText();
            }), DispatcherPriority.Background);
        }

        public void ApplyFontStroke()
        {
            OriginalCaptionDecorator.StrokeThickness = Translator.Setting.OverlayWindow.FontStroke;
            TranslatedCaptionDecorator.StrokeThickness = Translator.Setting.OverlayWindow.FontStroke;
        }

        public void ApplyBackgroundOpacity()
        {
            Color color = ((SolidColorBrush)BorderBackground.Background).Color;
            BorderBackground.Background = new SolidColorBrush(Color.FromArgb(
                (byte)Translator.Setting.OverlayWindow.Opacity, color.R, color.G, color.B));
        }

        private void UpdateTranslationColor(SolidColorBrush brush)
        {
            var color = brush.Color;

            double target = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B > 127 ? 0 : 255;
            byte r = (byte)Math.Clamp(color.R + (target - color.R) * 0.3, 0, 255);
            byte g = (byte)Math.Clamp(color.G + (target - color.G) * 0.4, 0, 255);
            byte b = (byte)Math.Clamp(color.B + (target - color.B) * 0.3, 0, 255);

            NoticePrefixRun.Foreground = brush;
            PreviousTranslationRun.Foreground = brush;
            CurrentTranslationRun.Foreground = new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }
}
