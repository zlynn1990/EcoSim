using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Content;
using SharpDX.Toolkit.Graphics;
using SharpDX.Toolkit.Input;
using Cloo;
using OpenCLWrapper;
using EcoSim.Rendering;
using EcoSim.Kernels;
using EcoSim.Controls;

namespace EcoSim
{
    internal class EcoSimGame : Game
    {
        public bool HardwareAcceleration { get; private set; }

        public int GridSize { get; private set; }
        public int TotalCells { get; private set; }

        private GraphicsDeviceManager _deviceManager;
        private SpriteBatch _spriteBatch;
        private Controller _controller;

        private FpsCounter _fpsCounter;
        private Camera _camera;

        private SpriteSheet _creatureSheet;

        private int _simulationSpeed = 1;

        private OpenCLProxy _clProxy;
        private ComputeKernel _createKernel, _processKernel, _applyKernel, _cleanupKernel;

        // OpenCL actions for simulation
        private CreateActions _createActions;
        private ProcessActions _processActions;
        private ApplyActions _applyActions;
        private CleanupActions _cleanupActions;

        // Smoothed properties for drawing
        private float[] _lerpedScales, _lerpedRotations, _lerpedPosX, _lerpedPosY;

        private Random _random;

        private bool _isFullScreen;
        private SpriteFont _font;

        public EcoSimGame(bool hardwareAcceleration, int gridSize, bool fullScreen)
        {
            HardwareAcceleration = hardwareAcceleration;

            GridSize = gridSize;
            TotalCells = GridSize * GridSize;

            _isFullScreen = fullScreen;
            _deviceManager = new GraphicsDeviceManager(this);

            _random = new Random();
        }

        protected override void Initialize()
        {
            Window.Title = "EcoSim";
            Window.AllowUserResizing = false;
            Content.RootDirectory = "Content";

            if (_isFullScreen)
            {
                _deviceManager.PreferredBackBufferWidth = GraphicsDevice.Adapter.DesktopBounds.Width;
                _deviceManager.PreferredBackBufferHeight = GraphicsDevice.Adapter.DesktopBounds.Height;
            }
            else
            {
                _deviceManager.PreferredBackBufferWidth = GraphicsDevice.Adapter.DesktopBounds.Width - 150;
                _deviceManager.PreferredBackBufferHeight = GraphicsDevice.Adapter.DesktopBounds.Height - 150;
            }

            _deviceManager.IsFullScreen = _isFullScreen;
            _deviceManager.ApplyChanges();

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _controller = new Controller(this);

            _camera = new Camera(new Vector2(3000, 2000), 0.5f, 0.1f, _deviceManager);

            _fpsCounter = new FpsCounter();

            InitializeCL();

            base.Initialize();
        }

        private void InitializeCL()
        {
            if (Directory.Exists("Kernels"))
            {
                KernelManager.GenerateKernels("Kernels");
            }
            else
            {
                KernelManager.GenerateKernels("../../Kernels");
            }

            var cells = new int[TotalCells];
            var randBuffer = new float[TotalCells];

            _lerpedScales = new float[TotalCells];
            _lerpedRotations = new float[TotalCells];
            _lerpedPosX = new float[TotalCells];
            _lerpedPosY = new float[TotalCells];

            // Randomize the grid with creatures and initialize kernel properties
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    int index = y * GridSize + x;

                    cells[index] = 0;

                    randBuffer[index] = _random.NextFloat(0.0f, 1.0f);

                    _lerpedPosX[index] = x * SymbolKernel.CELL_SIZE;
                    _lerpedPosY[index] = y * SymbolKernel.CELL_SIZE;

                    // Border
                    if (y == 0 || y == GridSize - 1 || x == 0 || x == GridSize - 1)
                    {
                        cells[index] = SymbolKernel.BORDER << 28;

                        _lerpedScales[index] = 1.0f;
                    }
                    else
                    {
                        int type = 0;

                        if (randBuffer[index] > 0.8f)
                        {
                            type = SymbolKernel.PLANT;
                        }
                        else if (randBuffer[index] > 0.795f)
                        {
                            type = SymbolKernel.BACTERIUM;
                        }

                        if (type > SymbolKernel.BORDER)
                        {
                            int energy = (int)(randBuffer[index] * 2845.43f) % 25 + 6;
                            int rotation = (int)(randBuffer[index] * 7493.32f) % 8;
                            int variation = (int)(randBuffer[index] * 342423.45f) % 3;
                            int idleTimer = (int)(randBuffer[index] * 2323934.432f) % 15;

                            int cell = type << 28;
                            cell = cell | (variation << 26);
                            cell = cell | (rotation << 23);
                            cell = cell | (energy << 18);
                            cell = cell | (idleTimer << 12);

                            cells[index] = cell;
                        }
                    }
                }
            }

