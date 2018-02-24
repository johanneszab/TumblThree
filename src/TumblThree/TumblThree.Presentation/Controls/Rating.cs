using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TumblThree.Presentation.Controls
{
    [TemplatePart(Name = "PART_RatingItems", Type = typeof(ItemsControl))]
    public class Rating : Slider
    {
        private ItemsControl itemsControl;
        private ReadOnlyCollection<RatingItem> ratingItems;

        static Rating()
        {
            MinimumProperty.OverrideMetadata(typeof(Rating),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
            MaximumProperty.OverrideMetadata(typeof(Rating),
                new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
            IsSnapToTickEnabledProperty.OverrideMetadata(typeof(Rating), new FrameworkPropertyMetadata(true));
            SmallChangeProperty.OverrideMetadata(typeof(Rating), new FrameworkPropertyMetadata(1.0));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Rating), new FrameworkPropertyMetadata(typeof(Rating)));
        }

        public Rating()
        {
            ratingItems = new ReadOnlyCollection<RatingItem>(new RatingItem[0]);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            itemsControl = (ItemsControl)GetTemplateChild("PART_RatingItems");
            GenerateRatingItems();
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            foreach (RatingItem ratingItem in ratingItems)
            {
                ratingItem.Value = Value;
            }
        }

        private void ItemMouseEnter(object sender, MouseEventArgs e)
        {
            double mouseOverValue = ((RatingItem)sender).ItemValue;
            foreach (RatingItem ratingItem in ratingItems)
            {
                ratingItem.MouseOverValue = mouseOverValue;
            }
        }

        private void ItemMouseLeave(object sender, MouseEventArgs e)
        {
            foreach (RatingItem ratingItem in ratingItems)
            {
                ratingItem.MouseOverValue = -1;
            }
        }

        private void ItemClick(object sender, RoutedEventArgs e)
        {
            double clickValue = ((RatingItem)sender).ItemValue;
            SetCurrentValue(ValueProperty, clickValue);
        }

        private void GenerateRatingItems()
        {
            // Clean up old items
            foreach (RatingItem item in ratingItems)
            {
                item.MouseEnter -= ItemMouseEnter;
                item.MouseLeave -= ItemMouseLeave;
                item.Click -= ItemClick;
            }

            // Create new items
            List<RatingItem> items = new List<RatingItem>();
            for (int i = 0; i < Convert.ToInt32(GetValue(MaximumProperty), CultureInfo.InvariantCulture); i++)
            {
                RatingItem item = new RatingItem() { ItemValue = i + 1, Value = Value };
                item.MouseEnter += ItemMouseEnter;
                item.MouseLeave += ItemMouseLeave;
                item.Click += ItemClick;
                items.Add(item);
            }
            ratingItems = new ReadOnlyCollection<RatingItem>(items);
            itemsControl.ItemsSource = ratingItems;
        }
    }
}
