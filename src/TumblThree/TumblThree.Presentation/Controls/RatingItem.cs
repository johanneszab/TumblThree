using System.Windows;
using System.Windows.Controls.Primitives;

namespace TumblThree.Presentation.Controls
{
    public class RatingItem : ButtonBase
    {
        private static readonly DependencyPropertyKey IsMouseOverRatingPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsMouseOverRating), typeof(bool), typeof(RatingItem), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsMouseOverRatingProperty = IsMouseOverRatingPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey RatingItemStatePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(RatingItemState), typeof(RatingItemState), typeof(RatingItem), new FrameworkPropertyMetadata(RatingItemState.Empty));

        public static readonly DependencyProperty RatingItemStateProperty = RatingItemStatePropertyKey.DependencyProperty;



        static RatingItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RatingItem), new FrameworkPropertyMetadata(typeof(RatingItem)));
        }


        private double value;
        private double mouseOverValue;


        internal int ItemValue { get; set; }

        internal double Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    UpdateRatingItemState();
                }
            }
        }

        internal double MouseOverValue
        {
            get { return mouseOverValue; }
            set
            {
                if (mouseOverValue != value)
                {
                    mouseOverValue = value;
                    UpdateRatingItemState();
                }
            }
        }

        public bool IsMouseOverRating
        {
            get { return (bool)GetValue(IsMouseOverRatingProperty); }
        }

        public RatingItemState RatingItemState
        {
            get { return (RatingItemState)GetValue(RatingItemStateProperty); }
        }

        private void UpdateRatingItemState()
        {
            RatingItemState state;

            double stateValue;
            if (MouseOverValue >= 1)
            {
                stateValue = MouseOverValue;
            }
            else
            {
                stateValue = Value;
            }

            if (stateValue >= ItemValue)
            {
                state = RatingItemState.Filled;
            }
            else if (stateValue > ItemValue - 1)
            {
                state = RatingItemState.Partial;
            }
            else
            {
                state = RatingItemState.Empty;
            }

            SetValue(RatingItemStatePropertyKey, state);
        }
    }
}