            _clProxy = new OpenCLProxy(!HardwareAcceleration);

            _clProxy.CreateIntBuffer("cells", cells, ComputeMemoryFlags.UseHostPointer);
            _clProxy.CreateIntBuffer("actions", TotalCells, ComputeMemoryFlags.UseHostPointer);
            _clProxy.CreateFloatBuffer("random", randBuffer, ComputeMemoryFlags.UseHostPointer);
            _clProxy.CreateFloatBuffer("lerpedScales", _lerpedScales, ComputeMemoryFlags.UseHostPointer);
            _clProxy.CreateFloatBuffer("lerpedRotations", _lerpedRotations, ComputeMemoryFlags.UseHostPointer);
            _clProxy.CreateFloatBuffer("lerpedPosX", _lerpedPosX, ComputeMemoryFlags.UseHostPointer);
            _clProxy.CreateFloatBuffer("lerpedPosY", _lerpedPosY, ComputeMemoryFlags.UseHostPointer);

            _clProxy.CreateIntArgument("gridSize", GridSize);
            _clProxy.CreateIntArgument("totalCells", TotalCells);
            _clProxy.CreateIntArgument("randOffset", 0);

            _createActions = new CreateActions();
            _processActions = new ProcessActions();
            _applyActions = new ApplyActions();
            _cleanupActions = new CleanupActions();

