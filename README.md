# Unity VR Calculator

This is a follow-up to the original calculator assignment, now adapted for VR.

The UI has been rebuilt using Unity’s Canvas system, and everything’s been reworked to support interaction in 3D space.

## What’s New
- Switched from UI Toolkit to Unity Canvas UI for VR support  
- Added hover and click sounds for all buttons  
- Designed for VR interaction using the default controller input  

## Controls (Meta Quest / Any OpenXR Headset)
**Right trigger → Interact with the Calculator**  
- Click the buttons to select  
- Click and drag up/down to scroll through the history  

Tested on Meta Quest 3, but it should run on any VR headset that supports Unity OpenXR.

## How to Run

1. **Unity version**  
   Built on Unity 6000.0.51f1.

2. **Scene**  
   Open `Assets/Scenes/CalculatorScene.unity`.

3. **Build**  
   A signed APK for Android-based VR headsets is included:  
   [Download the APK](Builds/VRCalculator.apk)  

Make sure to sideload the APK to your headset using SideQuest or similar tools.
