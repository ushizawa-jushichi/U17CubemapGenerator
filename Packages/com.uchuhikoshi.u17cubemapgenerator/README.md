<div class="image-container">
<img width="416" height="360" alt="U17CubemapGenerator Preview" src="https://github.com/ushizawa-jushichi/U17CubemapGenerator/blob/resources/screenshot_093_1.png" />
<img width="416" height="360" alt="U17CubemapGenerator Preview" src="https://github.com/ushizawa-jushichi/U17CubemapGenerator/blob/resources/screenshot_093_2.png" />
</div>

# U17CubemapGenerator

[English](README.md) | [日本語](README_ja.md)

A Unity Editor extension for creating, processing, previewing, and exporting Cubemaps in real time. Bake 360° views from your scene, assemble six-sided face textures, apply PBR IBL blurs, and export to panoramic, octahedral, cross, straight, or Matcap layouts. Supports Universal Render Pipeline (URP) and High Definition Render Pipeline (HDRP).

---

## Features

### 1. Scene View Capture (URP & HDRP)
- **Sync SceneView Camera**: Synchronizes with your Scene view camera to capture 360° cubemaps.
- Supports rendering from a selected Scene Camera with rotation tracking, HDR capture, and pipeline settings.

### 2. Drag-and-Drop Setup
- Load six-sided face textures or existing Cubemap assets via drag-and-drop.
- Automatic face assignment (+X, -X, +Y, -Y, +Z, -Z) based on filename suffixes (e.g., `_px`, `_left`, `_up`).

### 3. Real-Time 3D Preview
- Inspect reflections, seams, and lighting with interactive camera controls.
- **Preview Modes**: Skybox, Sphere, Cube.
- Includes per-axis rotation locks, reset button, background toggle, and super sampling.

### 4. Blurs & IBL Processing
- **GGX Specular IBL** (PBR material roughness) and **Gaussian / Bokeh Blurs**.
- Adjust roughness and sample count with real-time preview updates.

### 5. Face Flip Correction
- Correct texture orientations directly with **V (Vertical Flip)** and **H (Horizontal Flip)** controls for each face.

### 6. Output Layouts
- Export to multiple formats:
  - **Single Asset (`.cubemap`)**
  - **Equirectangular Panorama (2:1)** with rotation offset
  - **Octahedral Projection (1:1)**
  - **Cross Layouts** (Horizontal / Vertical)
  - **Straight Layouts** (Horizontal / Vertical)
  - **Six-Sided Textures** (`_XPlus`, `_XMinus`, etc.)
  - **Matcap** (with outer margin color fill)

### 7. Import Settings & GUID Preservation
- Exported textures are automatically configured with appropriate `TextureImporter` settings.
- Overwriting existing `.cubemap` assets preserves Unity GUIDs to maintain existing references.

---

## Requirements

- **Unity**: 2023.1 or later (compatible with Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP) / High Definition Render Pipeline (HDRP)
  - *Note: URP or HDRP is required for scene camera baking. Previewing, processing, and exporting existing textures/cubemaps work across all pipelines.*

---

## Installation (UPM)

In the Unity Package Manager, select **Add package from git URL...** and enter:

```
https://github.com/ushizawa-jushichi/U17CubemapGenerator.git
```

---

## Quick Start

1. Open `Tools > U17CubemapGenerator`.
2. **Generator Tab**:
   - Select **Input Mode**: `Current Scene`, `Six-Sided`, or `Cubemap`.
   - Adjust face flips (V/H) or blurs if needed.
   - Select **Output Resolution** and **Output Layout**.
   - Set destination path and click **Export**.
3. **Preview Tab**:
   - Select **Preview Object** (`Skybox`, `Sphere`, or `Cube`).
   - Rotate using mouse drag or viewport sliders.

---

## License

[MIT License](LICENSE)

## Sample Assets Credit

- [Bricks075A (ambientCG)](https://ambientcg.com/view?id=Bricks075A)
- [ChristmasTreeOrnamentSubstance004 (ambientCG)](https://ambientcg.com/view?id=ChristmasTreeOrnamentSubstance004)
- [Rural Landscape (Poly Haven)](https://polyhaven.com/a/rural_landscape)
