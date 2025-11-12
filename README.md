# Gesture Solar System

## Overview
**Gesture Solar System** is an interactive experience developed in **Unity** using the **Ultraleap Motion Controller 2**. It enables users to explore the solar system through natural, intuitive hand gestures — panning, rotating, zooming, and focusing on planets to reveal detailed scientific information. This project demonstrates seamless integration of **gesture recognition, camera control, and dynamic UI systems**, providing an educational yet immersive journey through space.

---

## Technical Highlights
- **Gesture-Based Control Framework** – Custom input layer mapping grab, pinch, and movement gestures to camera and object manipulation.  
- **Dynamic Camera System** – Smooth interpolation and FOV control for natural transitions between exploration and focus modes.  
- **Idle Camera State** – Automated slow rotation and zoom reset when the user is inactive to enhance immersion and clarity.

---

## How to Run
1. **Requirements**
   - Unity **2022.3 LTS** or later  
   - **Ultraleap Motion Controller 2** connected and configured  
   - Windows PC meeting Ultraleap hardware specifications  

2. **Setup**
   - Clone or download this repository.  
   - Open the project in Unity Hub.  
   - Ensure the Ultraleap SDK is correctly installed and recognized by Unity.  
   - Open the main scene located under `Assets/Scenes/Main(Landscape).unity`.  

3. **Execution**
   - Press **Play** in Unity.  
   - Use gestures to explore the solar system:  
     - **Left-Hand Grab + Move** – Pan the camera  
     - **Right-Hand Grab + Move** – Rotate the camera around the focal point  
     - **Two-Hand Grab/Release** – Zoom in or out  
     - **Right-Hand Pinch** – Enter Focused Mode to view planet details  
     - **Left-Hand Grab (Focused Mode)** – Rotate the selected planet  
     - **Long Right-Hand Pinch** – Exit Focused Mode or reset to default view  

---

## Core System Architecture
The project includes the complete Unity implementation, while the **CoreCodeSamples/** folder highlights key technical components for easier code review.  
Each submodule represents a major part of the gesture-driven interaction framework:

---

## Links
- 🌐 [Portfolio Page](https://www.henrywang.online/copy-of-chippy-noppo-vr-1) – Full project breakdown and gameplay demo video 
