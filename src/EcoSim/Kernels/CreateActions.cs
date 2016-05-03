namespace EcoSim.Kernels
{
    class CreateActions : SymbolKernel
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

            // Get 2d coords
            int y = index / gridSize;
            int x = index - (y*gridSize);

            // Parse the additonal components
            int idleTimer = ((cell >> 12) & 63);
            int rotation = (cell >> 23) & 7;

            // Clear all the dynamic cell data
            cell = (cell >> 26);
            cell = (cell << 26);

            int randIndex = index + randOffset;

            if (type == PLANT)
            {
                // Do an action!
                if (idleTimer > 23)
                {
                    if (random[randIndex + 4] < 0.2f) // Grow
                    {
                        float offset = random[randIndex - 2] * 0.1f + 0.85f;

                        energy = max((int)(energy * offset), 5);
                    }
                    else if (energy > 29) // Spawn
                    {
                        int xOffset = CENTER;
                        int yOffset = CENTER;

                        float randX = random[randIndex - 4];
                        float randY = random[randIndex + 6];

                        if (randX > 0.666666f)
                        {
                            xOffset = LEFT;
                        }
                        else if (randX < 0.3333333f)
                        {
                            xOffset = RIGHT;
                        }

                        if (randY > 0.666666f)
                        {
                            yOffset = UP;
                        }
                        else if (randY < 0.3333333f)
                        {
                            yOffset = DOWN;
                        }

                        int targetX = x + (xOffset - 1);
                        int targetY = y + (yOffset - 1);

                        int targetCell = cells[targetY*gridSize + targetX];
                        int targetType = targetCell >> 28;

                        // Only perform the spawn if the target cell is empty
                        if (targetType == EMPTY)
                        {
                            int variation = (cell >> 26) & 3;

                            // Write the action
                            int action = (SPAWN << 8);
                            action = action | (xOffset << 6);
                            action = action | (yOffset << 4);

                            // Write host attributes
                            action = action | (type << 28);
                            action = action | (variation << 26);
                            action = action | ((energy >> 1) << 18);

                            actions[index] = action;
                        }
                    }

                    energy = min(energy + 1, 31);

                    idleTimer = 0;
                }
            }
            else if (type == BACTERIUM)
            {
                // Do an action!
                if (idleTimer > (energy >> 1) + 15)
                {
                    // Find out the direction this action will act in
                    int xOffset = CENTER;
                    int yOffset = CENTER;

                    if (rotation == 0)
                    {
                        xOffset = RIGHT;
                        yOffset = CENTER;
                    }
                    else if (rotation == 1)
                    {
                        xOffset = RIGHT;
                        yOffset = DOWN;
                    }
                    else if (rotation == 2)
                    {
                        xOffset = CENTER;
                        yOffset = DOWN;
                    }
                    else if (rotation == 3)
                    {
                        xOffset = LEFT;
                        yOffset = DOWN;
                    }
                    else if (rotation == 4)
                    {
                        xOffset = LEFT;
                        yOffset = CENTER;
                    }
                    else if (rotation == 5)
                    {
                        xOffset = LEFT;
                        yOffset = UP;
                    }
                    else if (rotation == 6)
                    {
                        xOffset = CENTER;
                        yOffset = UP;
                    }
                    else if (rotation == 7)
                    {
                        xOffset = RIGHT;
                        yOffset = UP;
                    }

                    int targetX = x + (xOffset - 1);
                    int targetY = y + (yOffset - 1);

                    int targetCell = cells[targetY * gridSize + targetX];
                    int targetType = targetCell >> 28;

                    // Spawn
                    if (energy > 28 && random[randIndex + 3] > 0.85f)
                    {
                        float randX = random[randIndex - 4];
                        float randY = random[randIndex + 6];

                        if (randX > 0.666666f)
                        {
                            xOffset = LEFT;
                        }
                        else if (randX < 0.3333333f)
                        {
                            xOffset = RIGHT;
                        }

                        if (randY > 0.666666f)
                        {
                            yOffset = UP;
                        }
                        else if (randY < 0.3333333f)
                        {
                            yOffset = DOWN;
                        }

                        targetX = x + (xOffset - 1);
                        targetY = y + (yOffset - 1);

                        targetCell = cells[targetY * gridSize + targetX];
                        targetType = targetCell >> 28;

                        if (targetType == EMPTY)
                        {
                            int variation = (cell >> 26) & 3;

                            // Write the action
                            int action = (SPAWN << 8);
                            action = action | (xOffset << 6);
                            action = action | (yOffset << 4);

                            // Write host attributes
                            action = action | (type << 28);
                            action = action | (variation << 26);
                            action = action | ((energy >> 1) << 18);

                            actions[index] = action;
                        }
                    }
                    else if (targetType == PLANT && random[randIndex + 1] > 0.15f) // Eat
                    {
                        int action = (EAT << 8);
                        action = action | (xOffset << 6);
                        action = action | (yOffset << 4);

                        actions[index] = action;
                    }
                    else if (random[randIndex - 3] > 0.5f) // Move
                    {
                        if (targetType == EMPTY)
                        {
                            int variation = (cell >> 26) & 3;

                            // Write the action
                            int action = (MOVE << 8);
                            action = action | (xOffset << 6);
                            action = action | (yOffset << 4);

                            // Write host attributes
                            action = action | (type << 28);
                            action = action | (variation << 26);
                            action = action | (rotation << 23);
                            action = action | (energy << 18);

                            actions[index] = action;
                        }
                    }
                    else
                    {
                        if (random[randIndex + 5] > 0.5f)
                        {
                            rotation += ((int)(random[randIndex + 1] * 2030.0f) % 4);

                            if (rotation > 7) rotation = 0;
                        }
                        else
                        {
                            rotation -= ((int)(random[randIndex - 1] * 245430.0f) % 4);

                            if (rotation < 0) rotation = 7;
                        }
                    }

                    energy = max(energy - 1, 0);

                    idleTimer = 0;
                }
            }

            idleTimer++;

            // Update dynamic components
            cell = cell | (rotation << 23);
            cell = cell | (energy << 18);
            cell = cell | (idleTimer << 12);

            cells[index] = cell;
        }
    }
}
