# BOT-Light-Laser-Vision

A high-performance, tactically authentic bot perception mod for **SPTarkov 4.0** (Escape from Tarkov). 

Adds realistic bot reactions to weapon flashlights and visible/IR lasers, dynamic night vision overexposure, Bright Source Protection (BSP) tube shutdown, reverse triangulation, and multi-sensory cue accumulation—all designed with zero memory allocations and frame-budgeted linecasts.

---

## 🌟 Key Features

### 1. Flashlight Perception & Shadow Awareness
* **Naked-Eye Body Illumination**: Non-IR flashlights illuminating bots without NVGs trigger alert reactions even from behind, as bots detect their own cast shadows and surrounding visible light throw.
* **Direct Face Dazzle**: High-intensity light shone directly into bot eyes within 15 meters induces temporary ocular dazzle and realistic aim dispersion penalties.
* **Relative Brightness Gating**: Sunlight washes out distant beams during daytime outdoor raids, eliminating false alerts and saving CPU physics queries.

### 2. Night Vision Optics & Generation Scaling
* **Tiered Sight Distance**: Ambient night vision range scales realistically by device generation:
  * **Top-Tier Gen 3+ (GPNVG-18)**: 115m ambient range, 97° panoramic FOV.
  * **Military Gen 3 (PVS-14, PVS-31A)**: 90-95m ambient range, 40° FOV (PVS-14 preserves peripheral vision on open eye).
  * **Commercial Gen 2 (NVG-7)**: 65m ambient range, 40° FOV.
  * **Soviet Gen 1 (PNV-10T, N-15)**: 45m ambient range, 35° FOV.
* **Severe Weather Degradation**: Heavy rain, fog, or snow storms severely scatter light and cut ambient NVG sight distance to 15 meters.

### 3. Laser Tracking & Bright Source Protection
* **Lighthouse Beacon Override**: Active visible lights and infrared emitters stand out like lighthouses through night vision goggles, allowing detection up to 150 meters and overriding normal ambient distance caps.
* **Transverse Beam Crossing**: Bots with active NVGs spot laser beams crossing in front of their field of view (up to 100m for GPNVG-18, 75m for Soviet NVGs).
* **Direct Laser Tube Shutdown**: Concentrated infrared lasers striking night vision objective lenses trip Bright Source Protection circuits, causing an immediate 0.5s to 1.5s total vision blackout. Bots enter an anonymous high-alert state without receiving player coordinates.

### 4. Reverse Triangulation & Multi-Sensory Accumulation
* **No Hardcoded Movement Wheels**: Injects calculated coordinates directly into native EFT `BotsGroup.AddPointToSearch`, maintaining full compatibility with vanilla AI, SAIN, and BigBrain.
* **Distance-Scaled Depth Error**: Triangulation uncertainty increases at long range, creating elongated search ellipses. Bots close distance before pinpointing the source.
* **Dynamic Convergence**: As bots advance, active emissions continuously refine estimated coordinates; turning devices off freezes the search coordinate, rewarding player light discipline.
* **Multi-Sensory Confidence**: Additional sensory cues (gunfire, footsteps, repeated laser passes) rapidly collapse triangulation uncertainty.

---

## ⚡ Performance Architecture
* **State Gate**: Zero raycasts and zero math when player devices are switched off.
* **Single Laser Raycast**: Traces active lasers with one forward raycast per frame, shared across all bot queries.
* **Pre-Allocated Memory**: Zero heap allocations in update loops using pre-allocated buffers.
* **Frame Budgeting**: Caps physics linecasts to a maximum of two checks per frame.

---

## ⚙️ Configuration (F12 Menu)
All settings are fully customizable in-game via the BepInEx F12 menu:
* Toggle individual modules (flashlights, lasers, beacon overrides, weather scaling).
* Fine-tune distance multipliers, contrast thresholds, and depth error scales.
* Adjust blackout duration and dazzle spread penalties.

---

## 🛠️ Requirements & Compatibility
* **SPTarkov 4.0.0+**
* Fully compatible with **SAIN (Solarint's AI Modifications)** and **BigBrain**.
