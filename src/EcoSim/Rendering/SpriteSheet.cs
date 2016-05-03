using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Content;
using SharpDX.Toolkit.Graphics;

namespace EcoSim.Rendering
{
    class SpriteSheet
    {
        private Texture2D _texture;

        public Vector2 _origin;
        private Rectangle[] _sourceRectangles;

        public SpriteSheet(ContentManager content, string path, int frames)
        {
            _texture = content.Load<Texture2D>(path);

            int textureWidth = _texture.Width / frames;

            _origin = new Vector2(textureWidth / 2.0f, _texture.Height / 2.0f);
            _sourceRectangles = new Rectangle[frames];

            for (int i = 0; i < frames; i++)
            {
                _sourceRectangles[i] = new Rectangle(i * textureWidth, 0, textureWidth, _texture.Height);
            }
        }

        public void Draw(SpriteBatch spriteBatch, int textureId, Vector2 position, float rotation, float scale)
        {
            spriteBatch.Draw(_texture, position, _sourceRectangles[textureId], Color.White, rotation, _origin, scale, SpriteEffects.None, 0);
        }
    }
}
