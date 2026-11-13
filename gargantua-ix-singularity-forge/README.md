# GARGANTUA-IX Relativistic Singularity

100% 純天文物理寫實、零人造設施的電影級克爾旋轉黑洞（Kerr Rotating Black Hole）3D 程序化模型與 WebGL 互動生成器，遵循 `threejs-model-to-unity-pipeline` 標準打造，將全部多邊形預算集中於吸積盤電漿結構、立體重力透鏡光弧、雙極相對論性噴流與磁場流線，並支援 `.glb`、`.obj` 與 `1024×1024` PBR 貼圖一鍵無損匯出至 Unity URP 與 Blender。

---

## 一、 純天文結構與幾何平滑規格（`1 unit = 1 meter`）

根節點為 `GARGANTUA_IX_Singularity_Root`，奇點中心位於 `(0, 0, 0)`。**全模型嚴格禁用 `THREE.BoxGeometry` 與任何 90 度垂直硬邊，且嚴禁 `flatShading: true`**；所有天體電漿結構皆以 `THREE.CatmullRomCurve3` 多截面樣條放樣與雙凸透鏡（Lenticular）曲線環構成，邊緣平滑收薄以確保 Unity 任意 3D 視角皆具真實立體體積，並計算**真實累積歐幾里得弧長（Cumulative Euclidean Arc-Length）**作為 `V` 座標、平均閉合接縫法線與烘焙原生頂點色彩 `COLOR_0`（黑體輻射徑向溫度梯度 + 相對論性都卜勒集束效應：左側朝向觀察者藍移亮化 `#E6F7FF` / `#FFF0C2`，右側遠離觀察者紅移暗化 `#D9481E` / `#4A0B05`）。

| 子節點命名 (`Part_*`) | 天文物理結構與幾何規格 |
| :--- | :--- |
| `Part_EventHorizon` | 半徑 `1.80 m`（赤道）× `1.69 m`（極軸，克爾高速自轉扁球體 Kerr Oblateness）高細分測地線球體，零反照率純黑吸光材質（`#000000`, `metalness: 0.0`, `roughness: 1.0`），邊緣呈現銳利黑洞陰影輪廓 |
| `Part_PhotonRing` | 緊貼事件視界外緣（半徑 `1.92 m`）的多截面雙凸透鏡放樣高能光子環與微幅傾角框曳軌道絲，呈現熾熱白金色（`#FFF9E6` 至 `#FFB347`）自發光 |
| `Part_LensingUpperArc` / `Part_LensingLowerArc` | 三層嵌套參數化 3D 樣條放樣立體重力透鏡光弧（平滑漸薄雙凸透鏡截面），橫跨北極上方與南極下方並隨 `Z` 軸深彎繞視界背後，即時支援曲率調整（`0.5x–1.5x`） |
| `Part_AccretionInnerHotDisk` | 半徑 `2.25 m` – `4.10 m` ISCO 最內層穩定軌道高密度雙凸透鏡電漿盤 + 兩道對數螺旋高速剪切電漿流束 |
| `Part_AccretionMidTurbulentDisk` | 半徑 `3.80 m` – `6.80 m` 中層 3 臂螺旋密度波起伏吸積盤 + 三道立體螺旋波紋電漿帶 |
| `Part_AccretionOuterDustHalo` | 半徑 `6.20 m` – `9.60 m` 外層擴散氣體與暗紅塵埃吸積暈（邊緣漸薄收束，呈現琥珀橙至深赤紅光譜與 5 臂波紋浮雕） |
| `Part_NorthPolarJet` / `Part_SouthPolarJet` | 沿 `+Y` / `-Y` 自轉軸噴發之拋物線放樣相對論性電漿噴流漏斗（單側長 `10.0 m`，端點與噴口皆平滑閉合收束） |
| `Part_MagneticFluxHelix` | 8 股 `CatmullRomCurve3` 雙螺旋磁場流線電漿管（端點平滑收尖），從能層兩極纏繞雙極噴流向外延伸 |

---

## 二、 雙重 LOD 預算與即時互動控制

| LOD 模式 | 目標預算 | 實測規格 | Draw Calls | 適用場景 |
| :--- | :--- | :--- | :--- | :--- |
| **Hero Showcase** | ~20,000 Tris | **20,176 Tris** / **11,362 Verts** | **10** | 電影級近距離展示、3D 美術預覽 |
| **Game-Ready** | ~6,000 Tris | **4,520 Tris** / **2,746 Verts** | **10** | 即時遊戲執行期資產、Unity URP 高幀率場景 |

