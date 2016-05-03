using System;
using System.Linq;
using System.Collections.Generic;
using SharpDX.Toolkit;

namespace EcoSim.Rendering
{
    class FpsCounter
    {
        public int Current { get; private set; }

        private Queue<float> _sampleQueue;

        public FpsCounter()
        {
            _sampleQueue = new Queue<float>();
        }

        public void Update(GameTime gameTime)
        {
            if (_sampleQueue.Count > 19)
            {
                _sampleQueue.Dequeue();
            }

            _sampleQueue.Enqueue((float)gameTime.ElapsedGameTime.TotalSeconds);

            float fpsAvg = _sampleQueue.Average();

            Current = (int)Math.Round((1.0 / fpsAvg));
        }
    }
}
