# EcoSim

EcoSim is a simple biological simulation ran at a massive scale. The simulation uses cellular automata to model creatures living in a 2D grid world. The model is comprised of plants and bacteria. Plants grow by harvesting sunlight and are able to reproduce into empty neighboring cells when they reach a specific energy level. Conversely, bacteria can move freely between empty cells in search of plants to eat. If bacteria find a steady source of plant food they can reproduce and flourish; if they do not find plant food within a certain amount of time they die. These simple axiomatic rules have the ability to produce complex ecosystems that are cyclical and self-balancing—it’s also a great way to demonstrate the power of the GPU for a certain class of computational problems.

## Installation and Usage

If you just want to run the simulation, download the latest build found under the builds directory. Unzip the build and launch EcoSim.exe to get started. Otherwise, you can clone this repository and browse the source code.

The simulation has two command line arguments: "-size X" and "-f". The size argument specifies the size of the world grid. The default value is 300, meaning a 300x300 grid. The second argument runs the simulation full screen, the default is windowed mode.

## Controls

| Button | Action |
| ------ | ----------- |
| Arrow Keys  | Move around. |
| A | Zooms out. |
| D | Zooms in. |
| , | Reduce simulation speed. |
| . | Increase simulation speed. |