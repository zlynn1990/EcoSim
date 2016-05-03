namespace EcoSim.Kernels
{
    class ProcessActions : SymbolKernel
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

            // Get 2d coords
            int y = index / gridSize;
            int x = index - (y * gridSize);

            // Iterate over all 8 neighboring cells
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    // Skip the current cell...
                    if (dx == 0 && dy ==0) continue;

                    int neighborX = x + dx;
                    int neighborY = y + dy;

                    int neighborCell = actions[neighborY * gridSize + neighborX];

                    int neighborAction = (neighborCell >> 8) & 15;

                    // No target action, skip to next cell
                    if (neighborAction == NO_ACTION) continue;

                    int targetX = (neighborCell >> 6) & 3;
                    int targetY = (neighborCell >> 4) & 3;

                    // Check if the target cell is making an action in this direction
                    if (dx + (targetX - 1) == 0 && dy + (targetY - 1) == 0)
                    {
                        cell = cell | (RESOLVED << 4);
                        cell = cell | (targetX << 2);
                        cell = cell | targetY;

                        cells[index] = cell;

                        // Grant action and exit early
                        return;
                    }
                }
            }
        }
    }
}