using System.Windows;
using System.Windows.Controls;

namespace TumblThree.Presentation.Controls
{
    public class SuperToolTip : Control
    {
        static SuperToolTip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SuperToolTip), new FrameworkPropertyMetadata(typeof(SuperToolTip)));
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SuperToolTip), new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SuperToolTip), new FrameworkPropertyMetadata(""));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }
    }
}
