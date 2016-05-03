using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Content;
using SharpDX.Toolkit.Graphics;
using SharpDX.Toolkit.Input;
using EcoSim.Controls;

namespace EcoSim.Rendering
{
    class Camera
    {
        public RectangleF Bounds { get; private set; }
        public Matrix Transform { get; private set; }

        private Vector2 _position;
        private Vector2 _velocity;
        private Vector2 _screenCenter;

        private float _zoom, _targetZoom;
        private float _lerpFactor;

        public Camera(Vector2 position, float zoom, float lerpFactor, GraphicsDeviceManager deviceManager)
        {
            Bounds = Rectangle.Empty;
            Transform = Matrix.Identity;

            _position = position;
            _velocity = Vector2.Zero;
            _zoom = _targetZoom = zoom;

            _lerpFactor = lerpFactor;
            _screenCenter = new Vector2(deviceManager.PreferredBackBufferWidth * 0.5f,
                                        deviceManager.PreferredBackBufferHeight * 0.5f);
        }

        public void Update(GameTime gameTime, Controller controller)
        {
            if (controller.IsKeyDown(Keys.A) && _targetZoom > 0.1f)
            {
                _targetZoom -= 0.02f;
            }
            else if (controller.IsKeyDown(Keys.D) && _targetZoom < 2.0f)
            {
                _targetZoom += 0.02f;
            }

            _zoom = MathUtil.Lerp(_zoom, _targetZoom, _lerpFactor);

            var targetVelocity = Vector2.Zero;

            if (controller.IsKeyDown(Keys.Left)) { targetVelocity.X = -500f; }
            if (controller.IsKeyDown(Keys.Right)) { targetVelocity.X = 500f; }
            if (controller.IsKeyDown(Keys.Up)) { targetVelocity.Y = -500f; }
            if (controller.IsKeyDown(Keys.Down)) { targetVelocity.Y = 500f; }

            if (targetVelocity.Length() > 0)
            {
                targetVelocity /= _zoom;
            }

            _velocity = Vector2.Lerp(_velocity, targetVelocity, _lerpFactor);

            _position += _velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Scale the screen parameters by the zoom
            Vector2 scaledScreenCenter = _screenCenter / _zoom;
            Vector2 scaledViewport = scaledScreenCenter * 2.0f;

            Bounds = new RectangleF(_position.X - scaledScreenCenter.X, _position.Y - scaledScreenCenter.Y,
                                   scaledViewport.X, scaledViewport.Y);

            Transform = Matrix.Translation(new Vector3(-_position, 0)) *
                        Matrix.Scaling(_zoom) *
                        Matrix.Translation(new Vector3(_screenCenter, 0));
        }
    }
}
