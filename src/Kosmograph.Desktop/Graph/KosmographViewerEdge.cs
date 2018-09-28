/*
Microsoft Automatic Graph Layout,MSAGL

Copyright (c) Microsoft Corporation

All rights reserved.

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Kosmograph.Desktop.Graph.Base;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.WpfGraphControl;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using GeometryPoint = Microsoft.Msagl.Core.Geometry.Point;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Size = System.Windows.Size;

namespace Kosmograph.Desktop.Graph
{
    public class KosmographViewerEdge : KosmographViewerItemBase, IViewerEdge, IInvalidatable
    {
        public KosmographViewerEdge(DrawingEdge edge, FrameworkElement labelFrameworkElement)
        {
            Edge = edge;
            CurvePath = new Path
            {
                Data = GetICurveWpfGeometry(edge.GeometryEdge.Curve),
                Tag = this
            };

            EdgeAttrClone = edge.Attr.Clone();

            if (edge.Attr.ArrowAtSource)
                SourceArrowHeadPath = new Path
                {
                    Data = DefiningSourceArrowHead(),
                    Tag = this
                };
            if (edge.Attr.ArrowAtTarget)
                TargetArrowHeadPath = new Path
                {
                    Data = DefiningTargetArrowHead(Edge.GeometryEdge.EdgeGeometry, PathStrokeThickness),
                    Tag = this
                };

            SetPathStroke();

            if (labelFrameworkElement != null)
            {
                LabelFrameworkElement = labelFrameworkElement;
                Wpf2MsaglConverters.PositionFrameworkElement(LabelFrameworkElement, edge.Label.Center, 1);
            }
            edge.Attr.VisualsChanged += (a, b) => Invalidate();

            edge.IsVisibleChanged += obj =>
            {
                foreach (var frameworkElement in FrameworkElements)
                {
                    frameworkElement.Visibility = edge.IsVisible ? Visibility.Visible : Visibility.Hidden;
                }
            };
        }

        #region Edge viewer is composed of multiple visual elements

        public FrameworkElement LabelFrameworkElement;

        public Path CurvePath { get; set; }

        public Path SourceArrowHeadPath { get; set; }

        public Path TargetArrowHeadPath { get; set; }

        override public IEnumerable<FrameworkElement> FrameworkElements
        {
            get
            {
                if (this.SourceArrowHeadPath != null)
                    yield return this.SourceArrowHeadPath;
                if (this.TargetArrowHeadPath != null)
                    yield return this.TargetArrowHeadPath;

                if (this.CurvePath != null)
                    yield return this.CurvePath;

                if (this.LabelFrameworkElement != null)
                    yield return this.LabelFrameworkElement;
            }
        }

        #endregion Edge viewer is composed of multiple visual elements

        public EdgeAttr EdgeAttrClone { get; set; }

        public static Geometry DefiningTargetArrowHead(EdgeGeometry edgeGeometry, double thickness)
        {
            if (edgeGeometry.TargetArrowhead == null || edgeGeometry.Curve == null)
                return null;
            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open())
            {
                AddArrow(context, edgeGeometry.Curve.End,
                         edgeGeometry.TargetArrowhead.TipPosition, thickness);
                return streamGeometry;
            }
        }

        private Geometry DefiningSourceArrowHead()
        {
            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open())
            {
                AddArrow(context, Edge.GeometryEdge.Curve.Start, Edge.GeometryEdge.EdgeGeometry.SourceArrowhead.TipPosition, PathStrokeThickness);
                return streamGeometry;
            }
        }

        private double PathStrokeThickness
        {
            get
            {
                return PathStrokeThicknessFunc != null ? PathStrokeThicknessFunc() : this.Edge.Attr.LineWidth;
            }
        }

        static public Geometry GetICurveWpfGeometry(ICurve curve)
        {
            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open())
            {
                FillStreamGeometryContext(context, curve);
                return streamGeometry;
            }
        }

        private static void FillStreamGeometryContext(StreamGeometryContext context, ICurve curve)
        {
            if (curve == null)
                return;
            FillContextForICurve(context, curve);
        }

        static public void FillContextForICurve(StreamGeometryContext context, ICurve iCurve)
        {
            context.BeginFigure(iCurve.Start.ToWpf(), false, false);

            var c = iCurve as Curve;
            if (c != null)
                FillContexForCurve(context, c);
            else
            {
                var cubicBezierSeg = iCurve as CubicBezierSegment;
                if (cubicBezierSeg != null)
                    context.BezierTo(cubicBezierSeg.B(1).ToWpf(), cubicBezierSeg.B(2).ToWpf(), cubicBezierSeg.B(3).ToWpf(), true, false);
                else
                {
                    var ls = iCurve as LineSegment;
                    if (ls != null)
                        context.LineTo(ls.End.ToWpf(), true, false);
                    else
                    {
                        var rr = iCurve as RoundedRect;
                        if (rr != null)
                            FillContexForCurve(context, rr.Curve);
                        else
                        {
                            var poly = iCurve as Polyline;
                            if (poly != null)
                                FillContexForPolyline(context, poly);
                            else
                            {
                                var ellipse = iCurve as Ellipse;
                                if (ellipse != null)
                                {
                                    //       context.LineTo(Common.WpfPoint(ellipse.End),true,false);
                                    double sweepAngle = EllipseSweepAngle(ellipse);
                                    bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                                    Rectangle box = ellipse.FullBox();
                                    context.ArcTo(ellipse.End.ToWpf(),
                                                  new Size(box.Width / 2, box.Height / 2),
                                                  sweepAngle,
                                                  largeArc,
                                                  sweepAngle < 0
                                                      ? SweepDirection.Counterclockwise
                                                      : SweepDirection.Clockwise,
                                                  true, true);
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void FillContexForPolyline(StreamGeometryContext context, Polyline poly)
        {
            for (PolylinePoint pp = poly.StartPoint.Next; pp != null; pp = pp.Next)
                context.LineTo(pp.Point.ToWpf(), true, false);
        }

        private static void FillContexForCurve(StreamGeometryContext context, Curve c)
        {
            foreach (ICurve seg in c.Segments)
            {
                var bezSeg = seg as CubicBezierSegment;
                if (bezSeg != null)
                {
                    context.BezierTo(bezSeg.B(1).ToWpf(), bezSeg.B(2).ToWpf(), bezSeg.B(3).ToWpf(), true, false);
                }
                else
                {
                    var ls = seg as LineSegment;
                    if (ls != null)
                        context.LineTo(ls.End.ToWpf(), true, false);
                    else
                    {
                        var ellipse = seg as Ellipse;
                        if (ellipse != null)
                        {
                            //       context.LineTo(Common.WpfPoint(ellipse.End),true,false);
                            double sweepAngle = EllipseSweepAngle(ellipse);
                            bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                            Rectangle box = ellipse.FullBox();
                            context.ArcTo(ellipse.End.ToWpf(),
                                          new Size(box.Width / 2, box.Height / 2),
                                          sweepAngle,
                                          largeArc,
                                          sweepAngle < 0
                                              ? SweepDirection.Counterclockwise
                                              : SweepDirection.Clockwise,
                                          true, true);
                        }
                        else
                            throw new NotImplementedException();
                    }
                }
            }
        }

        public static double EllipseSweepAngle(Ellipse ellipse)
        {
            double sweepAngle = ellipse.ParEnd - ellipse.ParStart;
            return ellipse.OrientedCounterclockwise() ? sweepAngle : -sweepAngle;
        }

        private static void AddArrow(StreamGeometryContext context, GeometryPoint start, GeometryPoint end, double thickness)
        {
            if (thickness > 1)
            {
                GeometryPoint dir = end - start;
                GeometryPoint h = dir;
                double dl = dir.Length;
                if (dl < 0.001)
                    return;
                dir /= dl;

                var s = new GeometryPoint(-dir.Y, dir.X);
                double w = 0.5 * thickness;
                GeometryPoint s0 = w * s;

                s *= h.Length * HalfArrowAngleTan;
                s += s0;

                double rad = w / HalfArrowAngleCos;

                context.BeginFigure((start + s).ToWpf(), true, true);
                context.LineTo((start - s).ToWpf(), true, false);
                context.LineTo((end - s0).ToWpf(), true, false);
                context.ArcTo((end + s0).ToWpf(), new Size(rad, rad), Math.PI - ArrowAngle, false, SweepDirection.Clockwise, true, false);
            }
            else
            {
                GeometryPoint dir = end - start;
                double dl = dir.Length;
                //take into account the widths
                double delta = Math.Min(dl / 2, thickness + thickness / 2);
                dir *= (dl - delta) / dl;
                end = start + dir;
                dir = dir.Rotate(Math.PI / 2);
                GeometryPoint s = dir * HalfArrowAngleTan;

                context.BeginFigure((start + s).ToWpf(), true, true);
                context.LineTo(end.ToWpf(), true, true);
                context.LineTo((start - s).ToWpf(), true, true);
            }
        }

        private static readonly double HalfArrowAngleTan = Math.Tan(ArrowAngle * 0.5 * Math.PI / 180.0);
        private static readonly double HalfArrowAngleCos = Math.Cos(ArrowAngle * 0.5 * Math.PI / 180.0);
        private const double ArrowAngle = 30.0; //degrees

        #region Implementation of IViewerObject

        public DrawingObject DrawingObject => this.Edge;

        public bool MarkedForDragging { get; set; }

        public event EventHandler MarkedForDraggingEvent;

        public event EventHandler UnmarkedForDraggingEvent;

        #endregion Implementation of IViewerObject

        #region Implementation of IViewerEdge

        public DrawingEdge Edge { get; private set; }
        public IViewerNode Source { get; private set; }
        public IViewerNode Target { get; private set; }
        public double RadiusOfPolylineCorner { get; set; }

        public VLabel VLabel { get; set; }

        #endregion Implementation of IViewerEdge

        public void Invalidate(FrameworkElement fe, Rail rail, byte edgeTransparency)
        {
            var path = fe as Path;
            if (path != null)
                SetPathStrokeToRailPath(rail, path, edgeTransparency);
        }

        public void Invalidate()
        {
            var vis = Edge.IsVisible ? Visibility.Visible : Visibility.Hidden;
            foreach (var fe in FrameworkElements) fe.Visibility = vis;
            if (vis == Visibility.Hidden)
                return;
            CurvePath.Data = GetICurveWpfGeometry(Edge.GeometryEdge.Curve);
            if (Edge.Attr.ArrowAtSource)
                SourceArrowHeadPath.Data = DefiningSourceArrowHead();
            if (Edge.Attr.ArrowAtTarget)
                TargetArrowHeadPath.Data = DefiningTargetArrowHead(Edge.GeometryEdge.EdgeGeometry, PathStrokeThickness);
            SetPathStroke();
            if (VLabel != null)
                ((IInvalidatable)VLabel).Invalidate();
        }

        private void SetPathStroke()
        {
            SetPathStrokeToPath(CurvePath);
            if (SourceArrowHeadPath != null)
            {
                SourceArrowHeadPath.Stroke = SourceArrowHeadPath.Fill = Edge.Attr.Color.ToWpf();
                SourceArrowHeadPath.StrokeThickness = PathStrokeThickness;
            }
            if (TargetArrowHeadPath != null)
            {
                TargetArrowHeadPath.Stroke = TargetArrowHeadPath.Fill = Edge.Attr.Color.ToWpf();
                TargetArrowHeadPath.StrokeThickness = PathStrokeThickness;
            }
        }

        private void SetPathStrokeToRailPath(Rail rail, Path path, byte transparency)
        {
            path.Stroke = SetStrokeColorForRail(transparency, rail);
            path.StrokeThickness = PathStrokeThickness;

            foreach (var style in Edge.Attr.Styles)
            {
                if (style == Microsoft.Msagl.Drawing.Style.Dotted)
                {
                    path.StrokeDashArray = new DoubleCollection { 1, 1 };
                }
                else if (style == Microsoft.Msagl.Drawing.Style.Dashed)
                {
                    var f = DashSize();
                    path.StrokeDashArray = new DoubleCollection { f, f };
                    //CurvePath.StrokeDashOffset = f;
                }
            }
        }

        private Brush SetStrokeColorForRail(byte transparency, Rail rail)
        {
            return rail.IsHighlighted == false
                       ? new SolidColorBrush(new System.Windows.Media.Color
                       {
                           A = transparency,
                           R = Edge.Attr.Color.R,
                           G = Edge.Attr.Color.G,
                           B = Edge.Attr.Color.B
                       })
                       : Brushes.Red;
        }

        private void SetPathStrokeToPath(Path path)
        {
            path.Stroke = Edge.Attr.Color.ToWpf();
            path.StrokeThickness = PathStrokeThickness;

            foreach (var style in Edge.Attr.Styles)
            {
                if (style == Microsoft.Msagl.Drawing.Style.Dotted)
                {
                    path.StrokeDashArray = new DoubleCollection { 1, 1 };
                }
                else if (style == Microsoft.Msagl.Drawing.Style.Dashed)
                {
                    var f = DashSize();
                    path.StrokeDashArray = new DoubleCollection { f, f };
                    //CurvePath.StrokeDashOffset = f;
                }
            }
        }

        public override string ToString()
        {
            return Edge.ToString();
        }

        public static double _dashSize = 0.05; //inches
        public Func<double> PathStrokeThicknessFunc;

        public KosmographViewerEdge(DrawingEdge edge, LgLayoutSettings lgSettings)
        {
            Edge = edge;
            EdgeAttrClone = edge.Attr.Clone();
        }

        public double DashSize()
        {
            var w = PathStrokeThickness;
            var dashSizeInPoints = _dashSize * KosmographViewer.DpiXStatic;
            return dashSizeInPoints / w;
        }

        public void RemoveItselfFromCanvas(Canvas graphCanvas)
        {
            if (CurvePath != null)
                graphCanvas.Children.Remove(CurvePath);

            if (SourceArrowHeadPath != null)
                graphCanvas.Children.Remove(SourceArrowHeadPath);

            if (TargetArrowHeadPath != null)
                graphCanvas.Children.Remove(TargetArrowHeadPath);

            if (VLabel != null)
                graphCanvas.Children.Remove(VLabel.FrameworkElement);
        }

        public FrameworkElement CreateFrameworkElementForRail(Rail rail, byte edgeTransparency)
        {
            var iCurve = rail.Geometry as ICurve;
            Path fe;
            if (iCurve != null)
            {
                fe = (Path)CreateFrameworkElementForRailCurve(rail, iCurve, edgeTransparency);
            }
            else
            {
                var arrowhead = rail.Geometry as Arrowhead;
                if (arrowhead != null)
                {
                    fe = (Path)CreateFrameworkElementForRailArrowhead(rail, arrowhead, rail.CurveAttachmentPoint, edgeTransparency);
                }
                else
                    throw new InvalidOperationException();
            }
            fe.Tag = rail;
            return fe;
        }

        private FrameworkElement CreateFrameworkElementForRailArrowhead(Rail rail, Arrowhead arrowhead, GeometryPoint curveAttachmentPoint, byte edgeTransparency)
        {
            var streamGeometry = new StreamGeometry();

            using (StreamGeometryContext context = streamGeometry.Open())
            {
                AddArrow(context, curveAttachmentPoint, arrowhead.TipPosition,
                         PathStrokeThickness);
            }

            var path = new Path
            {
                Data = streamGeometry,
                Tag = this
            };

            SetPathStrokeToRailPath(rail, path, edgeTransparency);
            return path;
        }

        private FrameworkElement CreateFrameworkElementForRailCurve(Rail rail, ICurve iCurve, byte transparency)
        {
            var path = new Path
            {
                Data = GetICurveWpfGeometry(iCurve),
            };
            SetPathStrokeToRailPath(rail, path, transparency);

            return path;
        }
    }
}