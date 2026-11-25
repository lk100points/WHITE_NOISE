# WHITE NOISE — THE LAST WINTER

Unity 6.3 LTS project • Windows x64 target • first/third-person psychological thriller prototype

A compact, procedural vertical slice set in North Point, a fictional coastal town in Maine in December 2004. The project uses Unity built-in primitives and runtime-generated materials; it does not require paid assets, URP, HDRP, or third-party packages.

## Open and play

1. Install **Unity Hub** and a **Unity 6.3 LTS** Editor with Windows Build Support (IL2CPP is not required for this prototype).
2. In Unity Hub choose **Add → Add project from disk**.
3. Select this folder (`WHITE_NOISE_UNITY`).
4. Open `Assets/Scenes/Main.unity` and press **Play**.
5. The runtime bootstrap creates the world, player, UI, ambience, and interaction points automatically.

## Controls

- `W A S D` — move
- Mouse — look
- `Left Shift` — move faster
- `E` — interact
- `C` — switch first-person / offset third-person camera
- `Esc` — pause
- `1–4` — select a kit or a dialogue option when the relevant panel is open

Keys can be rebound from **Controls** on the title screen or Pause menu. The settings are saved locally.

## What is in this slice

- Five connected areas: cabin, forest/cliff, sawmill, gas station, and North Point town.
- A small cabin interior with stove, radio, kitchen, bed, gear cabinet, and cellar drawer.
- First-person and left-offset third-person cameras.
- Snowfall, cold fog, warm practical lights, and generated wind / stove ambience. No continuous music.
- A day clock, money, fuel, supplies, firewood, fish, provisions, and simple cooking.
- Sawmill work, fuel purchase, shop purchase, fishing, firewood gathering, and abstracted deer-provision gathering.
- Three conversation characters: Eli Mercer, Mara, and Deputy Reed.
- The first-night event, radio clue, cellar clue, town notice, a follow-up deputy conversation, and a six-day ending framework.
- Local save/load through Unity `PlayerPrefs`.
- A simple Windows x64 build menu: **WHITE NOISE → Build Windows x64**.

## First play route

1. Leave the title menu with **New Game**.
2. Walk to the cabin door and press `E` to enter.
3. Inspect the gear cabinet and choose a morning kit.
4. Go outside. Visit the forest for wood or provisions, fish at the cliff, work at the sawmill, and use the gas station for supplies/fuel.
5. Return to the cabin. Try the stove, radio, kitchen, and cellar drawer.
6. After the clock reaches evening, the first-night event interrupts the routine. Your response affects trust and the information available later.
7. Rest from the bed after 6 PM to move to the next day.

## Windows build

Open the project in Unity, then select **WHITE NOISE → Build Windows x64** from the top menu. The output is written to:

`Build/Windows/WHITE_NOISE.exe`

The build includes the Unity player files beside the executable. Zip the entire `Build/Windows` folder, not just the `.exe`.

### GitHub Actions build

A manual workflow is included at `.github/workflows/build-windows.yml`. After pushing the project to GitHub:

1. Add the Unity license as a repository secret named `UNITY_LICENSE` (the workflow uses GameCI).
2. Open **Actions → Build Windows x64 → Run workflow**.
3. Download the `WHITE_NOISE-Windows-x64` artifact from the completed run.

The workflow cannot build until a valid Unity license secret is configured.

## Technical notes

- Built with the built-in render pipeline and the legacy Input Manager for broad Unity 6.3 compatibility.
- No TAA, motion blur, bloom, or depth-of-field is enabled. The runtime enables 2× MSAA.
- Most environment geometry is generated from simple meshes and primitives at runtime. This keeps the source lightweight and editable, but the current pass is a greybox/art-direction prototype, not a finished PS2-quality asset set.
- The current interaction model is distance-based. It is intentionally simple and predictable for this slice.
- The source package has not been opened or compiled in Unity inside this environment because the Unity Editor is not installed here. Run the project once in Unity before treating it as a verified release build.

## Narrative and safety scope

The darker layer is a mystery / investigation thread about the protagonist's return to North Point. It uses dialogue, clues, a radio event, and police follow-up. This prototype intentionally does not implement realistic methods of harming people, body disposal, evidence concealment, or evasion of law enforcement. The tension comes from observation, consequences, and what the protagonist chooses to disclose.

## Project structure

- `Assets/Scripts/GameRoot.cs` — game state, interaction routing, dialogue, UI, save/load, time and day flow
- `Assets/Scripts/PlayerController.cs` — movement and camera modes
- `Assets/Scripts/WorldBuilder.cs` — procedural environment, locations, lighting, snow, NPC silhouettes
- `Assets/Scripts/WorldPoint.cs` — distance-based interaction anchors
- `Assets/Editor/WhiteNoiseBuild.cs` — Windows x64 build command
- `Assets/Scenes/Main.unity` — runtime-bootstrap scene
- `.github/workflows/build-windows.yml` — manual Windows build workflow
