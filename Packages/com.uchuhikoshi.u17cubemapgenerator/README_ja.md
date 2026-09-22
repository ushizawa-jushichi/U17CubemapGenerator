<div class="image-container">
<img width="416" height="360" alt="U17CubemapGenerator Preview" src="https://github.com/ushizawa-jushichi/U17CubemapGenerator/blob/resources/screenshot_093_1.png" />
<img width="416" height="360" alt="U17CubemapGenerator Preview" src="https://github.com/ushizawa-jushichi/U17CubemapGenerator/blob/resources/screenshot_093_2.png" />
</div>

# U17CubemapGenerator

[English](README.md) | [日本語](README_ja.md)

Unityエディタ上でキューブマップを**リアルタイムにプレビューしながら作成・加工・エクスポートできる**高機能ツールです。シーンのキャプチャから6面画像の統合、PBR向けIBLブラー処理、パノラマやMatcapへの展開まで、キューブマップに関するあらゆる作業を直感的なGUIで完結できます。

---

## 主な特徴（できること）

### 1. シーンビューの視点から一発キャプチャ
- **Sync SceneView Camera**: シーンビューの視点移動とリアルタイムに同期しながら、見ている風景そのままを360度キューブマップとして即座にベイクできます。
- 配置済みのシーンカメラを指定してレンダリングすることも可能。カメラの向きに追従させる回転機能（`Rotatable`）やHDRキャプチャにも対応しています。

### 2. ドラッグ＆ドロップで即座にプレビュー
- 6面画像（Skybox用テクスチャ素材）や既存のCubemapアセットをウィンドウにドラッグ＆ドロップするだけで即座に読み込みます。
- ファイル名のサフィックス（`_px`, `_left`, `_up` など）から配置面（+X, -X, +Y, -Y, +Z, -Z）を自動判定するため、1枚ずつ手動で設定する手間がかかりません。

### 3. ウィンドウ内で直感的に回せる3Dプレビュー
- マウスドラッグで視点やオブジェクトをぐるぐる回転させながら、キューブマップの映り込みや継ぎ目をその場で確認できます。
- **3種類のプレビュー形状**:
  - **Skybox**: 360度の全天球背景ビュー
  - **Sphere**: 金属感のある標準反射球
  - **Cube**: キューブメッシュ
- 軸ごとの回転制限（X, Y, Zチェックボックス）や回転リセット、背景Skyboxの表示切替、エディタ画面をくっきり美しく表示する **Super Sampling** 機能も搭載。

### 4. スライダー操作で手軽に美しいぼかし（ブラー / IBL反射）
- PBRマテリアルの粗さ（Roughness）に応じた自然な環境光反射を再現する **GGX Specular IBL** や、ふんわりとしたボケ味を作る **Gaussian / Bokeh ブラー** を搭載。
- スライダーを動かすだけで、リアルタイムプレビューを見ながら直感的に質感を作り込めます。

### 5. 面の向き・反転のズレもボタン1つで修正 (Face Flip)
- 取り込んだテクスチャの天地や左右が反転していた場合でも、外部ペイントソフトで修正する必要はありません。
- 6面それぞれに用意された **V (上下反転)** と **H (左右反転)** ボタンを押すだけで、エディタ上で即座に向きを補正できます。

### 6. あらゆる用途に対応する多彩な出力レイアウト
- ゲームやシェーダーの用途に合わせて、以下の形式へワンクリックで変換・出力できます：
  - **単一アセット (`.cubemap`)**: UnityネイティブのCubemap形式
  - **正距円筒図法パノラマ (Equirectangular)**: 水平回転オフセット調整が可能な 360° パノラマ画像 (2:1)
  - **クロス展開 (Cross Horizontal / Vertical)**: 十字型に展開されたテクスチャ
  - **一列並び展開 (Straight Horizontal / Vertical)**: 横または縦に6面を並べたテクスチャ
  - **6面個別画像 (Six-Sided)**: 面ごとの個別ファイル (`_XPlus`, `_XMinus` ...)
  - **Matcap**: 球面環境マップシェーダー用の円形テクスチャ（外縁部の色を塗り広げて境界の黒ずみを防ぐ補正機能付き）

