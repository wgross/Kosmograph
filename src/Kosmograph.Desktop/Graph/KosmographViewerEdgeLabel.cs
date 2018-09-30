using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.WpfGraphControl;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using DrawingLabel = Microsoft.Msagl.Drawing.Label;

namespace Kosmograph.Desktop.Graph
{
    public class KosmographViewerEdgeLabel : IViewerObject, IInvalidatable
    {
        public readonly FrameworkElement FrameworkElement;

        public DrawingLabel EdgeLabel { get; }

        public KosmographViewerEdgeLabel(Edge edge, FrameworkElement frameworkElement)
        {
            this.FrameworkElement = frameworkElement;
            this.EdgeLabel = edge.Label;
        }

        #region IViewerObject members

        public DrawingObject DrawingObject => this.EdgeLabel;

        public bool MarkedForDragging
        {
            get => this.markedForDragging;
            set
            {
                this.markedForDragging = value;
                if (value)
                {
                    this.AttachmentLine = new Line
                    {
                        Stroke = System.Windows.Media.Brushes.Black,
                        StrokeDashArray = new System.Windows.Media.DoubleCollection(OffsetElems())
                    }; //the line will have 0,0, 0,0 start and end so it would not be rendered

                    ((Canvas)FrameworkElement.Parent).Children.Add(AttachmentLine);
                }
                else
                {
                    ((Canvas)FrameworkElement.Parent).Children.Remove(AttachmentLine);
                    this.AttachmentLine = null;
                }
            }
        }

        private bool markedForDragging;

        public event EventHandler MarkedForDraggingEvent;

        public event EventHandler UnmarkedForDraggingEvent;

        #endregion IViewerObject members

        private IEnumerable<double> OffsetElems()
        {
            yield return 1;
            yield return 2;
        }

        private Line AttachmentLine { get; set; }

        #region IInvalidate members

        public void Invalidate()
        {
            var label = (DrawingLabel)DrawingObject;
            Wpf2MsaglConverters.PositionFrameworkElement(FrameworkElement, label.Center, 1);
            var geomLabel = label.GeometryLabel;
            if (AttachmentLine != null)
            {
                AttachmentLine.X1 = geomLabel.AttachmentSegmentStart.X;
                AttachmentLine.Y1 = geomLabel.AttachmentSegmentStart.Y;

                AttachmentLine.X2 = geomLabel.AttachmentSegmentEnd.X;
                AttachmentLine.Y2 = geomLabel.AttachmentSegmentEnd.Y;
            }
        }

        #endregion IInvalidate members
    }
}