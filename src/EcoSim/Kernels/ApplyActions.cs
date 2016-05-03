namespace EcoSim.Kernels
{
    class ApplyActions : SymbolKernel
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
            int y = index/gridSize;
            int x = index - (y*gridSize);

            int action = actions[index];

            int hostAction = (action >> 8) & 15;

            // We are requesting an action, check if it was granted by the target cell
            if (hostAction != NO_ACTION)
            {
                int actionX = (action >> 6) & 3;
                int actionY = (action >> 4) & 3;

                int neighborX = x + (actionX - 1);
                int neighborY = y + (actionY - 1);

                int neighborCell = cells[neighborY*gridSize + neighborX];

                int resolved = (neighborCell >> 4) & 1;

                // The action was resolved here
                if (resolved == RESOLVED)
                {
                    int resolvedX = (neighborCell >> 2) & 3;
                    int resolvedY = neighborCell & 3;

                    // Action was granted
                    if (actionX == resolvedX && actionY == resolvedY)
                    {
                        if (hostAction == SPAWN)
                        {
                            int energy = (action >> 18) & 31;

                            // Erase the energy component
                            cell = cell & ENERGY_BIT_MASK;

                            cell = cell | (energy << 18);
                        }
                        else if (hostAction == MOVE)
                        {
                            // Erase the type component (cell is now empty)
                            cell = cell & TYPE_BIT_MASK;
                        }
                        else if (hostAction == EAT)
                        {
                            int energy = (cell >> 18) & 31;

                            energy = min(energy + 5, 31);

                            // Erase the energy component
                            cell = cell & ENERGY_BIT_MASK;

                            cell = cell | (energy << 18);
                        }
                    }
                }
            }

            int resolvedAction = (cell >> 4) & 1;

            // This cell resolved a host action
            if (resolvedAction == RESOLVED)
            {
                int hostResolveX = (cell >> 2) & 3;
                int hostResolveY = cell & 3;

                // Order of operation matters...
                int hostX = x - (hostResolveX - 1);
                int hostY = y - (hostResolveY - 1);

                int hostIndex = hostY * gridSize + hostX;

                int hostActions = actions[hostIndex];

                int hostActionId = (hostActions >> 8) & 15;

                if (hostActionId == SPAWN)
                {
                    int randIndex = index + randOffset;

                    int hostType = (hostActions >> 28);
                    int hostVariation = (hostActions >> 26) & 3;
                    int hostEnergy = (hostActions >> 18) & 31;

                    int rotation = (int)(random[randIndex - 2] * 7493.32f) % 8;
                    int idleTimer = (int)(random[randIndex + 2] * 2323934.432f) % 15;

                    cell = cell | (hostType << 28);
                    cell = cell | (hostVariation << 26);
                    cell = cell | (rotation << 23);
                    cell = cell | (hostEnergy << 18);
                    cell = cell | (idleTimer << 12);

                    // Start this cell at its parent's position
                    lerpedPosX[index] = lerpedPosX[hostIndex];
                    lerpedPosY[index] = lerpedPosY[hostIndex];
                    lerpedRotations[index] = rotation * PIOVER4;
                }
                else if (hostActionId == MOVE)
                {
                    int hostType = hostActions >> 28;
                    int hostRotation = (hostActions >> 23) & 7;
                    int hostVariation = (hostActions >> 26) & 3;
                    int hostEnergy = (hostActions >> 18) & 31;

                    // Copy over basic attributes
                    cell = cell | hostType << 28;
                    cell = cell | (hostVariation << 26);
                    cell = cell | (hostRotation << 23);
                    cell = cell | (hostEnergy << 18);

                    // Copy lerped values
                    lerpedScales[index] = lerpedScales[hostIndex];
                    lerpedRotations[index] = lerpedRotations[hostIndex];
                    lerpedPosX[index] = lerpedPosX[hostIndex];
                    lerpedPosY[index] = lerpedPosY[hostIndex];
                }
                else if (hostActionId == EAT)
                {
                    int energy = (cell >> 18) & 31;

                    energy = max(energy - 6, 0);

                    // Erase the energy component
                    cell = cell & 2139357183;

                    cell = cell | (energy << 18);
                }
            }

            cells[index] = cell;
        }
    }
}