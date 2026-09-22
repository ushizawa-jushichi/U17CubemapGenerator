<div class="image-container">
<img width="416" height="360" alt="U17CubemapGenerator Preview" src="https://github.com/ushizawa-jushichi/U17CubemapGenerator/blob/resources/screenshot_093_1.png" />
<img width="416" height="360" alt="U17CubemapGenerator Preview" src="https://github.com/ushizawa-jushichi/U17CubemapGenerator/blob/resources/screenshot_093_2.png" />
</div>

# U17CubemapGenerator

[English](README.md) | [日本語](README_ja.md)

A versatile, high-performance Unity Editor extension for **creating, processing, previewing, and exporting Cubemaps in real time**. From baking 360° views directly from your scene to assembling six-sided face textures, applying PBR IBL blurs, and exporting into panoramic or Matcap layouts, U17CubemapGenerator streamlines all cubemap workflows into an intuitive GUI.

---

## Key Highlights & Features

### 1. Instant Capture from Scene View
- **Sync SceneView Camera**: Synchronizes in real time with your Scene view navigation, allowing you to bake the exact scene environment you are looking at into a 360° cubemap with a single click.
- Supports rendering from any selected scene Camera, with camera rotation tracking (`Rotatable`) and HDR capture.

### 2. Effortless Drag-and-Drop Setup
- Drag and drop six-sided face textures or existing Cubemap assets straight into the window to load and preview them immediately.
- Automatic face assignment based on file name suffixes (e.g. `_px`, `_left`, `_up`), eliminating tedious manual texture slot assignments.

### 3. Interactive Real-Time 3D Preview
- Freely rotate viewpoints and objects with mouse drag to inspect reflections, seams, and lighting in real time.
- **3 Preview Modes**:
  - **Skybox**: Full 360° environment dome
  - **Sphere**: Highly reflective standard sphere
  - **Cube**: 3D cube mesh
- Features per-axis rotation locks (X, Y, Z toggles), Reset Rotation button, Background Skybox toggle, and **Super Sampling** for crisp, anti-aliased viewport rendering.

### 4. High-Quality Blurs & IBL Reflections via Sliders
- Built-in **GGX Specular IBL** (simulating realistic PBR material roughness) and **Gaussian / Bokeh Blurs** (for soft background depth).
- Tweak roughness and sample counts with sliders and immediately see the results in the live preview.

### 5. Fix Inverted or Flipped Textures with One Click (Face Flip)
- If imported textures have inverted vertical or horizontal orientations, there is no need to open an external image editor.
- Use the dedicated **V (Vertical Flip)** and **H (Horizontal Flip)** buttons for each of the 6 faces to fix orientation issues directly in Unity.

### 6. Versatile Output Layouts for Any Workflow
- Export into your required format with a single click:
  - **Single Asset (`.cubemap`)**: Native Unity Cubemap asset
  - **Equirectangular Panorama**: 360° cylindrical panorama (2:1 aspect ratio) with horizontal rotation offset (`Equirectangular Y`)
  - **Cross Layouts (Cross Horizontal / Vertical)**: Standard horizontal (4x3) and vertical (3x4) cross maps
  - **Straight Layouts (Straight Horizontal / Vertical)**: 6-in-line horizontal or vertical strips
  - **Six-Sided Textures**: Individual face files (`_XPlus`, `_XMinus`, etc.)
  - **Matcap**: Spherical environment map for Matcap shaders, with outer margin color fill to eliminate border seam artifacts

### 7. Automatic Texture Import Configuration
- Textures exported into the project are automatically configured with appropriate `TextureImporter` settings (TextureShape, sRGB, Mipmaps).
- Cross, Straight, and Equirectangular formats can be immediately imported as functional cubemaps by enabling **Import as Cubemap**.

### 8. Non-Destructive Asset Overwrites (GUID Preservation)
- Overwriting an existing `.cubemap` asset preserves its Unity GUID, even if the resolution or format is changed.
- Existing materials, prefabs, and scene references will never break or require manual reassignment.

---

## Requirements
- **Unity**: 2023.1 or later (fully compatible with Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP 15.0.7+)
  - *Note: URP is required for baking directly from scene cameras. Previewing, processing, and exporting six-sided textures and existing Cubemap assets work across all pipelines.*

---

## Installation (UPM)

In the Unity Package Manager, select **Add package from git URL...** and enter:

```
https://github.com/ushizawa-jushichi/U17CubemapGenerator.git
```

---

## How to Use (Quick Start)

