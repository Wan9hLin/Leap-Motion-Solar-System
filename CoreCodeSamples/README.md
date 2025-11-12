# Core Code Samples

**Focus:**  
Showcases the essential scripts that power the gesture-based interaction framework of *Gesture Solar System*.  
These components demonstrate how hand-tracking data from the Ultraleap Motion Controller is mapped to camera control, object manipulation, and dynamic UI feedback, creating a seamless and immersive exploration experience.

**Key Scripts:**  
- **CameraController_2.cs** – Manages camera panning, rotation, and zooming based on hand gestures; supports smooth FOV transitions, target focusing, and idle auto-rotation when inactive.  
- **PlanetsManager_leap.cs** – Handles planet interactions including selection, zoom, and rotation; synchronizes camera transitions and updates focus targets based on gesture input.  
- **PlanetInfoManager.cs** – Controls the UI display of planetary data such as size, orbit, and surface details; dynamically updates scientific information and related imagery.  
- **SelfRotation.cs** – Governs continuous self-rotation of planets; supports directional control, rotation toggling, and smooth orientation reset through interpolation.  
- **InteractionInputModule_leap.cs** – Core gesture input handler; interprets grab and pinch gestures for interaction, zoom, and selection, and manages cursor feedback for precise user control.
