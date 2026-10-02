using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Triangulation;
using System;

namespace MGUI.Shared.Rendering
{
    /// <summary>
    /// MonoGame.Extended's <c>VectorDraw.PrimitiveDrawing</c> (v3.8, MIT), ported to the Zna
    /// <see cref="PrimitiveBatch"/>: points, segments, polygons, rectangles, circles and ellipses as vertex-coloured
    /// line and triangle lists. Solid shapes are triangulated with MonoGame.Extended's <see cref="Triangulator"/>, as
    /// in the original.
    /// </summary>
    public class PrimitiveDrawing
    {
        private readonly PrimitiveBatch _primitiveBatch;

        public const int CircleSegments = 32;

        public PrimitiveDrawing(PrimitiveBatch primitiveBatch)
        {
            _primitiveBatch = primitiveBatch;
        }

        private void CheckReady()
        {
            if (!_primitiveBatch.IsReady())
                throw new InvalidOperationException("BeginCustomDraw must be called before drawing anything.");
        }

        public void DrawPoint(Vector2 center, Color color)
        {
            CheckReady();

            //Add two points or the PrimitiveBatch acts up
            _primitiveBatch.AddVertex(center, color, PrimitiveType.LineList);
            _primitiveBatch.AddVertex(center, color, PrimitiveType.LineList);
        }

        public void DrawRectangle(Vector2 location, float width, float height, Color color)
        {
            CheckReady();
            DrawPolygon(location, RectangleVertices(width, height), color);
        }

        public void DrawSolidRectangle(Vector2 location, float width, float height, Color color)
        {
            CheckReady();
            DrawSolidPolygon(location, RectangleVertices(width, height), color);
        }

        private static Vector2[] RectangleVertices(float width, float height) => new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(width, 0),
            new Vector2(width, height),
            new Vector2(0, height)
        };

        public void DrawCircle(Vector2 center, float radius, Color color)
        {
            CheckReady();

            const double increment = Math.PI * 2.0 / CircleSegments;
            double theta = 0.0;

            for (int i = 0; i < CircleSegments; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
                Vector2 v2 = center + radius * new Vector2((float)Math.Cos(theta + increment), (float)Math.Sin(theta + increment));

                _primitiveBatch.AddVertex(v1, color, PrimitiveType.LineList);
                _primitiveBatch.AddVertex(v2, color, PrimitiveType.LineList);

                theta += increment;
            }
        }

        public void DrawSolidCircle(Vector2 center, float radius, Color color)
        {
            CheckReady();

            const double increment = Math.PI * 2.0 / CircleSegments;
            double theta = 0.0;

            Color colorFill = color * 0.5f;

            Vector2 v0 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
            theta += increment;

            for (int i = 1; i < CircleSegments - 1; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
                Vector2 v2 = center + radius * new Vector2((float)Math.Cos(theta + increment), (float)Math.Sin(theta + increment));

                _primitiveBatch.AddVertex(v0, colorFill, PrimitiveType.TriangleList);
                _primitiveBatch.AddVertex(v1, colorFill, PrimitiveType.TriangleList);
                _primitiveBatch.AddVertex(v2, colorFill, PrimitiveType.TriangleList);

                theta += increment;
            }

            DrawCircle(center, radius, color);
        }

        public void DrawSegment(Vector2 start, Vector2 end, Color color)
        {
            CheckReady();

            _primitiveBatch.AddVertex(start, color, PrimitiveType.LineList);
            _primitiveBatch.AddVertex(end, color, PrimitiveType.LineList);
        }

        public void DrawPolygon(Vector2 position, Vector2[] vertices, Color color, bool closed = true)
        {
            CheckReady();

            int count = vertices.Length;

            for (int i = 0; i < count - 1; i++)
            {
                _primitiveBatch.AddVertex(vertices[i] + position, color, PrimitiveType.LineList);
                _primitiveBatch.AddVertex(vertices[i + 1] + position, color, PrimitiveType.LineList);
            }
            if (closed)
            {
                _primitiveBatch.AddVertex(vertices[count - 1] + position, color, PrimitiveType.LineList);
                _primitiveBatch.AddVertex(vertices[0] + position, color, PrimitiveType.LineList);
            }
        }

        /// <param name="outline">If true, the fill is drawn at half opacity and outlined with <paramref name="color"/>.</param>
        public void DrawSolidPolygon(Vector2 position, Vector2[] vertices, Color color, bool outline = true)
        {
            CheckReady();

            if (vertices.Length == 2)
            {
                DrawPolygon(position, vertices, color);
                return;
            }

            AddTriangulated(position, vertices, color * (outline ? 0.5f : 1.0f));

            if (outline)
                DrawPolygon(position, vertices, color);
        }

        public void DrawEllipse(Vector2 center, Vector2 radius, int sides, Color color)
        {
            CheckReady();
            DrawPolygon(center, CreateEllipse(radius.X, radius.Y, sides), color);
        }

        /// <param name="outline">If true, the fill is drawn at half opacity (the original draws no outline here).</param>
        public void DrawSolidEllipse(Vector2 center, Vector2 radius, int sides, Color color, bool outline = true)
        {
            CheckReady();
            AddTriangulated(center, CreateEllipse(radius.X, radius.Y, sides), color * (outline ? 0.5f : 1.0f));
        }

        private void AddTriangulated(Vector2 position, Vector2[] vertices, Color color)
        {
            Triangulator.Triangulate(vertices, WindingOrder.CounterClockwise, out Vector2[] outVertices, out int[] outIndices);

            for (int i = 0; i < outIndices.Length - 2; i += 3)
            {
                _primitiveBatch.AddVertex(outVertices[outIndices[i]] + position, color, PrimitiveType.TriangleList);
                _primitiveBatch.AddVertex(outVertices[outIndices[i + 1]] + position, color, PrimitiveType.TriangleList);
                _primitiveBatch.AddVertex(outVertices[outIndices[i + 2]] + position, color, PrimitiveType.TriangleList);
            }
        }

        private static Vector2[] CreateEllipse(float rx, float ry, int sides)
        {
            var vertices = new Vector2[sides];

            var t = 0.0;
            var dt = 2.0 * Math.PI / sides;
            for (var i = 0; i < sides; i++, t += dt)
            {
                var x = (float)(rx * Math.Cos(t));
                var y = (float)(ry * Math.Sin(t));
                vertices[i] = new Vector2(x, y);
            }
            return vertices;
        }
    }
}
