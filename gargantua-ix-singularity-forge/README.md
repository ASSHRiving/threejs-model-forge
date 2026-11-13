# GARGANTUA-IX Relativistic Singularity

電影級天文物理與精密科幻觀測結構兼具的克爾旋轉黑洞（Kerr Rotating Black Hole）與相對論性吸積盤 3D 程序化模型生成器，遵循 `threejs-model-to-unity-pipeline` 標準打造，支援無損 `.glb`、`.obj` 與 `1024×1024` PBR 貼圖一鍵匯出至 Unity URP 與 Blender。

---

## 一、 幾何結構與比例規格（`1 unit = 1 meter`）

根節點為 `GARGANTUA_IX_Singularity_Root`，奇點中心位於 `(0, 0, 0)`。**全模型嚴格禁用未經倒角的原始 `BoxGeometry` 與任何 90 度垂直硬邊**，全面採用連續多截面放樣、雙凸透鏡（Lenticular）曲線環、`CatmullRomCurve3` Bishop 平行傳輸掃掠體、以及多段倒角擠出/旋轉成形，並計算真實累積歐幾里得弧長（Cumulative Euclidean Arc-Length）作為 `V` 座標、平均閉合接縫法線與烘焙 `COLOR_0` 頂點色彩（Crevice AO + 黑體輻射梯度 + 相對論性都卜勒集束效應）。

| 子節點命名 | 幾何規格與結構說明 |
| :--- | :--- |
| `Part_EventHorizon` | 半徑 `1.80 m` 高細分測地線球體（零反照率純黑吸光 `metalness: 0.0, roughness: 1.0`）與外緣微弱重力紅移暗化邊界層 |
| `Part_PhotonRing` | 半徑 `1.92 m` 雙凸透鏡赤道電漿光子環 + 兩組高傾角球面光子軌道光環（白金 `#FFF6D6` 至 `#FF9D2E`） |
| `Part_LensingUpperArc` / `Part_LensingLowerArc` | 雙層參數化 3D 樣條放樣立體重力透鏡彎曲光弧（南北極弧線，雙凸透鏡漸薄截面，可即時調整彎曲率 `0.5x–1.5x`） |
| `Part_AccretionInnerDisk` | 內半徑 `2.35 m` – `4.05 m` 超高溫最內穩定圓軌道（ISCO）雙凸透鏡立體吸積環，開普勒高速差速旋轉 |
| `Part_AccretionMidSpiral` | 中半徑 `4.09 m` – `6.24 m` 3 臂對數螺旋密度波紋 3D 吸積環暨高層電漿絲環 |
| `Part_AccretionOuterDust` | 外半徑 `6.28 m` – `8.50 m` 5 臂冷塵埃吸收與磁化絲狀外環 |
| `Part_NorthPolarJet` / `Part_SouthPolarJet` | 沿 `+Y` / `-Y` 噴發之拋物線 Blandford–Znajek 相對論性電漿噴流漏斗（單側長 `9.5 m`，延伸至 `±11.25 m`） |
| `Part_MagneticHelix` | 8 股 `CatmullRomCurve3` 雙螺旋磁場流線管（端點平滑收尖） |
| `Part_TelemetryRing` / `Part_SensorNodes` | 半徑 `9.60 m` 外環克爾度規觀測雙軌環（12 組彎曲 C 型拱肋）+ 3 座無直角流線型觀測艙（配備 `MeshPhysicalMaterial` 半透明石英穹頂、內部金箔拋物線聚焦反射鏡、雙軸倒角萬向節雷射干涉儀與脈動核心） |

---

## 二、 LOD 預算與即時控制

| LOD 模式 | 目標預算 | 實測數據（含全觀測陣列） | 適用場景 |
| :--- | :--- | :--- | :--- |
| **Hero Showcase** | ~20k Tris | **20,800 Tris** / **14,740 Verts** / **24 Draw Calls** / 14 Mats | 電影級近距離展示、3D 美術預覽 |
| **Game-Ready** | ~6k Tris | **6,552 Tris** / **6,212 Verts** / **24 Draw Calls** / 14 Mats | 實時遊戲執行期資產、Unity URP 高幀率場景 |

