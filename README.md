# Linxium SuperShader

面向 URP 的 Shader 与后处理效果包。仅依赖 `com.unity.render-pipelines.universal`，**无需 UniTask / DOTween**。

---

## 包内容

| 模块 | Shader 路径 | 是否需要额外配置 |
|------|-------------|------------------|
| 手绘蠕动 | `Linxium/HandDrawnWobble` | 否，创建材质即可 |
| CRT 后处理栈 | `Hidden/Linxium/*`（内部） | 是，需 Renderer Feature + Volume |

---

## 环境要求

- Unity **6000.2+**
- URP **17.3+**（Unity 6 Render Graph，无需 Compatibility Mode）

---

## 安装

在项目的 `Packages/manifest.json` 中添加：

```json
"com.linxium.supershader": "https://github.com/CS-LX/Linxium.SuperShader.git"
```

---

## 1. 手绘蠕动 Shader

**路径：** `Linxium/HandDrawnWobble`

适用于 Sprite / 透明物体的顶点蠕动效果，支持 Stencil。

| 属性 | 说明 |
|------|------|
| Wobble Speed (Hz) | 蠕动频率 |
| Wobble Scale | 噪声采样尺度 |
| Wobble Strength | 位移强度 |
| Stencil * | 模板测试配置 |

创建材质 → 指定此 Shader → 赋给 Renderer 即可，**无需** Volume 或 Renderer Feature。

---

## 2. CRT 后处理栈

三层效果可独立开关、叠加使用：

```
Volume Profile
    ↓
VolumeManager Stack
    ↓
SuperShaderRendererFeature（URP Renderer Feature）
    ↓  渲染顺序
Glitch Transition → CRT Look → TV Static
```

### 2.1 项目级配置（每个 URP 项目做一次）

1. 打开 **URP Renderer Asset**（如 `PC_Renderer`）
2. **Add Renderer Feature** → `Super Shader Renderer Feature`
3. 场景中放置 **Global Volume**，新建或指定 Volume Profile
4. 在 Profile 中 **Add Override**，从 `Linxium/` 下选择所需组件

### 2.2 Volume 组件

| 菜单路径 | 类名 | 作用 |
|----------|------|------|
| `Linxium/CRT Look` | `CRTLookVolume` | 扫描线、桶形畸变、暗角、色差 |
| `Linxium/TV Static` | `TVStaticVolume` | 雪花噪点叠加 |
| `Linxium/Glitch Transition` | `GlitchTransitionVolume` | 故障、滚屏、垂直塌陷 |

**CRT Look 参数**

| 参数 | 范围 | 默认 | 说明 |
|------|------|------|------|
| Scanline Intensity | 0–1 | 0 | 扫描线强度 |
| Distortion Amount | 0–0.5 | 0 | 桶形畸变 |
| Scanline Speed | 0–12 | 8 | 扫描线滚动速度 |
| Vignette Intensity | 0–0.5 | 0 | 暗角 |
| Chromatic Aberration | 0–0.05 | 0 | 色差 |

**TV Static 参数**

| 参数 | 范围 | 默认 | 说明 |
|------|------|------|------|
| Noise Intensity | 0–1 | 0 | 噪点强度 |
| Noise Speed | 0–120 | 24 | 噪点刷新速度 |
| Monochrome | 0–1 | 1 | 单色/彩色雪花 |

**Glitch Transition 参数**

| 参数 | 范围 | 默认 | 说明 |
|------|------|------|------|
| Glitch Strength | 0–1 | 0 | 水平撕裂 |
| Roll Y | 0–1 | 0 | 垂直滚屏 |
| Vertical Collapse | 0–1 | 0 | 垂直塌陷 |
| Flash | 0–1 | 0 | 闪光 |

> **启用方式：** 勾选组件左侧 **Active**，再对需要调节的参数勾选 **Override** 并设值。所有强度类参数默认 **0**，仅勾选 Active 不会产生可见效果。

> **注意：** 请勿在 URP Asset 的 `DefaultVolumeProfile` 中默认启用 CRT/TV，否则场景 Volume 关闭后仍可能被全局默认配置叠加。

### 2.3 运行时 API

**临时覆盖 Volume 数值**（过场、Gameplay 用，叠加在 Volume 之上）：

```csharp
using Linxium.SuperShader;

PostEffectOverrides.CRT.ScanlineIntensity = 0.3f;
PostEffectOverrides.Glitch.GlitchStrength = 0.8f;
PostEffectOverrides.ResetAll(); // 清除全部覆盖
```

**开关机过场**（内置 Coroutine，无需额外依赖）：

```csharp
// 方式 A：回调
GlitchTransitionPlayer.Play(
    GlitchTransitionKind.PowerOff,
    duration: 0.36f,
    useUnscaledTime: true,
    onComplete: () => { /* 切场景 */ });

// 方式 B：yield 等待
IEnumerator SwitchScene() {
    GlitchTransitionPlayer.Play(GlitchTransitionKind.PowerOff);
    while (GlitchTransitionPlayer.IsPlaying) yield return null;
    // 加载新场景...
    GlitchTransitionPlayer.Play(GlitchTransitionKind.PowerOn);
}
```

---

## 从旧 CRT 实现迁移

若从 26CGJ 项目的单体 CRT 方案迁移：

| 旧 (26CGJ) | 新 (SuperShader) |
|------------|------------------|
| `CrtEffectVolume` | `CRTLookVolume` + 可选 `TVStaticVolume` |
| `CrtEffectRuntimeOverrides` | `PostEffectOverrides`（`CRT` / `TV` / `Glitch`） |
| `SessionCrtSwitchEffect` | `GlitchTransitionPlayer.Play(PowerOff/PowerOn)` |
| `CrtRendererFeature` | `SuperShaderRendererFeature` |

---

## 目录结构

```
Linxium.SuperShader/
├── Shaders/
│   ├── HandDrawnLayer.shader          # Linxium/HandDrawnWobble
│   └── PostProcessing/
│       ├── CRTLook.shader
│       ├── TVStatic.shader
│       └── GlitchTransition.shader
└── Runtime/
    ├── Linxium.SuperShader.asmdef
    └── PostProcessing/
        ├── Features/SuperShaderRendererFeature.cs
        ├── Volumes/                     # CRTLook / TVStatic / GlitchTransition
        ├── Overrides/                   # PostEffectOverrides + Coroutine Runner
        └── Presets/GlitchTransitionPlayer.cs
```

---

## License

MIT
