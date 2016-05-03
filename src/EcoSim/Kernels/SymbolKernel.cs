using OpenCLWrapper;

namespace EcoSim.Kernels
{
    abstract class SymbolKernel : CLSourceBase
    {
        #region SYMBOL START

        public const int CELL_SIZE = 40;
        internal const int WORLD_START = 50;
        internal const int WORLD_CUT_OFF = 50;
        internal const int RAND_OFFSET = 40;

        internal const int EMPTY = 0;
        internal const int BORDER = 1;
        internal const int PLANT = 2;
        internal const int BACTERIUM = 3;

        internal const int CENTER = 1;
        internal const int LEFT = 0;
        internal const int RIGHT = 2;
        internal const int UP = 0;
        internal const int DOWN = 2;

        internal const int NO_ACTION = 0;
        internal const int SPAWN = 1;
        internal const int MOVE = 2;
        internal const int EAT = 3;

        internal const int ENERGY_BIT_MASK = 2139357183;
        internal const int TYPE_BIT_MASK = 268435455;

        internal const int RESOLVED = 1;

        internal const float _PI_ = 3.141592653589793238462f;
        internal const float NEGATIVE_PI = -3.141592653589793238462f;
        internal const float TWOPI = 6.283185307179586476f;
        internal const float PIOVER4 = 0.7853982f;

        #endregion SYMBOL END
    }
}
