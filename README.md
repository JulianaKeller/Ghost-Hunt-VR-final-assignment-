# Ghost Hunt VR

A Unity-based VR ghost hunting experience built for Oculus-compatible XR devices and Unity 2022.3 LTS.

Players explore a haunted VR environment, use tools like a vacuum gun and flashlight, and capture ghosts while a networked game session tracks score and time.

## Why this project is useful

- Demonstrates a VR gameplay loop with object interaction, ghost behavior, and UI feedback.
- Uses Unity XR Interaction Toolkit and Oculus XR Plugin for headset input and controller support.
- Includes multiplayer-ready networking with Unity Netcode for GameObjects.
- Built on Universal Render Pipeline (URP) for modern VR rendering.

## Key features

- Ghost vacuum capture mechanics with charge, visuals, and feedback
- Paralyzing and capturing ghosts using assignment-specific gameplay scripts
- Time and ghost count UI with networked game state synchronization
- Built-in Oculus XR support and XR management configuration
- Scene and input setup ready for Unity Editor playtesting

## Getting started

### Requirements

- Unity Editor 2022.3.50f1
- Unity Hub with the 2022.3 LTS toolchain installed
- Oculus XR support installed in Unity and configured for your headset
- A compatible PC or headset platform for Unity XR playback

### Open the project

1. Open Unity Hub.
2. Add the repository folder at the workspace root.
3. Open the project in Unity Editor.
4. Let Unity restore packages from `Packages/manifest.json`.

### Run the main scene

1. In Unity, open `Assets/VR Lab Class - Assignment 5/Scenes/VR Lab Class - Assignment 5.unity`.
2. Connect a supported VR headset.
3. Enter Play mode to test the scene.

### Notes

- The project uses the `Assets/VR Lab Class - Assignment 5/` folder for assignment-specific gameplay and scene assets.
- `Assets/VRSYS/` contains VR system tooling and demo support.
- `Packages/manifest.json` lists the required Unity packages, including XR, Netcode, TextMesh Pro, and URP.

## Project structure

- `Assets/VR Lab Class - Assignment 5/Scenes/VR Lab Class - Assignment 5.unity` - main playable scene
- `Assets/VR Lab Class - Assignment 5/Scripts/Assignment 5/` - core ghost hunt gameplay scripts
- `Assets/VRSYS/` - third-party VR system framework
- `Packages/manifest.json` - Unity package dependencies
- `ProjectSettings/` - project configuration and XR settings

## Help and support

- Use Unity Editor Console logs for runtime issues.
- Inspect `Packages/manifest.json` for package versions and dependencies.
- For package-specific support, consult Unity package documentation.

## Contributing

- This repository does not include a separate `CONTRIBUTING.md` file.
- To contribute, open an issue or submit a pull request with clear details and relevant Unity scene/script changes.
- Keep changes focused on VR gameplay, XR compatibility, and network behavior.
