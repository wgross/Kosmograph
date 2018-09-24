﻿using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Miscellaneous;
using System;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Kosmograph.Desktop.Graph
{
    public partial class KosmographViewer
    {
        /// <summary>
        /// The background of the canvas is kept because it is transformed during changes of the canvas size
        /// </summary>
        private System.Windows.Shapes.Rectangle canvasBackgroundRect;

        //private FrameworkElement _rectToFillGraphBackground;

        public event EventHandler LayoutStarted;

        public event EventHandler LayoutComplete;

        public CancelToken CancelToken { get; set; }

        public bool RunLayoutAsync { get; set; }

        public bool NeedToCalculateLayout
        {
            get { return needToCalculateLayout; }
            set { needToCalculateLayout = value; }
        }

        private GeometryGraph geometryGraphUnderLayout;

        private void LayoutGraph()
        {
            if (this.NeedToCalculateLayout)
            {
                try
                {
                    LayoutHelpers.CalculateLayout(this.geometryGraphUnderLayout, this.Graph.LayoutAlgorithmSettings, this.CancelToken);

                    //if (MsaglFileToSave != null)
                    //{
                    //    drawingGraph.Write(MsaglFileToSave);
                    //    Console.WriteLine("saved into {0}", MsaglFileToSave);
                    //    Environment.Exit(0);
                    //}
                }
                catch (OperationCanceledException)
                {
                    //swallow this exception
                }
            }
        }

        private void PostLayoutStep()
        {
            this.GraphCanvasShow();
            this.PushDataFromLayoutGraphToFrameworkElements();
            this.backgroundWorker = null; //this will signal that we are not under layout anymore
            this.GraphChanged.Invoke(this, null);
            this.SetInitialTransform();
        }

        private void PushDataFromLayoutGraphToFrameworkElements()
        {
            this.DrawGraphBackgound();

            this.CreateVNodes();
            this.CreateEdges();
        }

        #region Draw the background of the graph

        public void DrawGraphBackgound()
        {
            // at the very back (Z order -2) is a transparent background.
            this.canvasBackgroundRect = this.CreateCanvasBackgroundRect();
            this.GraphCanvas.Children.Add(this.canvasBackgroundRect);

            // behind the graph itself is a rect which has the graphs
            // background color (Z order -1)
            var graphBackground = this.CreateAndPositionGraphBackgroundRectangle();
            if (graphBackground != null)
                this.GraphCanvas.Children.Add(graphBackground);
        }

        private Rectangle CreateCanvasBackgroundRect()
        {
            var parent = (Panel)this.GraphCanvas.Parent;

            var rectangle = new System.Windows.Shapes.Rectangle()
            {
                Width = parent.ActualWidth,
                Height = parent.ActualHeight,
                Fill = System.Windows.Media.Brushes.Transparent
            };

            Canvas.SetLeft(rectangle, 0);
            Canvas.SetTop(rectangle, 0);
            Panel.SetZIndex(rectangle, -2);

            return rectangle;
        }

        private Rectangle CreateAndPositionGraphBackgroundRectangle()
        {
            var rect = this.SetGraphBackgroundSize(this.CreateGraphBackgroundRect());
            if (rect is null)
                return null;

            return rect;
        }

        private Rectangle CreateGraphBackgroundRect()
        {
            var lgGraphBrowsingSettings = drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgGraphBrowsingSettings is null)
            {
                return this.SetGraphBackgroundSize(new Rectangle());
            }
            else return null;
        }

        private Rectangle SetGraphBackgroundSize(Rectangle graphBackground)
        {
            if (this.GeometryGraph is null)
                return graphBackground;

            // Canvas.SetLeft(_rectToFillGraphBackground, geomGraph.Left);
            // Canvas.SetTop(_rectToFillGraphBackground, geomGraph.Bottom);
            graphBackground.Width = this.GeometryGraph.Width;
            graphBackground.Height = this.GeometryGraph.Height;
            graphBackground.Fill = this.Graph.Attr.BackgroundColor.ToWpf();

            Panel.SetZIndex(graphBackground, -1);

            Wpf2MsaglConverters.PositionFrameworkElement(graphBackground, this.GeometryGraph.BoundingBox.Center, 1);

            return graphBackground;
        }

        #endregion Draw the background of the graph
    }
}