### WebGL 即時互動項目（4 滑桿 + 2 開關）
- **4 組即時滑桿**：
  1. `Kerr Spin / Accretion Velocity`（`0.0x` – `3.0x`）
  2. `Doppler Shift & Blackbody Temp`（`3,000 K` – `15,000 K`）
  3. `Relativistic Jet Luminosity`（`0%` – `100%`）
  4. `Gravitational Lensing Warp`（`0.5x` – `1.5x`）
- **2 組檢視開關**：`Show Polar Jets & Helices（顯示雙極噴流與磁場線）`、`Wireframe / Topology` 拓撲檢視開關。
- **Pre-Export Rest-Pose Reset**：點擊 `.glb` 或 `.obj` 匯出前自動暫停動態並將所有旋轉中的吸積盤、噴流與磁場線歸零至標準初始姿態 `(0, 0, 0)`，序列化純淨 `assetGroup` 後恢復預覽。

---

## 三、 `1024×1024` 純天文程序化 PBR 貼圖規格

透過離屏 `<canvas>` 動態生成五張無縫週期性 `1024×1024` 純天文 PBR 貼圖（`Metalness` 全域為 `0.0`，杜絕任何金屬或塑膠感）：

1. **`GARGANTUA_IX_Albedo.png`**（`SRGBColorSpace`）：多八度亂流螺旋電漿絲（Band A）、縱向重力透鏡電離流束（Band B）、雙極噴流與磁螺旋電離帶（Band C）及純黑事件視界（Band D）。
2. **`GARGANTUA_IX_Normal_Sobel.png`**（`NoColorSpace`）：經 `3×3 Sobel` 濾波器從電漿渦流高度場烘焙之切線空間法線貼圖。
3. **`GARGANTUA_IX_Roughness.png`**（`NoColorSpace`）：事件視界純啞光（`1.0`）、吸積盤電漿與塵埃帶（`0.65–0.90`）。
4. **`GARGANTUA_IX_Metalness.png`**（`NoColorSpace`）：純天文天體全域設為 `0.0`（`#000000`）。
5. **`GARGANTUA_IX_Emissive.png`**（`SRGBColorSpace`）：高動態範圍電漿絲發光通道、光子環核心、上下重力透鏡光弧與噴流螺旋電離紋理。

---

## 四、 Unity URP 匯入與設定步驟

1. **匯入 `.glb` 模型**：
   - 於 Unity Package Manager 安裝 `com.unity.cloud.gltfast`（`glTFast`），將匯出之 `GARGANTUA_IX_Singularity_hero.glb` 或 `GARGANTUA_IX_Singularity_game.glb` 匯入專案 `Assets/Models/GargantuaIX/`（若改用 Blender 轉 `.fbx`，請啟用 `Path Mode: Copy` 與 `Embed Textures`）。
2. **URP Lit 材質與貼圖設定**：
   - 若手動指定獨立 `.png` 貼圖，請確認 `GARGANTUA_IX_Normal_Sobel.png` 的 Texture Type 設為 **Normal map**；`Roughness` 與 `Metalness` 取消勾選 **sRGB (Color Texture)**。
   - 啟用 **Emission** 並指定 `GARGANTUA_IX_Emissive.png`（配合 Unity URP Volume Post-Processing Bloom 與 `ACES` Tonemapping）。
   - `MAT_EventHorizon_Singularity` 保持 `Base Map` 純黑 `#000000`、`Smoothness: 0.0`、`Metallic: 0.0`、關閉 Emission 與 Specular Highlights。
   - `MAT_Relativistic_Polar_Jet` 設為 `Surface Type: Transparent`、`Blending Mode: Alpha`（或 `Additive` 強化深空電漿光束感）。
3. **掛載執行期控制器**：
   - 將 `SingularityAccretionController.cs` 掛載於匯入的根節點 `GARGANTUA_IX_Singularity_Root`。
   - 腳本於 `Awake()` 自動解析所有 `Part_*` 子節點，於 `Update()` 執行開普勒差速自轉（內圈最快、外圈漸慢）、磁力線螺旋進動，並以零 GC 分配的 `MaterialPropertyBlock` 驅動 `_EmissionColor` HDR 能量脈動與黑體色溫偏移。
