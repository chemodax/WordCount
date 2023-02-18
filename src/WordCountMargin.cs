using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace WordCount
{
    /// <summary>
    /// Margin's canvas and visual definition including both size and content
    /// </summary>
    internal class WordCountMargin : Label, IWpfTextViewMargin, INotifyPropertyChanged
    {
        /// <summary>
        /// Margin name.
        /// </summary>
        public const string MarginName = "WordCountMargin";

        /// <summary>
        /// A value indicating whether the object is disposed.
        /// </summary>
        private bool isDisposed;
        private AsyncAutoResetEvent updateEvent;
        private CancellationTokenSource cancellationSource;
        private Task updaterTask;
        readonly private IWpfTextView textView;

        public ITextBuffer textBuffer { get; }

        string labelText = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public string LabelText
        {
            get
            {
                return labelText;
            }

            set
            {
                if (labelText != value)
                {
                    labelText = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("LabelText"));
                    }
                }
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="WordCountMargin"/> class for a given <paramref name="textView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> to attach the margin to.</param>
        public WordCountMargin(IWpfTextView textView)
        {
            this.MinWidth = 50;
            this.Padding = new Thickness(8, 0, 5, 0);
            this.ClipToBounds = true;
            this.VerticalContentAlignment = VerticalAlignment.Center;
            this.DataContext = this;
            this.SetResourceReference(Label.StyleProperty, VsResourceKeys.LabelEnvironment90PercentFontSizeStyleKey);
            this.SetResourceReference(Control.BackgroundProperty, VsBrushes.ScrollBarBackgroundKey);
            this.SetResourceReference(Control.ForegroundProperty, CommonControlsColors.TextBoxTextBrushKey);
            this.SetBinding(Label.ContentProperty, "LabelText");

            this.textView = textView;
            this.textBuffer = textView.TextBuffer;
            this.textBuffer.Changed += TextBuffer_Changed;
            this.textView.Selection.SelectionChanged += TextView_SelectionChanged;

            this.updateEvent = new AsyncAutoResetEvent();
            this.cancellationSource = new CancellationTokenSource();
            this.updaterTask = Task.Run(UpdaterTaskAsync, this.cancellationSource.Token);

            this.updateEvent.Set();
        }

        private async Task UpdaterTaskAsync()
        {
            while (true)
            {
                await this.updateEvent.WaitAsync(this.cancellationSource.Token);

                UpdateText();
            }
        }

        bool IsSeparator(char ch)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            switch (uc)
            {
                case UnicodeCategory.DashPunctuation:
                    // Do not count dash as separator.
                    return false;

                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.MathSymbol:
                case UnicodeCategory.OtherPunctuation:
                    return true;

                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.Control:
                    return true;

                default:
                    return false;
            }
        }

        private void UpdateText()
        {
            ITextSelection selection = textView.Selection;
            NormalizedSnapshotSpanCollection spans;
            if (selection.IsEmpty)
            {
                ITextSnapshot snapshot = textBuffer.CurrentSnapshot;
                spans = new NormalizedSnapshotSpanCollection(snapshot, new Span(0, snapshot.Length));
            }
            else
            {
                spans = selection.SelectedSpans;
            }

            int charCount = 0;
            int wordCount = 0;
            int lineCount = 0;
            foreach (SnapshotSpan span in spans)
            {
                bool prevSeparator = true;
                ITextSnapshot snapshot = span.Snapshot;
                for (int idx = span.Start; idx < span.End; idx++)
                {
                    char ch = snapshot[idx];
                    bool isWhitespace = Char.IsWhiteSpace(ch);
                    bool isSeparator = IsSeparator(ch);

                    if (!isWhitespace)
                        charCount++;

                    if (!isSeparator && prevSeparator)
                        wordCount++;

                    prevSeparator = isSeparator;
                }

                if (!span.IsEmpty)
                {
                    int startLine = snapshot.GetLineNumberFromPosition(span.Start);
                    int endLine = snapshot.GetLineNumberFromPosition(span.End);

                    lineCount += endLine - startLine + 1;
                }
            }

            LabelText = string.Format("Chars: {0}  Words: {1}  Lines: {2}", charCount, wordCount, lineCount);
        }

        private void TextBuffer_Changed(object sender, EventArgs e)
        {
            updateEvent.Set();
        }

        private void TextView_SelectionChanged(object sender, EventArgs e)
        {
            updateEvent.Set();
        }

        #region IWpfTextViewMargin

        /// <summary>
        /// Gets the <see cref="Sytem.Windows.FrameworkElement"/> that implements the visual representation of the margin.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public FrameworkElement VisualElement
        {
            // Since this margin implements Canvas, this is the object which renders
            // the margin.
            get
            {
                this.ThrowIfDisposed();
                return this;
            }
        }

        #endregion

        #region ITextViewMargin

        /// <summary>
        /// Gets the size of the margin.
        /// </summary>
        /// <remarks>
        /// For a horizontal margin this is the height of the margin,
        /// since the width will be determined by the <see cref="ITextView"/>.
        /// For a vertical margin this is the width of the margin,
        /// since the height will be determined by the <see cref="ITextView"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public double MarginSize
        {
            get
            {
                this.ThrowIfDisposed();

                return this.ActualHeight;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the margin is enabled.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public bool Enabled
        {
            get
            {
                this.ThrowIfDisposed();

                // The margin should always be enabled
                return true;
            }
        }

        /// <summary>
        /// Gets the <see cref="ITextViewMargin"/> with the given <paramref name="marginName"/> or null if no match is found
        /// </summary>
        /// <param name="marginName">The name of the <see cref="ITextViewMargin"/></param>
        /// <returns>The <see cref="ITextViewMargin"/> named <paramref name="marginName"/>, or null if no match is found.</returns>
        /// <remarks>
        /// A margin returns itself if it is passed its own name. If the name does not match and it is a container margin, it
        /// forwards the call to its children. Margin name comparisons are case-insensitive.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="marginName"/> is null.</exception>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Equals(marginName, WordCountMargin.MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;
        }

        /// <summary>
        /// Disposes an instance of <see cref="WordCountMargin"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!this.isDisposed)
            {
                GC.SuppressFinalize(this);
                if (cancellationSource != null)
                {
                    cancellationSource.Cancel();
                    cancellationSource.Dispose();
                    cancellationSource = null;

                    if (updaterTask != null)
                    {
                        try
                        {
                            updaterTask.Wait();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                this.isDisposed = true;
            }
        }

        #endregion

        /// <summary>
        /// Checks and throws <see cref="ObjectDisposedException"/> if the object is disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(MarginName);
            }
        }
    }
}
