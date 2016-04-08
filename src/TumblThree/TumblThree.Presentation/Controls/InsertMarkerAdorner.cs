using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace TumblThree.Presentation.Controls
{
    public class InsertMarkerAdorner : Adorner
    {
        private readonly FrameworkElement control;
        private FrameworkElement item;
        private bool showMarkerAfterItem;
        private Rect adornerViewRect;


        public InsertMarkerAdorner(FrameworkElement control)
            : base(control)
        {
            this.control = control;
        }


        public void ShowMarker(FrameworkElement item, bool showMarkerAfterItem)
        {
            this.item = item;
            this.showMarkerAfterItem = showMarkerAfterItem;
            InvalidateVisual();
        }

        public void ResetMarker()
        {
            this.item = null;
            InvalidateVisual();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            adornerViewRect = new Rect(finalSize);
            return base.ArrangeOverride(finalSize);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (item != null)
            {
                var itemRect = new Rect(item.RenderSize);
                Point startPoint;
                Point endPoint;
                if (!showMarkerAfterItem)
                {
                    startPoint = itemRect.TopLeft;
                    endPoint = itemRect.TopRight;
                }
                else
                {
                    startPoint = itemRect.BottomLeft;
                    endPoint = itemRect.BottomRight;
                }

                startPoint = item.TranslatePoint(startPoint, control);
                endPoint = item.TranslatePoint(endPoint, control);

                drawingContext.PushClip(new RectangleGeometry(adornerViewRect));
                drawingContext.DrawLine(new Pen(Brushes.Green, 2), startPoint, endPoint);
            }
        }
    }
}