### 7. インポート設定も自動完了
- プロジェクト内にエクスポートしたテクスチャは、テクスチャ形状（Cube / 2D）やsRGB、Mipmapなどのインポート設定が最適な状態に自動構成されます。
- パノラマやクロス・ストレート形式も、**Import as Cubemap** にチェックを入れるだけでUnityがキューブマップとして即座に認識します。

### 8. 再エクスポートしてもマテリアルの参照が外れない安心設計 (GUID維持)
- 解像度やフォーマットを変更して上書きエクスポートした場合でも、アセット固有の識別子（GUID）を保持します。
- すでにMaterialやPrefab、シーン内に割り当てていた参照リンクが外れて再設定に追われる心配がありません。

---

## 動作環境
- **Unity**: 2023.1 以降（Unity 6 完全対応）
- **レンダーパイプライン**: Universal Render Pipeline (URP 15.0.7 以降)
  - ※シーンカメラからのキャプチャ機能に URP を使用します。6面画像や既存Cubemapのプレビュー・変換・エクスポートは他パイプラインでも動作します。

---

## インストール方法 (UPM)

Unity Package Manager を開き、**Add package from git URL...** に以下を入力してインストールしてください：

```
https://github.com/ushizawa-jushichi/U17CubemapGenerator.git
```

---

## 使い方（クイックスタート）

1. 上部メニューからウィンドウを開きます: `Tools > U17CubemapGenerator`。
2. **Generator タブ**:
   - **Input Mode** を選択します:
     - `Current Scene`: カメラから直接キャプチャ。**Sync SceneView Camera** にチェックを入れると、シーンビューのカメラ視線とリアルタイム連動します。
     - `Six-Sided`: 6面のテクスチャを割り当て（フォルダからのドラッグ＆ドロップ対応）。
     - `Cubemap`: 既存のCubemapアセットを直接割り当て。
   - 必要に応じて **Face Flip** セクションで各面の上下（V）・左右（H）反転を調整します。
   - ぼかしを加えたい場合は **Blur Enable** にチェックを入れ、アルゴリズム（GGX / Bokeh）や粗さ、サンプル数を調整します。
   - **Output Resolution**（解像度）と **Output Layout**（レイアウト形式）を選択します。
   - 保存先パス（`Export File`）を指定し、**Export** ボタンを押します。
3. **Preview タブ**:
   - **Preview Object**（Skybox / Sphere / Cube）を切り替えて見た目を確認します。
   - プレビュー画面上をドラッグして回転させたり、X/Y/Zトグルで回転軸を制限できます。
   - **Super Sampling** を有効にすると、高解像度アンチエイリアシングで滑らかに表示されます。
4. **ツールバー機能**:
   - **Hide UI**: UIを非表示にし、プレビュー画面のみを大きく表示します。
   - **Hide Preview**: プレビューの描画を停止し、バックグラウンドでのGPU負荷をゼロにします。

---

## 機能詳細・技術仕様（エンジニア向け）

<details>
<summary><b>内部アーキテクチャと詳細仕様を展開する</b></summary>

### GPUコンピュートシェーダーとTDRクラッシュ防止
- **シェーダー構成**: `CubemapBlur.compute` による並行ディスパッチ（`CSMain` カーネル）。
- **TDR保護（チャンク分割処理）**: 高サンプル数（>= 1024）や大解像度テクスチャの計算時、GPUドライバのタイムアウト（TDR: Timeout Detection and Recovery）によるクラッシュを防ぐため、Y軸の行ブロック単位で分割ディスパッチを実行。1フレームあたりの実行時間が上限（8.0ms）を超えた場合は `EditorAwaitableUtility` により次フレームへ非同期に処理を譲渡し、進捗バーに進捗率を通知します。
- **内部高解像度サンプリング**: ダウンスケール前に高解像度内部サンプリングを維持し、極小の高輝度光源などの白飛び・ディテール消失を防止。

