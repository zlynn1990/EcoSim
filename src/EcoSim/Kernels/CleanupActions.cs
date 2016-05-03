namespace EcoSim.Kernels
{
    class CleanupActions : SymbolKernel
    {
        // -------- ------ CELL ----- --------
        // 01110000 00000000 00000000 00000000 type >> 28
        // 00001100 00000000 00000000 00000000 variation >> 26 & 3
        // 00000011 10000000 00000000 00000000 rotation >> 23 & 7
        // 00000000 01111100 00000000 00000000 energy >> 18 & 31
        // 00000000 00000011 11110000 00000000 idle timer >> 12 & 63
        // 00000000 00000000 00000000 00010000 resolved action >> 4 & 1
        // 00000000 00000000 00000000 00001100 resolved action x >> 2 & 3
        // 00000000 00000000 00000000 00100011 resolved action y & 3

        // -------- ---- ACTIONS ---- --------
        // 01110000 00000000 00000000 00000000 type >> 28
        // 00001100 00000000 00000000 00000000 variation >> 26 & 3
        // 00000011 10000000 00000000 00000000 rotation >> 23 & 7
        // 00000000 01111100 00000000 00000000 energy >> 18 & 31
        // 00000000 00000000 00001111 00000000 action id >> 8 & 15
        // 00000000 00000000 00000000 11000000 action x >> 6 & 3
        // 00000000 00000000 00000000 00110000 action y >> 4 & 3
        public void Run(int[] cells, int[] actions, float[] random, float[] lerpedScales, float[] lerpedRotations, float[] lerpedPosX, float[] lerpedPosY, int gridSize, int totalSize, int randOffset)
        {
            int index = get_global_id(0);

            // Don't update cells outside the dynamic world
            if (index < WORLD_START || index > totalSize - WORLD_CUT_OFF) return;

            // Get cell
            int cell = cells[index];
            int type = cell >> 28;

            if (type == BORDER) return;

            int energy = (cell >> 18) & 31;

            // Reset all actions
            actions[index] = 0;

            if (type == EMPTY)
            {
                // Make sure actions get reset
                cell = EMPTY;
            }
            else
            {
                // Creature is dead
                if (energy == 0)
                {
                    cell = EMPTY;
                }
                else
                {
                    int rotation = (cell >> 23) & 7;

                    // Get 2d coords
                    int y = index / gridSize;
                    int x = index - (y * gridSize);

                    // Lerp the drawing paramters
                    lerpedScales[index] = lerpedScales[index] + ((energy * 0.03225f * 0.78f + 0.25f) - lerpedScales[index]) * 0.1f;

                    // Do complex angle lerp
                    if (type == BACTERIUM)
                    {
                        float current = lerpedRotations[index];
                        float target = rotation * PIOVER4;

                        float d = current;

                        if (target < current)
                        {
                            float c = target + TWOPI;

                            d = c - current > current - target
                                    ? current + (target - current)*0.1f
                                    : current + (c - current)*0.1f;
                        }
                        else if (target > current)
                        {
                            float c = target - TWOPI;

                            d = target - current > current - c
                                    ? current + (c - current)*0.1f
                                    : current + (target - current)*0.1f;
                        }

                        if (d > _PI_)
                            d = (d - _PI_) + NEGATIVE_PI;
                        else if (d < NEGATIVE_PI)
                            d = _PI_ - (NEGATIVE_PI - d);

                        lerpedRotations[index] = d;
                    }


                    lerpedPosX[index] = lerpedPosX[index] + (x * CELL_SIZE - lerpedPosX[index]) * 0.1f;
                    lerpedPosY[index] = lerpedPosY[index] + (y * CELL_SIZE - lerpedPosY[index]) * 0.1f;

                    // Clear the resolved actions
                    cell = (cell >> 4);
                    cell = (cell << 4);
                }
            }

            cells[index] = cell;
        }
    }
}