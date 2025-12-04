# kepstructor
My plugins for Torque Constructor

**Note**: If you update a plugin, do delete the associated `.dso` file before re-opening Constructor.

## AutoDIF

Allows connection with PQ for automatic DIF conversion

- Activation: automatic
- Interior Folder (Scene Property): determines the interiors folder to place the DIF
  - Add or remove options via `Constructor/prefs.cs` -> `$pref::AutoDIF::folders<Number>`
- Export On Save (Plugin Pref): If the scene should be exported to PQ when hitting ctrl s
- Save On Connect (Plugin Pref): If the scene should be saved upon hitting "Connect Constructor" in PQ. Ensures DIF is up to date
- Build BSP (Plugin Pref): If BSP Tree should be built for raycasts (may slow or stall export)

## Autosave

Saves scenes automatically after a set period of time

- Activation: automatic
- Enabled (Plugin Pref): If autosave should happen
- Interval (Plugin Pref): Number of seconds to wait between saves
- Save Session (Plugin Pref): If scenes should be saved when closing Constructor and re-opened when opening Constructor
## MP Entity Assist

Re-orders entity IDs of selection to create a valid Moving Platform
- Activation: Select desired MP parts (Door_Elevator, markers, triggers), then click the icon