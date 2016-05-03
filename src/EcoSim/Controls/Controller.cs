using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;

namespace EcoSim.Controls
{
    class Controller
    {
        private KeyboardState _keyboardState;

        private KeyboardManager _keyboardManager;

        public Controller(Game game)
        {
            _keyboardManager = new KeyboardManager(game);

            _keyboardState = _keyboardManager.GetState();
        }

        public bool IsKeyPressed(Keys k)
        {
            return _keyboardState.IsKeyPressed(k);
        }

        public bool IsKeyReleased(Keys k)
        {
            return _keyboardState.IsKeyReleased(k);
        }

        public bool IsKeyDown(Keys k)
        {
            return _keyboardState.IsKeyDown(k);
        }

        public void Update()
        {
            _keyboardState = _keyboardManager.GetState();
        }
    }
}
