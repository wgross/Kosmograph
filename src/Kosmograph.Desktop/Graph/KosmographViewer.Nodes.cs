﻿using Kosmograph.Desktop.ViewModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using GeometryRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;

namespace Kosmograph.Desktop.Graph
{
    public partial class KosmographViewer
    {
        private const double DesiredPathThicknessInInches = 0.008;

        private double GetBorderPathThickness() => DesiredPathThicknessInInches * DpiX;

        private void FillFrameworkElementsWithNodeLabels(DrawingNode drawingNode, out TextBlock fe)
        {
            fe = null;

            var textBlock = this.GraphCanvas
                .InvokeInUiThread(() => CreateTextBlockFromDrawingObjectLabel(drawingNode.Label));

            if (textBlock is null)
                return;

            this.drawingObjectsToFrameworkElements[drawingNode] = textBlock;
            fe = textBlock;
        }

        private void GetOrCreateViewNodes()
        {
            foreach (var node in this.Graph.Nodes.Concat(this.Graph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
            {
                this.Invalidate(this.GetOrCreateViewerNode(node));
            }
        }

        private KosmographViewerNode GetOrCreateViewerNode(DrawingNode drawingNode)
        {
            // this moethod looks like weird twin of IVIewer.CreateIViewerNode...
            lock (this.syncRoot)
            {
                if (this.drawingObjectsToIViewerObjects.TryGetValue(drawingNode, out var existingViewerNode))
                    return (KosmographViewerNode)existingViewerNode;

                // create unexisting node label
                TextBlock nodeLabel;
                if (!this.drawingObjectsToFrameworkElements.TryGetValue(drawingNode, out nodeLabel))
                    nodeLabel = this.CreateAndRegisterFrameworkElementOfDrawingNode(drawingNode);

                var viewerNode = new KosmographViewerNode(drawingNode, (TextBlock)nodeLabel,
                    funcFromDrawingEdgeToVEdge: e => (KosmographViewerEdge)drawingObjectsToIViewerObjects[e],
                    pathStrokeThicknessFunc: () => GetBorderPathThickness() * drawingNode.Attr.LineWidth);

                this.GraphCanvasAddChildren(viewerNode.FrameworkElements);

                // remember the created KosmographViewerNode.
                this.drawingObjectsToIViewerObjects[drawingNode] = viewerNode;

                return viewerNode;
            }
        }

        private TextBlock CreateAndRegisterFrameworkElementOfDrawingNode(DrawingNode node)
        {
            return this.drawingObjectsToFrameworkElements[node] = CreateTextBlockFromDrawingObjectLabel(node.Label);
        }

        #region Update a node

        public void UpdateNode(EntityViewModel node)
        {
            var drawingNode = this.Graph.FindNode(node.Model.Id.ToString());
            if (drawingNode is null)
                return;

            // update the underlying label
            drawingNode.Label.Text = node.Name;

            // remasure the node
            var tb = new TextBlock { Text = node.Name };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            drawingNode.GeometryNode.BoundingBox = new GeometryRectangle(0, 0, tb.DesiredSize.Width, tb.DesiredSize.Height)
            {
                Center = drawingNode.GeometryNode.BoundingBox.Center
            };
        }

        #endregion Update a node

        #region Add Node

        public void CreateNode(EntityViewModel node)
        {
            var drawingNode = this.Graph.AddNode(node.Model.Id.ToString());
            drawingNode.Attr.LineWidth = 1;
            drawingNode.Attr.XRadius = 0;
            drawingNode.Attr.YRadius = 0;
            drawingNode.Label.Text = node.Name;

            var viewerNode = this.CreateIViewerNode(drawingNode);
            this.RunLayoutInUIThread();
        }

        #endregion Add Node

        #region Remove a Node

        public void RemoveNode(EntityViewModel node)
        {
            var drawingNode = this.Graph.FindNode(node.Model.Id.ToString());
            if (drawingNode is null)
                return;

            // remove the node
            if (this.drawingObjectsToIViewerObjects.TryGetValue(drawingNode, out var viewerNode))
                this.GraphCanvasRemoveChildren(((KosmographViewerNode)viewerNode).FrameworkElements);
            this.drawingObjectsToIViewerObjects.Remove(drawingNode);
            this.drawingObjectsToFrameworkElements.Remove(drawingNode);

            // removes the nodes edges
            foreach (var drawingEdge in drawingNode.Edges)
            {
                if (this.drawingObjectsToIViewerObjects.TryGetValue(drawingEdge, out var viewerEdge))
                {
                    this.GraphCanvasRemoveChildren(((KosmographViewerEdge)viewerEdge).FrameworkElements);
                    this.drawingObjectsToIViewerObjects.Remove(drawingEdge);
                    this.drawingObjectsToFrameworkElements.Remove(drawingEdge);
                }
            }

            // remove the node from te underlying MSAGL graph.
            // this will incliude the edges
            this.Graph.RemoveNode(drawingNode);
        }

        #endregion Remove a Node
    }
}