### 完全非同期エクスポートパイプライン
- **AsyncGPUReadback**: GPUメモリからメインメモリへの読み出しを非同期に実行（非対応プラットフォーム時は安全に同期読み込みへフォールバック）。
- **Awaitable & File.WriteAllBytesAsync**: Unity 2023の `Awaitable` システムと非同期ファイルI/Oを活用し、4096pxなどの大容量テクスチャ出力時にもエディタのメインスレッド（UI）を一切フリーズさせません。キャンセル操作にも完全対応。

### 正確なカラーマネジメント & HDR対応
- **Adaptive sRGB**: ソーステクスチャの `graphicsFormat` を解析し、最適なsRGBフラグを自動設定。
- **URP sRGB欠落対策**: URPの `UniversalRenderPipeline.ProcessRenderRequests` においてキューブマップ直接描画時に sRGB ディスクリプタが初期化されない不具合を回避するため、内部で正確なフォーマットを持つ一時2D RenderTextureを経由して転送・カラーマネジメントを100%保証。
- **HDRサポート**: HDR入力時は自動的に拡張子を `.exr`（32-bit Float / RGBAHalf）へと切り替え、高ダイナミックレンジを保持してエンコード。

### GPUテクスチャサイズ上限のリアルタイム検証
- 各レイアウト（例: Straight形式で横6倍など）の最終解像度を計算し、ハードウェアの `SystemInfo.maxTextureSize` および `SystemInfo.maxCubemapSize` とリアルタイムに比較。
- 上限を超える組み合わせが選択された場合はエラーメッセージを表示して **Export** ボタンを無効化し、エディタクラッシュやGPUエラーを未然に防止。

### 6面テクスチャのドラッグ＆ドロップ自動判別命名規則
ウィンドウにテクスチャをドラッグ＆ドロップした際、ファイル名の末尾（サフィックス）から以下のキーワードを検出して各面に自動配分します：
- **+X (Right)**: `_xplus`, `_posx`, `_px`, `+x`, `_right`, `_rt`, `-right`, `-rt`, `-px`
- **-X (Left)**: `_xminus`, `_negx`, `_nx`, `-x`, `_left`, `_lf`, `-left`, `-lf`, `-nx`
- **+Y (Up)**: `_yplus`, `_posy`, `_py`, `+y`, `_up`, `_top`, `-up`, `-top`, `-py`
- **-Y (Down)**: `_yminus`, `_negy`, `_ny`, `-y`, `_down`, `_bottom`, `_dn`, `_btm`, `-down`, `-bottom`, `-ny`
- **+Z (Front)**: `_zplus`, `_posz`, `_pz`, `+z`, `_front`, `_forward`, `_ft`, `_fwd`, `-front`, `-ft`, `-pz`
- **-Z (Back)**: `_zminus`, `_negz`, `_nz`, `-z`, `_back`, `_backward`, `_bk`, `_bwd`, `-back`, `-bk`, `-nz`

### スマートな保存先フォルダ推定とアセットGUID復元
- **初期フォルダ推定**:
  1. 現在設定されているエクスポートパス
  2. 入力元として割り当てられているテクスチャまたはCubemapアセットのフォルダ
  3. Projectウィンドウで現在選択中のフォルダまたはアセットの階層
  4. 上記が見つからない場合は `Assets/`
- **GUID維持アルゴリズム (`AssetMetaUtility`)**:
  `.cubemap` 上書き保存時、同一スペックであれば `EditorUtility.CopySerialized` と `Graphics.CopyTexture` で即時更新。解像度やフォーマットが変更されてインスタンスの再生成が必要な場合でも、既存アセットのGUIDを退避して `.meta` ファイルを自動復元・再インポートすることで、MaterialやPrefab等からのプロジェクト内参照リンクを完全に保護。

</details>

---

## ライセンス
[MIT License](LICENSE)

## サンプルアセットのクレジット
- [Bricks075A (ambientCG)](https://ambientcg.com/view?id=Bricks075A)
- [ChristmasTreeOrnamentSubstance004 (ambientCG)](https://ambientcg.com/view?id=ChristmasTreeOrnamentSubstance004)
- [Rural Landscape (Poly Haven)](https://polyhaven.com/a/rural_landscape)
