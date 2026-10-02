using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;

namespace MGUI.Shared.Rendering
{
    /// <summary>
    /// The shape drawing MGUI uses from MonoGame.Extended's <c>ShapeExtensions</c> (v3.8, MIT), ported to the Fna
    /// <see cref="SpriteBatch"/>: filled rectangles, lines, polygons and circles as stretched and rotated sprites of a
    /// 1×1 white texture, with the original's maths.
    /// </summary>
    public static class SpriteBatchShapeExtensions
    {
        private static Texture2D _whitePixelTexture;

        private static Texture2D GetTexture(SpriteBatch spriteBatch)
        {
            if (_whitePixelTexture == null || _whitePixelTexture.IsDisposed || _whitePixelTexture.GraphicsDevice != spriteBatch.GraphicsDevice)
            {
                _whitePixelTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                _whitePixelTexture.SetData(new[] { Color.White });
                spriteBatch.Disposing += (sender, args) =>
                {
                    _whitePixelTexture?.Dispose();
                    _whitePixelTexture = null;
                };
            }

            return _whitePixelTexture;
        }

        /// <summary>Draws a closed polygon through <paramref name="points"/> (offset by <paramref name="offset"/>) with
        /// lines of <paramref name="thickness"/>.</summary>
        public static void DrawPolygon(this SpriteBatch spriteBatch, Vector2 offset, IReadOnlyList<Vector2> points, Color color, float thickness = 1f, float layerDepth = 0)
        {
            if (points.Count == 0)
                return;

            if (points.Count == 1)
            {
                DrawPoint(spriteBatch, points[0], color, (int)thickness);
                return;
            }

            var texture = GetTexture(spriteBatch);

            for (var i = 0; i < points.Count - 1; i++)
                DrawPolygonEdge(spriteBatch, texture, points[i] + offset, points[i + 1] + offset, color, thickness, layerDepth);

            DrawPolygonEdge(spriteBatch, texture, points[points.Count - 1] + offset, points[0] + offset, color, thickness, layerDepth);
        }

        private static void DrawPolygonEdge(SpriteBatch spriteBatch, Texture2D texture, Vector2 point1, Vector2 point2, Color color, float thickness, float layerDepth)
        {
            var length = Vector2.Distance(point1, point2);
            var angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            var scale = new Vector2(length, thickness);
            spriteBatch.Draw(texture, point1, null, color, angle, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
        }

        /// <summary>Draws a filled rectangle.</summary>
        public static void FillRectangle(this SpriteBatch spriteBatch, RectangleF rectangle, Color color, float layerDepth = 0)
        {
            FillRectangle(spriteBatch, rectangle.Position, rectangle.Size, color, layerDepth);
        }

        /// <summary>Draws a filled rectangle at <paramref name="location"/>.</summary>
        public static void FillRectangle(this SpriteBatch spriteBatch, Vector2 location, Size2 size, Color color, float layerDepth = 0)
        {
            spriteBatch.Draw(GetTexture(spriteBatch), location, null, color, 0, Vector2.Zero, size, SpriteEffects.None, layerDepth);
        }

        /// <summary>Draws a line from <paramref name="point1"/> to <paramref name="point2"/>, centred on it across its
        /// <paramref name="thickness"/>.</summary>
        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color, float thickness = 1f, float layerDepth = 0)
        {
            var distance = Vector2.Distance(point1, point2);
            var angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            DrawLine(spriteBatch, point1, distance, angle, color, thickness, layerDepth);
        }

        /// <summary>Draws a line of <paramref name="length"/> from <paramref name="point"/> at <paramref name="angle"/>.</summary>
        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 point, float length, float angle, Color color, float thickness = 1f, float layerDepth = 0)
        {
            var origin = new Vector2(0f, 0.5f);
            var scale = new Vector2(length, thickness);
            spriteBatch.Draw(GetTexture(spriteBatch), point, null, color, angle, origin, scale, SpriteEffects.None, layerDepth);
        }

        /// <summary>Draws a square point of <paramref name="size"/> centred on <paramref name="position"/>.</summary>
        public static void DrawPoint(this SpriteBatch spriteBatch, Vector2 position, Color color, float size = 1f, float layerDepth = 0)
        {
            var scale = Vector2.One * size;
            var offset = new Vector2(0.5f) - new Vector2(size * 0.5f);
            spriteBatch.Draw(GetTexture(spriteBatch), position + offset, null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
        }

        /// <summary>Draws a circle outline as a polygon of <paramref name="sides"/>.</summary>
        public static void DrawCircle(this SpriteBatch spriteBatch, Vector2 center, float radius, int sides, Color color, float thickness = 1f, float layerDepth = 0)
        {
            DrawPolygon(spriteBatch, center, CreateCircle(radius, sides), color, thickness, layerDepth);
        }

        private static Vector2[] CreateCircle(double radius, int sides)
        {
            const double max = 2.0 * Math.PI;
            var points = new Vector2[sides];
            var step = max / sides;
            var theta = 0.0;

            for (var i = 0; i < sides; i++)
            {
                points[i] = new Vector2((float)(radius * Math.Cos(theta)), (float)(radius * Math.Sin(theta)));
                theta += step;
            }

            return points;
        }
    }
}