            _createKernel = _clProxy.CreateKernel(_createActions);
            _processKernel = _clProxy.CreateKernel(_processActions);
            _applyKernel = _clProxy.CreateKernel(_applyActions);
            _cleanupKernel = _clProxy.CreateKernel(_cleanupActions);
        }

        protected override void LoadContent()
        {
            _creatureSheet = new SpriteSheet(Content, "creature", 16);

            _font = Content.Load<SpriteFont>("Arial");

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            _fpsCounter.Update(gameTime);

            _controller.Update();
            _camera.Update(gameTime, _controller);

            if (_controller.IsKeyPressed(Keys.Escape))
            {
                Exit();
            }

            if (_controller.IsKeyPressed(Keys.OemPeriod) && _simulationSpeed < 32)
            {
                _simulationSpeed *= 2;
            }

            if (_controller.IsKeyPressed(Keys.OemComma) && _simulationSpeed > 1)
            {
                _simulationSpeed /= 2;
            }

            RunKernel(gameTime);

            base.Update(gameTime);
        }

        private void RunKernel(GameTime gameTime)
        {
            for (int iteration = 0; iteration < _simulationSpeed; iteration++)
            {
                if (_clProxy.HardwareAccelerationEnabled)
                {
                    _clProxy.UpdateIntArgument("randOffset", _random.Next(-SymbolKernel.RAND_OFFSET, SymbolKernel.RAND_OFFSET));

                    _clProxy.RunKernel(_createKernel, GridSize * GridSize);

                    _clProxy.UpdateIntArgument("randOffset", _random.Next(-SymbolKernel.RAND_OFFSET, SymbolKernel.RAND_OFFSET));

                    _clProxy.RunKernel(_processKernel, GridSize * GridSize);

                    _clProxy.UpdateIntArgument("randOffset", _random.Next(-SymbolKernel.RAND_OFFSET, SymbolKernel.RAND_OFFSET));

                    _clProxy.RunKernel(_applyKernel, GridSize * GridSize);

                    _clProxy.RunKernel(_cleanupKernel, GridSize * GridSize);
                }
                else
                {
                    int randOffset = _random.Next(-SymbolKernel.RAND_OFFSET, SymbolKernel.RAND_OFFSET);

                    Parallel.For(0, TotalCells, i =>
                    {
                        _createActions.Run(_clProxy.ReadIntBuffer("cells", TotalCells), _clProxy.ReadIntBuffer("actions", TotalCells),
                                          _clProxy.ReadFloatBuffer("random", TotalCells), _lerpedScales, _lerpedRotations, _lerpedPosX,
                                          _lerpedPosY, GridSize, TotalCells, randOffset);
                    });

                    _createActions.Finish();

                    randOffset = _random.Next(-SymbolKernel.RAND_OFFSET, SymbolKernel.RAND_OFFSET);

                    Parallel.For(0, TotalCells, i =>
                    {
                        _processActions.Run(_clProxy.ReadIntBuffer("cells", TotalCells), _clProxy.ReadIntBuffer("actions", TotalCells),
                                          _clProxy.ReadFloatBuffer("random", TotalCells), _lerpedScales, _lerpedRotations, _lerpedPosX,
                                          _lerpedPosY, GridSize, TotalCells, randOffset);
                    });

                    _processActions.Finish();

                    randOffset = _random.Next(-SymbolKernel.RAND_OFFSET, SymbolKernel.RAND_OFFSET);

                    Parallel.For(0, TotalCells, i =>
                    {
                        _applyActions.Run(_clProxy.ReadIntBuffer("cells", TotalCells), _clProxy.ReadIntBuffer("actions", TotalCells),
                                          _clProxy.ReadFloatBuffer("random", TotalCells), _lerpedScales, _lerpedRotations, _lerpedPosX,
                                          _lerpedPosY, GridSize, TotalCells, randOffset);
                    });

                    _applyActions.Finish();

                    Parallel.For(0, TotalCells, i =>
                    {
                        _cleanupActions.Run(_clProxy.ReadIntBuffer("cells", TotalCells), _clProxy.ReadIntBuffer("actions", TotalCells),
                                          _clProxy.ReadFloatBuffer("random", TotalCells), _lerpedScales, _lerpedRotations, _lerpedPosX,
                                          _lerpedPosY, GridSize, TotalCells, randOffset);
                    });

                    _cleanupActions.Finish();
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(SpriteSortMode.Deferred, this.GraphicsDevice.BlendStates.NonPremultiplied, null, null, null, null, _camera.Transform);

            // Read data from the GPU
            int[] cells = _clProxy.ReadIntBuffer("cells", TotalCells);

            _lerpedScales = _clProxy.ReadFloatBuffer("lerpedScales", TotalCells);
            _lerpedRotations = _clProxy.ReadFloatBuffer("lerpedRotations", TotalCells);
            _lerpedPosX = _clProxy.ReadFloatBuffer("lerpedPosX", TotalCells);
            _lerpedPosY = _clProxy.ReadFloatBuffer("lerpedPosY", TotalCells);

            RectangleF cameraBounds = _camera.Bounds;
            cameraBounds.Inflate(50f, 50.0f);

            // Figure out which creatures are on screen
            int horizontalStart = cameraBounds.Left > 0 ? (int)cameraBounds.Left / SymbolKernel.CELL_SIZE : 0;
            int horizontalEnd = cameraBounds.Right < GridSize * SymbolKernel.CELL_SIZE ? (int)cameraBounds.Right / SymbolKernel.CELL_SIZE : GridSize - 1;

            int verticalStart = cameraBounds.Top > 0 ? (int)cameraBounds.Top / SymbolKernel.CELL_SIZE : 0;
            int verticalEnd = cameraBounds.Bottom < GridSize * SymbolKernel.CELL_SIZE ? (int)cameraBounds.Bottom / SymbolKernel.CELL_SIZE : GridSize - 1;

            // Draw all visible creatures
            for (int x = horizontalStart; x < horizontalEnd; x++)
            {
                for (int y = verticalStart; y < verticalEnd; y++)
                {
                    int index = y * GridSize + x;

                    int cell = cells[index];

                    int type = cell >> 28;

                    if (type == SymbolKernel.EMPTY) continue;

                    var position = new Vector2(_lerpedPosX[index], _lerpedPosY[index]);

                    int variation = (cell >> 26) & 3;

                    if (type == SymbolKernel.PLANT)
                    {
                        _creatureSheet.Draw(_spriteBatch, variation, position, _lerpedRotations[index], _lerpedScales[index]);
                    }
                    else if (type == SymbolKernel.BACTERIUM)
                    {
                        _creatureSheet.Draw(_spriteBatch, 3 + variation, position, _lerpedRotations[index], _lerpedScales[index]);
                    }
                }
            }

            _spriteBatch.End();

            _spriteBatch.Begin();

            _spriteBatch.DrawString(_font, string.Format("FPS: {0}", _fpsCounter.Current), new Vector2(5, 5), Color.White);
            _spriteBatch.DrawString(_font, string.Format("Simulation Speed: {0}X", _simulationSpeed), new Vector2(5, _deviceManager.PreferredBackBufferHeight - 30), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
