using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TumblThree.Presentation.Controls
{
    public class SearchableTextBlock : TextBlock
    {
        public static new readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SearchableTextBlock), new FrameworkPropertyMetadata(string.Empty, ControlPropertyChangedCallback));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(SearchableTextBlock), new FrameworkPropertyMetadata(string.Empty, ControlPropertyChangedCallback));

        public static readonly DependencyProperty HighlightBackgroundProperty =
            DependencyProperty.Register(nameof(HighlightBackground), typeof(Brush), typeof(SearchableTextBlock), new FrameworkPropertyMetadata(Brushes.Orange, ControlPropertyChangedCallback));

        public static readonly DependencyProperty IsMatchCaseProperty =
            DependencyProperty.Register(nameof(IsMatchCase), typeof(bool), typeof(SearchableTextBlock), new FrameworkPropertyMetadata(false, ControlPropertyChangedCallback));


        private IReadOnlyList<string> textParts = new string[0];


        public new string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public Brush HighlightBackground
        {
            get { return (Brush)GetValue(HighlightBackgroundProperty); }
            set { SetValue(HighlightBackgroundProperty, value); }
        }

        public bool IsMatchCase
        {
            get { return (bool)GetValue(IsMatchCaseProperty); }
            set { SetValue(IsMatchCaseProperty, value); }
        }


        private void UpdateContet()
        {
            var newTextParts = SplitText();
            if (textParts.SequenceEqual(newTextParts))
            {
                return;
            }

            var highlightBackground = HighlightBackground;
            Inlines.Clear();
            bool isHighlight = false;
            foreach (var textPart in newTextParts)
            {
                if (!string.IsNullOrEmpty(textPart))
                {
                    if (isHighlight)
                    {
                        Inlines.Add(new Run(textPart) { Background = highlightBackground });
                    }
                    else
                    {
                        Inlines.Add(new Run(textPart));
                    }
                }

                isHighlight = !isHighlight;
            }

            textParts = newTextParts;
        }

        private IReadOnlyList<string> SplitText()
        {
            var text = Text;
            var searchText = SearchText;

            if (string.IsNullOrEmpty(searchText))
            {
                return new[] { text };
            }

            List<string> textParts = new List<string>();
            int index = 0;
            var comparisonType = IsMatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
            while (true)
            {
                int position = text.IndexOf(searchText, index, comparisonType);
                if (position < 0)
                {
                    break;
                }
                textParts.Add(text.Substring(index, (position - index)));
                textParts.Add(text.Substring(position, searchText.Length));
                index = position + searchText.Length;
            }

            textParts.Add(text.Substring(index, text.Length - index));
            return textParts;
        }

        private static void ControlPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SearchableTextBlock)d).UpdateContet();
        }
    }
}
