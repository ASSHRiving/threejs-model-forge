# threejs-model-forge

基於 Three.js (`0.170.0`) 與 `threejs-model-to-unity-pipeline` 工作流程建置的程序化 3D 模型生成器與 Unity URP 控制器合集。每個資產皆內建雙 LOD 切換（Hero / Game-Ready）、1024×1024 程序化 Canvas PBR 貼圖（3×3 Sobel 法線烘焙與嚴格 ColorSpace 分離）、頂點色彩 AO（`COLOR_0`），並支援一鍵 Rest-Pose 歸零後無損導出 `.glb`、`.obj` 與獨立 `.png` PBR 貼圖。

## 收錄模型列表

| 模型專案 | 類型 / 題材 | WebGL 生成器 | Unity URP 附屬腳本 |
| :--- | :--- | :--- | :--- |
| **Astral Chronos Reliquary** | 精密機械天文渾天儀秘匣 | [astral_chronos_forge.html](./astral-chronos-forge/Tools/ModelForge/astral_chronos_forge.html) | `AstrolabeOrbitController.cs`, `ArcaneEmissionPulse.cs` |
| **TRITON-IX Abyssal Submersible Rig** | 深海科學研究載人潛水器 | [triton_ix_submersible_forge.html](./triton-ix-submersible-forge/Tools/ModelForge/triton_ix_submersible_forge.html) | `SubmersibleThrusterController.cs`, `SubmersibleBeaconPulse.cs` |
| **WRAITH-IX Tactical Stealth Interceptor** | 近未來微科幻戰術隱形戰機 | [wraith_ix_interceptor_forge.html](./wraith-ix-interceptor-forge/Tools/ModelForge/wraith_ix_interceptor_forge.html) | `StealthInterceptorController.cs` |

## 本機預覽方式

建議使用本地 HTTP 伺服器啟動根目錄，以便一鍵下載 `.glb` / `.obj` / `.png` 檔案且不受 `file://` 權限限制：

```bash
python -m http.server 8767
```

開啟瀏覽器訪問 `http://localhost:8767/` 即可透過展示首頁切換預覽所有模型。

## 資料夾規範（新模型擴充結構）

日後新增模型皆統一建立於本倉庫下之獨立子資料夾：

```text
<asset-name>/
├── Assets/
│   └── Scripts/          # Unity URP Runtime 驅動與 MaterialPropertyBlock 腳本
└── Tools/
    └── ModelForge/       # 獨立單檔 Three.js 程序化生成與 PBR/.GLB 導出器 (.html)
```
