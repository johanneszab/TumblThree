using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace TumblThree.Presentation.Controls
{
    public static class ToolTipBehavior
    {
        public static readonly DependencyProperty AutoToolTipProperty =
            DependencyProperty.RegisterAttached("AutoToolTip", typeof(bool), typeof(ToolTipBehavior), new FrameworkPropertyMetadata(false, AutoToolTipPropertyChanged));


        [AttachedPropertyBrowsableForType(typeof(TextBlock))]
        public static bool GetAutoToolTip(DependencyObject element)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            return (bool)element.GetValue(AutoToolTipProperty);
        }

        public static void SetAutoToolTip(DependencyObject element, bool value)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            element.SetValue(AutoToolTipProperty, value);
        }

        private static void AutoToolTipPropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            TextBlock textBlock = element as TextBlock;
            if (textBlock == null)
            {
                throw new ArgumentException("The attached property AutoToolTip can only be used with a TextBlock.", nameof(element));
            }
            if (textBlock.TextTrimming == TextTrimming.None)
            {
                throw new InvalidOperationException("The attached property AutoToolTip can only be used with a TextBlock that uses one of the TextTrimming options.");
            }

            DependencyPropertyDescriptor textDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            if (e.NewValue.Equals(true))
            {
                ComputeAutoToolTip(textBlock);
                textDescriptor.AddValueChanged(textBlock, TextBlockTextChanged);
                textBlock.SizeChanged += TextBlockSizeChanged;
            }
            else
            {
                textDescriptor.RemoveValueChanged(textBlock, TextBlockTextChanged);
                textBlock.SizeChanged -= TextBlockSizeChanged;
            }
        }

        private static void TextBlockTextChanged(object sender, EventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            ComputeAutoToolTip(textBlock);
        }

        private static void TextBlockSizeChanged(object sender, SizeChangedEventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            ComputeAutoToolTip(textBlock);
        }

        private static void ComputeAutoToolTip(TextBlock textBlock)
        {
            // It is necessary to call Measure so that the DesiredSize gets updated.
            textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            var desiredWidth = textBlock.DesiredSize.Width;

            if (textBlock.ActualWidth < desiredWidth)
            {
                ToolTipService.SetToolTip(textBlock, textBlock.Text);
            }
            else
            {
                ToolTipService.SetToolTip(textBlock, null);
            }
        }
    }
}

