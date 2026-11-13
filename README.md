# threejs-model-forge

基於 Three.js (`0.169.0`–`0.170.0`) 與 `threejs-model-to-unity-pipeline` 工作流程建置的程序化 3D 模型生成器與 Unity URP 控制器合集。資產普遍支援 LOD 切換、1024×1024 程序化 Canvas PBR 貼圖（3×3 Sobel 法線烘焙與嚴格 ColorSpace 分離）、頂點色彩 AO（`COLOR_0`），並支援一鍵無損導出 `.glb`、`.obj` 與獨立 `.png` PBR 貼圖。

## 收錄模型列表

| 模型專案 | 類型 / 題材 | WebGL 生成器 | Unity URP 附屬腳本 |
| :--- | :--- | :--- | :--- |
| **Astral Chronos Reliquary** | 精密機械天文渾天儀秘匣 | [astral_chronos_forge.html](./astral-chronos-forge/Tools/ModelForge/astral_chronos_forge.html) | `AstrolabeOrbitController.cs`, `ArcaneEmissionPulse.cs` |
| **TRITON-IX Abyssal Submersible Rig** | 深海科學研究載人潛水器 | [triton_ix_submersible_forge.html](./triton-ix-submersible-forge/Tools/ModelForge/triton_ix_submersible_forge.html) | `SubmersibleThrusterController.cs`, `SubmersibleBeaconPulse.cs` |
| **WRAITH-IX Tactical Stealth Interceptor** | 近未來微科幻戰術隱形戰機 | [wraith_ix_interceptor_forge.html](./wraith-ix-interceptor-forge/Tools/ModelForge/wraith_ix_interceptor_forge.html) | `StealthInterceptorController.cs` |
| **AETHER-9 Vectored Plasma-Turbofan** | 科幻矢量電漿渦輪風扇引擎 | [aether_9_vectored_plasma_turbofan_engine_forge.html](./aether-9-turbofan-forge/Tools/ModelForge/aether_9_vectored_plasma_turbofan_engine_forge.html) | `Aether9TurbineController.cs` |
| **Arcane Astrolabe Altar** | 哥德秘術星盤祭壇 | [arcane_astrolabe_altar_procedural_3d_forge.html](./arcane-astrolabe-altar-forge/Tools/ModelForge/arcane_astrolabe_altar_procedural_3d_forge.html) | `ArcaneAstrolabeAltarController.cs` |
| **Sovereign's Eclipse Relic** | 君王蝕日秘匣大劍與神龕 | [sovereign_s_eclipse_arcane_relic_forge.html](./sovereigns-eclipse-relic-forge/Tools/ModelForge/sovereign_s_eclipse_arcane_relic_forge.html) | `SovereignRelicController.cs` |
| **Sovereign's Oath (3A ARPG Forge)** | 3A ARPG 視角君王誓約祭壇 | [sovereign_s_oath_3a_arpg_asset_forge.html](./sovereigns-oath-arpg-forge/Tools/ModelForge/sovereign_s_oath_3a_arpg_asset_forge.html) | `SovereignsOathController.cs` |
| **Sovereign's Oath (Gothic Altar)** | 哥德皇家大劍聖壇 | [sovereign_s_oath_royal_greatsword_gothic_altar_forge.html](./sovereigns-oath-gothic-altar-forge/Tools/ModelForge/sovereign_s_oath_royal_greatsword_gothic_altar_forge.html) | `SovereignsOathGothicController.cs` |
| **AEGIS-VII Orbital Drop Mech** | 近未來重型軌道空降作戰機甲 | [aegis_vii_orbital_mech_forge.html](./aegis-vii-orbital-mech-forge/Tools/ModelForge/aegis_vii_orbital_mech_forge.html) | `OrbitalMechController.cs` |
| **AERO-900X Widebody Commercial Airliner** | 次世代雙發廣體長程客機（含駕駛艙與客艙剖視） | [aero_900x_airliner_forge.html](./aero-900x-airliner-forge/aero_900x_airliner_forge.html) | `AirlinerFlightSystemsController.cs` |
| **GARGANTUA-IX Relativistic Singularity** | 克爾旋轉黑洞、立體重力透鏡與觀測陣列 | [gargantua_ix_singularity_forge.html](./gargantua-ix-singularity-forge/gargantua_ix_singularity_forge.html) | `SingularityAccretionController.cs` |

## 本機預覽方式

建議使用本地 HTTP 伺服器啟動根目錄，以便一鍵下載 `.glb` / `.obj` / `.png` 檔案且不受 `file://` 權限限制：

```bash
python -m http.server 8767
```

開啟瀏覽器訪問 `http://localhost:8767/` 即可透過展示首頁切換預覽所有模型。

## 資料夾規範（新模型擴充結構）

本倉庫同時收錄通用程序化建模技能 [threejs-model-to-unity-pipeline](./.agent/skills/threejs-model-to-unity-pipeline/SKILL.md)，於 Antigravity 開啟本專案時會自動載入為 Workspace Skill。日後新增模型皆統一建立於本倉庫下之獨立子資料夾：

```text
.agent/skills/
└── threejs-model-to-unity-pipeline/
    └── SKILL.md          # 通用 Three.js -> Unity URP / Blender 建模與導出技能規範
<asset-name>/
├── Assets/
│   └── Scripts/          # Unity URP Runtime 驅動與 MaterialPropertyBlock 腳本
└── Tools/
    └── ModelForge/       # 獨立單檔 Three.js 程序化生成與 PBR/.GLB 導出器 (.html)
```
