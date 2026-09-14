# Recent Changes

## 1.0.2.2 (Ultimate Compatibility Fix)

### Changes
- **Lazy UI Initialization**: Implemented a "on-demand" instantiation strategy for the Hopper UI. The UI prefab is no longer loaded or built during game startup or in the main menu. Instead, it is instantiated the very first time a player interacts with a Hopper in the game world.
- **Hardened Mod Compatibility**: By delaying UI creation until the game world is active, we ensure that other major mods (Jewelcrafting, AzuEPI, etc.) have their internal states fully initialized before they encounter our custom `InventoryGrid`. This resolves persistent `NullReferenceException` crashes during loading and in the main menu.
- **Improved Stability**: Removed all scene-name based logic for UI initialization, making the mod more robust against different game versions and custom scene names.

## 1.0.2.1 (AzuEPI Stability)

### Changes
- **Enhanced AzuEPI Compatibility**: Applied "blanket" layout group coverage to the custom filter grid and its roots.
- **Lazy Discovery Prevention**: The custom `InventoryGrid` is now disabled by default and only enabled during active interaction, preventing other mods' global scanners from interacting with it when it's not ready.

## 1.0.2 (Filtering & Compatibility)

### Changes
- **Dedicated Filter Inventory**: Replaced the "shadow item" filter system with a dedicated 4-slot inventory in the Hopper UI. This separates filtering rules from item storage.
- **Global Whitelist/Blacklist**: Refactored filter logic to check against the dedicated filter slots rather than the main inventory, preventing "auto-learning" conflicts.
- **Unity UI Transition**: Migrated the Hopper UI layout to Unity-authored prefabs, improving visual placement and reducing complex runtime UI code.
- **Mod Compatibility Fixes**:
    - **Jewelcrafting**: Delayed UI initialization until after the Main Menu to prevent Jewelcrafting's inventory patches from crashing the game.
    - **AzuEPI**: Added a runtime `GridLayoutGroup` to custom grids to resolve `NullReferenceException` reported by AzuEPI during its UI refresh cycle.
    - **General Stability**: Added safety checks to ensure the UI stays hidden unless a hopper is actively being interacted with.

### Reasons
- **Logic Conflict**: The previous "shadow" system in the main inventory made Blacklist mode impossible to use once items started flowing through. A separate inventory allows for permanent filter definitions.
- **UI Flexibility**: Manual layout in Unity is much more robust than programmatic layout when dealing with third-party mod overlays (like equipment slots or vanity panels).
- **Deep Compatibility**: Many popular Valheim mods (AzuEPI, Jewelcrafting) assume specific UI hierarchies. These changes ensure ValheimPipes plays nicely with standard modded loadouts.


## 1.0.1.1 (MultiUserChest Refactor)

### Changes
- **Core Interface Refactor**: Updated `IPushTarget` and `IPullTarget` to pass `Container` references instead of `Inventory`.
- **Hopper & Pipe Logic**: Refactored `Hopper.cs` and `Pipe.cs` to pass their local `Container` component during all item transfers.
- **Target Migration**: Updated all 11+ target implementations (Containers, Smelters, Beehives, Cooking Stations, etc.) to utilize **MultiUserChest (MUC)** extension methods.
- **Dependency Enforcement**: Added `using MultiUserChest` and proper `Inventory` reference handling (e.g., `sourceContainer.GetInventory()`) across the logic layer.

### Reasons
- **Multiplayer Synchronization**: Standard Valheim `Inventory` manipulation is unsafe in multiplayer and often causes item duplication or loss. MUC provides atomic, RPC-backed transfers that ensure data consistency across all clients.
- **Network Safety**: MUC extension methods require the `Container` object to access its `ZNetView` for signaling. Passing the container explicitly ensures the library can handle ownership transfers and locks correctly.
- **Mod Stability**: Centralizing on MUC for both ends of the transport pipeline (Hopper <-> Target) eliminates the race conditions that previously plagued the bronze and iron pipe systems on dedicated servers.