1. Open the tool window from the top menu: `Tools > U17CubemapGenerator`.
2. **Generator Tab**:
   - Select the **Input Mode**:
     - `Current Scene`: Bake from a camera. Check **Sync SceneView Camera** to mirror your Scene view navigation.
     - `Six-Sided`: Assign 6 face textures (supports drag-and-drop from Project/folder).
     - `Cubemap`: Assign an existing Cubemap asset.
   - (Optional) Adjust **Face Flip** (V / H) for each face to correct texture orientations.
   - (Optional) Check **Blur Enable** and configure algorithm (GGX / Bokeh), roughness, and samples.
   - Select the desired **Output Resolution** and **Output Layout**.
   - Choose your destination path (`Export File`) and click **Export**.
3. **Preview Tab**:
   - Choose the **Preview Object** (`Skybox`, `Sphere`, or `Cube`).
   - Drag directly in the preview viewport or use sliders to rotate. Restrict rotation via X/Y/Z checkboxes as needed.
   - Toggle **Super Sampling** for higher visual fidelity.
4. **Toolbar Controls**:
   - **Hide UI**: Hides controls for a clean, full-window preview.
   - **Hide Preview**: Pauses preview rendering to eliminate background GPU overhead.

---

## Detailed Specifications & Technical Details

<details>
<summary><b>Expand Detailed Specifications</b></summary>

### Heavy Workload & TDR Crash Protection
- **TDR (GPU Timeout) Protection**: For heavy blur processing (>= 1024 samples or high resolutions), work is progressively split into Y-axis block chunks. Per-frame compute time is controlled to yield execution safely and display progress without causing GPU driver timeouts or Editor crashes.

### Fully Asynchronous Export Pipeline
- **Non-Freezing UI Output**: Combines asynchronous GPU readbacks (AsyncGPUReadback) with asynchronous file I/O so that exporting high-resolution 4K/8K textures never freezes the Unity Editor UI. Fully supports task cancellation.

### Accurate Color Management & HDR Support
- **Automatic sRGB / Linear Handling**: Automatically detects source texture format specifications and preserves accurate color space management (sRGB / Linear) across pipeline steps.
- **HDR Auto-Encoding**: Automatically switches export file format to `.exr` (32-bit Float / RGBAHalf) for HDR sources to retain full high dynamic range.

### Real-Time GPU Dimension Validation
- Calculates final layout dimensions (e.g., 6x width for Straight format) and validates against your hardware's maximum GPU texture limits (`maxTextureSize` / `maxCubemapSize`) in real time.
- Displays a warning and disables the **Export** button if dimensions exceed hardware capabilities, preventing GPU errors or crashes.

### Drag-and-Drop Suffix Naming Rules
When face textures are dropped onto the window, face slots are automatically assigned by detecting filename suffixes:
- **+X (Right)**: `_xplus`, `_posx`, `_px`, `+x`, `_right`, `_rt`, `-right`, `-rt`, `-px`
- **-X (Left)**: `_xminus`, `_negx`, `_nx`, `-x`, `_left`, `_lf`, `-left`, `-lf`, `-nx`
- **+Y (Up)**: `_yplus`, `_posy`, `_py`, `+y`, `_up`, `_top`, `-up`, `-top`, `-py`
- **-Y (Down)**: `_yminus`, `_negy`, `_ny`, `-y`, `_down`, `_bottom`, `_dn`, `_btm`, `-down`, `-bottom`, `-ny`
- **+Z (Front)**: `_zplus`, `_posz`, `_pz`, `+z`, `_front`, `_forward`, `_ft`, `_fwd`, `-front`, `-ft`, `-pz`
- **-Z (Back)**: `_zminus`, `_negz`, `_nz`, `-z`, `_back`, `_backward`, `_bk`, `_bwd`, `-back`, `-bk`, `-nz`

### Smart Destination Folder Inference & Reference Protection (GUID Preservation)
- **Automatic Export Directory Inference**:
  1. Currently set export path
  2. Input texture or Cubemap asset folder
  3. Currently selected folder in the Project window
  4. Defaults to `Assets/` if none found
- **Reference Protection (GUID Preservation)**:
  Overwriting `.cubemap` files automatically preserves the Unity asset GUID—even when resolution or format changes require reinstantiating the asset instance. Existing material, prefab, and scene references will never break.

</details>

---

## License
[MIT License](LICENSE)

## Sample Assets Credit
- [Bricks075A (ambientCG)](https://ambientcg.com/view?id=Bricks075A)
- [ChristmasTreeOrnamentSubstance004 (ambientCG)](https://ambientcg.com/view?id=ChristmasTreeOrnamentSubstance004)
- [Rural Landscape (Poly Haven)](https://polyhaven.com/a/rural_landscape)
