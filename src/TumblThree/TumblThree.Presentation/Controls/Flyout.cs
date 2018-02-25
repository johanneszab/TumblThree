using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace TumblThree.Presentation.Controls
{
    public class Flyout : Popup
    {
        public static readonly DependencyProperty HorizontalFlyoutAlignmentProperty =
            DependencyProperty.Register(nameof(HorizontalFlyoutAlignment), typeof(HorizontalFlyoutAlignment), typeof(Flyout),
                new PropertyMetadata(HorizontalFlyoutAlignment.Left));

        public new static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(Flyout), new PropertyMetadata(0d));

        private readonly Stopwatch closedStopwatch;

        static Flyout()
        {
            StaysOpenProperty.OverrideMetadata(typeof(Flyout), new FrameworkPropertyMetadata(false));
            AllowsTransparencyProperty.OverrideMetadata(typeof(Flyout), new FrameworkPropertyMetadata(true));
            PopupAnimationProperty.OverrideMetadata(typeof(Flyout), new FrameworkPropertyMetadata(PopupAnimation.Slide));
            IsOpenProperty.OverrideMetadata(typeof(Flyout),
                new FrameworkPropertyMetadata(IsOpenPropertyChangedCallback, IsOpenCoerceValueCallback));
        }

        public Flyout()
        {
            closedStopwatch = new Stopwatch();
        }

        public HorizontalFlyoutAlignment HorizontalFlyoutAlignment
        {
            get { return (HorizontalFlyoutAlignment)GetValue(HorizontalFlyoutAlignmentProperty); }
            set { SetValue(HorizontalFlyoutAlignmentProperty, value); }
        }

        public new double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            var target = (FrameworkElement)PlacementTarget;
            var child = (FrameworkElement)Child;

            if (HorizontalFlyoutAlignment == HorizontalFlyoutAlignment.Left)
            {
                if (!SystemParameters.MenuDropAlignment)
                {
                    SetBaseHorizontalOffset(HorizontalOffset);
                }
                else
                {
                    // Handedness = right handed; shows the context menus on the left side
                    SetBaseHorizontalOffset(-target.ActualWidth + child.ActualWidth + HorizontalOffset);
                }
            }
            else if (HorizontalFlyoutAlignment == HorizontalFlyoutAlignment.Right)
            {
                if (!SystemParameters.MenuDropAlignment)
                {
                    SetBaseHorizontalOffset(target.ActualWidth - child.ActualWidth + HorizontalOffset);
                }
                else
                {
                    // Handedness = right handed; shows the context menus on the left side
                    SetBaseHorizontalOffset(HorizontalOffset);
                }
            }
            else
            {
                if (!SystemParameters.MenuDropAlignment)
                {
                    SetBaseHorizontalOffset((target.ActualWidth / 2) - (child.ActualWidth / 2) + HorizontalOffset);
                }
                else
                {
                    // Handedness = right handed; shows the context menus on the left side
                    SetBaseHorizontalOffset((-target.ActualWidth / 2) + (child.ActualWidth / 2) + HorizontalOffset);
                }
            }

            child.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsOpen = false;
            }
            base.OnPreviewKeyUp(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            closedStopwatch.Restart();
        }

        private void SetBaseHorizontalOffset(double value)
        {
            SetValue(Popup.HorizontalOffsetProperty, value);
        }

        private static void IsOpenPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Just needed for the FrameworkPropertyMetadata API
        }

        private static object IsOpenCoerceValueCallback(DependencyObject d, object baseValue)
        {
            var flyout = (Flyout)d;
            if (flyout.closedStopwatch.IsRunning && flyout.closedStopwatch.ElapsedMilliseconds < 200)
            {
                return DependencyProperty.UnsetValue;
            }
            return baseValue;
        }
    }
}
