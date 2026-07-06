# Linxium.SuperShader

实用 URP Shader 与后处理效果合集。仅依赖 URP，**无需 UniTask / DOTween**。

## 安装

在 `Packages/manifest.json` 中添加：

```json
"com.linxium.supershader": "https://github.com/CS-LX/Linxium.SuperShader.git"
```

## 快速接入（3 步）

1. 打开 URP Renderer Asset，**Add Renderer Feature** → `Super Shader Renderer Feature`
2. 在场景中添加 **Global Volume**，新建或编辑 Volume Profile
3. 添加所需 Override：
   - `Linxium/CRT Look` — 复古 CRT 扫描线、桶形畸变、暗角、色差
   - `Linxium/TV Static` — 雪花屏 / 信号噪点
   - `Linxium/Glitch Transition` — 故障、滚屏、垂直塌陷（也可仅用过场 API 驱动）

> 关闭效果时请取消 Volume 条目左侧的 **Active** 勾选。请勿在 `DefaultVolumeProfile` 中默认启用 CRT/TV，否则场景 Volume 关闭后仍可能被全局默认配置叠加。

## Shaders

### HandDrawnLayer (`Linxium/HandDrawnWobble`)

URP 手绘风格顶点蠕动，适用于 Sprite / 透明物体，支持 Stencil。

> CRT / TV 所有强度参数默认 **0**。必须勾选 **Active** 且对具体参数勾选 **Override** 并调大后才会生效。

### Post-processing（解耦三层）

| 层 | Volume | 作用 |
|----|--------|------|
| CRT Look | `CRTLookVolume` | 常驻复古显像管观感 |
| TV Static | `TVStaticVolume` | 可选雪花噪点叠加 |
| Glitch Transition | `GlitchTransitionVolume` | 瞬时故障 / 开关机过场 |

渲染顺序：**Glitch → CRT Look → TV Static**（仅执行已激活的 Pass）。

> Unity 6 / URP 17+ 使用 Render Graph，`SuperShaderRendererFeature` 已实现 `RecordRenderGraph`，无需开启 Compatibility Mode。

## 过场动画（无需额外依赖）

```csharp
using Linxium.SuperShader;
using System.Collections;

// 方式 A：Coroutine + 回调
GlitchTransitionPlayer.Play(GlitchTransitionKind.PowerOff, 0.36f, useUnscaledTime: true, onComplete: () => {
    // 切换场景...
});

// 方式 B：yield 等待
IEnumerator SwitchScene() {
    GlitchTransitionPlayer.Play(GlitchTransitionKind.PowerOff);
    while (GlitchTransitionPlayer.IsPlaying) yield return null;
    // 加载...
    GlitchTransitionPlayer.Play(GlitchTransitionKind.PowerOn);
}
```

运行时参数通过 `PostEffectOverrides` 叠加在 Volume 之上，无需静态全局类。

## 从旧 CRT 实现迁移

| 旧 (26CGJ) | 新 (SuperShader) |
|------------|------------------|
| `CrtEffectVolume` | `CRTLookVolume` + 可选 `TVStaticVolume` |
| `CrtEffectRuntimeOverrides` | `PostEffectOverrides`（`CRT` / `TV` / `Glitch`） |
| `SessionCrtSwitchEffect` | `GlitchTransitionPlayer.Play(PowerOff/PowerOn)` |
| `CrtRendererFeature` | `SuperShaderRendererFeature` |

## License

MIT