### WebGL 即時互動項目
- **4 組物理參數滑桿**：`Accretion Spin Speed (0.0x–3.0x)`、`Plasma Temp & Doppler (3,000 K–15,000 K)`、`Polar Jet Intensity (0%–100%)`、`Lensing Curvature (0.5x–1.5x)`。
- **檢視切換開關**：`Show Telemetry Array`、`Wireframe`、`Collider_*` 凸包代理檢視。
- **Pre-Export Rest-Pose Reset**：點擊 `.glb` 或 `.obj` 匯出前會自動暫停並歸零所有旋轉子物件至標準靜止姿態，並隱藏 `Collider_*` 代理盒。

---

## 三、 `1024×1024` 程序化 PBR 貼圖規格

透過離屏 `<canvas>` 動態生成五張無縫週期性 `1024×1024` 分區 PBR 圖集（支援單張點擊或一鍵全部下載）：

1. **`GARGANTUA_IX_Albedo.png`**（`SRGBColorSpace`）：多八度亂流螺旋電漿絲、暗物質塵埃吸收帶、拉絲鈦金屬裝甲與六角金箔反射鏡塗層。
2. **`GARGANTUA_IX_Normal_Sobel.png`**（`NoColorSpace`）：以 `3×3 Sobel` 卷積濾波器自高度場烘焙之切線空間法線貼圖。
3. **`GARGANTUA_IX_Roughness.png`**（`NoColorSpace`）：事件視界純啞光 (`1.0`)、塵埃電漿 (`0.55–0.75`)、拉絲鈦金屬 (`0.22–0.35`)、金箔鏡面 (`0.18–0.25`)。
4. **`GARGANTUA_IX_Metalness.png`**（`NoColorSpace`）：天體電漿與事件視界 (`0.0`)、觀測艙鈦金屬 (`0.88–0.90`)、金箔反射鏡 (`0.95`)。
5. **`GARGANTUA_IX_Emissive.png`**（`SRGBColorSpace`）：高動態範圍電漿絲發光通道、光子環核心與觀測艙光學指示線。

---

## 四、 Unity URP 匯入與設定步驟

1. **匯入 `.glb` 模型**：
   - 於 Unity 專案透過 Package Manager 安裝 `com.unity.cloud.gltfast`（`glTFast`），或將匯出之 `GARGANTUA_IX_Singularity_hero.glb` 直接拖入 `Assets/Models/GargantuaIX/`。
   - 若使用 Blender 轉出 `.fbx`，請設定 `Path Mode: Copy` 並勾選 `Embed Textures`。
2. **材質與貼圖通道設定（URP Lit）**：
   - 若獨立套用 `.png` 貼圖，請確認 `GARGANTUA_IX_Normal_Sobel.png` 的 Texture Type 設為 **Normal map**；`Roughness` 與 `Metalness` 取消勾選 **sRGB (Color Texture)**。
   - 啟用 **Emission** 並將 `GARGANTUA_IX_Emissive.png` 指定至 Emission Map（Color 設為 HDR 強度 `> 1.5` 以搭配 URP Post-Processing Bloom）。
   - 將 `MAT_Quartz_Optical_Dome` 設為 URP Lit `Surface Type: Transparent`、`Smoothness: 0.95`、`Alpha: 0.30`。
3. **掛載執行期控制器**：
   - 將 `SingularityAccretionController.cs` 掛載於匯入的根節點 `GARGANTUA_IX_Singularity_Root` 上。
   - 腳本於 `Awake()` 會自動綁定所有 `Part_*` 子物件與發光渲染器，並於 `Update()` 以零 GC 分配的 `MaterialPropertyBlock` 驅動開普勒差速自轉與 `_EmissionColor` HDR 脈動。
