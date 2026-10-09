<div class="image-container">
<img width="416" height="360" alt="U17CubemapGenerator Preview" src="https://github.com/ushizawa-jushichi/U17CubemapGenerator/blob/resources/screenshot_093_1.png" />
<img width="416" height="360" alt="U17CubemapGenerator Preview" src="https://github.com/ushizawa-jushichi/U17CubemapGenerator/blob/resources/screenshot_093_2.png" />
</div>

# U17CubemapGenerator

[English](README.md) | [日本語](README_ja.md)

Unityエディタ上でキューブマップを**リアルタイムにプレビューしながら作成・加工・エクスポートできる**ツールです。シーンのキャプチャから6面画像の統合、PBR向けIBLブラー処理、パノラマや八面体（Octahedral）、Matcapへの展開に対応しています。Universal Render Pipeline (URP) および High Definition Render Pipeline (HDRP) に対応しています。

---

## 主な特徴

### 1. シーンビューの視点からキャプチャ (URP / HDRP 対応)
- **Sync SceneView Camera**: シーンビューの視点移動と連動し、見ている風景を360度キューブマップとしてベイクできます。
- シーン内の指定カメラからのレンダリング、カメラ回転追従（`Rotatable`）、HDRキャプチャに対応。

### 2. ドラッグ＆ドロップ対応
- 6面画像や既存のCubemapアセットをウィンドウにドラッグ＆ドロップして即座に読み込みます。
- ファイル名のサフィックス（`_px`, `_left`, `_up` など）から配置面（+X, -X, +Y, -Y, +Z, -Z）を自動判別します。

### 3. 直感的な3Dプレビュー
- 視点やオブジェクトをドラッグ操作で回転させ、映り込みや継ぎ目を確認できます。
- **3種類のプレビュー形状**: Skybox / Sphere / Cube
- 軸ごとの回転制限、回転リセット、背景Skybox切替、Super Sampling機能を搭載。

### 4. ブラー / IBL処理
- **GGX Specular IBL**（PBRマテリアル向け）および **Gaussian / Bokeh ブラー** を搭載。
- スライダー操作で粗さやサンプル数を調整し、リアルタイムプレビューで確認できます。

### 5. 画像の反転補正 (Face Flip)
- 各面の **V (上下反転)** と **H (左右反転)** ボタンで、テクスチャの向きをエディタ上で補正できます。

### 6. 多彩な出力レイアウト
- 用途に合わせて以下の形式へ変換・出力できます：
  - **単一アセット (`.cubemap`)**
  - **正距円筒図法パノラマ (Equirectangular)** (2:1、水平回転オフセット調整対応)
  - **八面体マップ (Octahedral)** (1:1)
  - **クロス展開 (Cross Horizontal / Vertical)**
  - **一列並び展開 (Straight Horizontal / Vertical)**
  - **6面個別画像 (Six-Sided)** (`_XPlus`, `_XMinus` ...)
  - **Matcap** (外縁部の色補正機能付き)

### 7. インポート設定の自動割り当てとGUID維持
- エクスポートしたテクスチャには最適な `TextureImporter` 設定が自動適用されます。
- `.cubemap` ファイルを上書き保存する際もGUIDを保持するため、既存のマテリアル参照が外れません。

---

## 動作環境

- **Unity**: 2023.1 以降（Unity 6 対応）
- **レンダーパイプライン**: Universal Render Pipeline (URP) / High Definition Render Pipeline (HDRP)
  - ※シーンカメラからのキャプチャ機能に URP / HDRP を使用します。テクスチャやCubemapのプレビュー・変換・エクスポートは他パイプラインでも動作します。

---

## インストール方法 (UPM)

Unity Package Manager を開き、**Add package from git URL...** に以下を入力してインストールしてください：

```
https://github.com/ushizawa-jushichi/U17CubemapGenerator.git
```

---

## 使い方（クイックスタート）

1. メニューからウィンドウを開きます: `Tools > U17CubemapGenerator`。
2. **Generator タブ**:
   - **Input Mode**（`Current Scene` / `Six-Sided` / `Cubemap`）を選択。
   - 必要に応じて **Face Flip** や **Blur** を設定。
   - **Output Resolution** と **Output Layout** を指定し、**Export** を実行。
3. **Preview タブ**:
   - **Preview Object**（Skybox / Sphere / Cube）を選択し、ドラッグ操作で回転させて確認。

---

## ライセンス

[MIT License](LICENSE)

## サンプルアセットのクレジット

- [Bricks075A (ambientCG)](https://ambientcg.com/view?id=Bricks075A)
- [ChristmasTreeOrnamentSubstance004 (ambientCG)](https://ambientcg.com/view?id=ChristmasTreeOrnamentSubstance004)
- [Rural Landscape (Poly Haven)](https://polyhaven.com/a/rural_landscape)
