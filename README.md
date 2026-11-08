<div align="center">

<img src="src/LiveCaptions-Translator.ico" width="128" height="128" alt="LiveCaptions Translator Icon"/>

# LiveCaptions Translator

### 用 Windows 实时字幕 + 翻译 API，做实时语音翻译

[![GitHub Release](https://img.shields.io/github/v/release/Courtoisrl/LiveCaptions-Translator?label=Latest&color=yellow)](https://github.com/Courtoisrl/LiveCaptions-Translator/releases/latest)
[![Windows 11](https://img.shields.io/badge/platform-Windows11-blue?logo=windows11&color=1E9BFA)](https://www.microsoft.com/windows/windows-11)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/github/license/Courtoisrl/LiveCaptions-Translator)](LICENSE)

</div>

## 这是什么

Windows 自带的实时字幕（LiveCaptions）识别准确、占用低，但它只转写、不翻译。本项目把 LiveCaptions 的识别结果接上翻译 API，在看直播、视频、开会时给出实时的原文 + 译文，并且不需要 Copilot+ PC。

本仓库是 [SakiRinn/LiveCaptions-Translator](https://github.com/SakiRinn/LiveCaptions-Translator) 的二次开发版本，在上游功能全部保留的前提下，加入了几处偏日常使用的改动。

## 与上游的差异

| 改动 | 行为 |
| --- | --- |
| **最小化到托盘** | 点最小化按钮后主窗口收进通知区，不再占任务栏；托盘图标就是程序图标 |
| **启动即暂停翻译** | 每次打开软件都处于暂停（Log Only）状态，需要时再点一下恢复 |
| **启动时最小化开关** | 设置页新增「启动时最小化」，开启后每次启动直接最小化到托盘 |
| **悬浮字幕最多三行** | 原文、译文各自最多显示 3 行，超出后保留最新内容，旧行向上滚出 |
| **悬浮字幕布局记忆** | 记住上次显示的卡片组合与上下顺序，下次启动照旧 |
| **悬浮字幕开关记忆** | 上次关掉了悬浮窗，这次启动就不会自动弹出来；上次开着，这次启动会自动打开 |
| **界面为简体中文** | 设置页与常用界面都已汉化，API 名称等技术名词保留英文 |

<div align="center">
  <img src="images/preview.png" alt="LiveCaptions Translator 主界面" width="90%" />
  <br>
  <em style="font-size:80%">主界面</em>
</div>

## 功能

- **🔄 与 Windows 实时字幕无缝配合**

  程序会自动拉起 Windows LiveCaptions，并在首次使用后把它隐藏起来，你只需要面对一个窗口。

  在 Windows LiveCaptions 里勾选 ***Include microphone audio*** 即可识别麦克风；识别源语言在 LiveCaptions 里改。

- **🎨 现代界面**

  Fluent UI 风格，跟随系统自动切换浅色 / 深色主题。

- **🌐 多种翻译服务**

  | API | 类型 | 部署 |
  | --- | --- | --- |
  | Google / Google2 | 传统 | 在线，免配置 |
  | DeepL | 传统 | 在线 |
  | 有道 | 传统 | 在线 |
  | 百度翻译 | 传统 | 在线 |
  | [MTranServer](https://github.com/xxnuo/MTranServer) | 传统 | 自建 |
  | [LibreTranslate](https://libretranslate.com/) | 传统 | 自建 |
  | [Ollama](https://ollama.com) | 大模型 | 本地 |
  | LM Studio | 大模型 | 本地 |
  | OpenAI 兼容接口 | 大模型 | 在线 |
  | [OpenRouter](https://openrouter.ai) | 大模型 | 在线 |

  大模型类接口更擅长处理不完整句子和上下文，长句、口语场景推荐优先选它们。

- **🪟 悬浮字幕窗口**

  无边框、半透明的置顶窗口，可以把字幕叠在游戏或视频上；点击“锁定窗口”后鼠标可穿透窗口、完全不挡操作。

  <div align="center">
    <img src="images/overlay_window.png" alt="悬浮字幕窗口" width="80%" />
    <br>
    <em style="font-size:80%">悬浮字幕窗口</em>
  </div>

  窗口上的控制栏可以调字体大小、描边、加粗、文字颜色、背景透明度与颜色，切换“原文 / 双语 / 仅译文”三种显示模式，切换原文与译文的上下顺序，也可以拖动窗口左右边缘调整宽度。窗口位置会被记住。

- **⚙️ 常用控制**

  标题栏提供置顶、暂停翻译、日志卡片、悬浮窗四个按钮；字幕区域点击即可复制，历史记录可导出 CSV。

- **📒 历史记录**

  原文与译文按时间保存，支持搜索、分页（20 / 30 / 50 / 100 条每页）、导出 CSV 和清空。

  <div align="center">
    <img src="images/history.png" alt="历史记录" width="90%" />
    <br>
    <em style="font-size:80%">历史记录</em>
  </div>

- **🎞️ 日志卡片**

  在主界面展开最近几句的转写记录，方便对照上下文；显示数量由设置里的「显示句数」决定。

  <div align="center">
    <img src="images/log_cards.png" alt="日志卡片" width="90%" />
    <br>
    <em style="font-size:80%">日志卡片</em>
  </div>

## 托盘与启动行为

- **最小化到托盘**：点主窗口的最小化按钮，窗口隐藏、托盘出现程序图标，任务栏不再占位。左键单击托盘图标或右键菜单 **显示主窗口** 可以恢复窗口，**退出** 退出程序。
- **后台继续工作**：最小化到托盘只是隐藏窗口，翻译和悬浮字幕都在照常运行。
- **启动时最小化**：设置页「上下文感知」下方的开关。打开后每次启动都直接收进托盘，适合开机自启、只在悬浮窗里看字幕的用法。
- **启动即暂停**：每次启动默认是暂停翻译的状态，主窗口译文区显示 `[已暂停]`，历史记录只写原文。点标题栏的暂停按钮即可恢复，这个状态只在本次运行内有效。如果希望启动就翻译，可以把 `src/Translator.cs` 里的 `LogOnlyFlag` 默认值改回 `false` 后重新编译。

## 悬浮字幕的模式与记忆

打开悬浮窗时会恢复上次的显示布局：显示哪些卡片（仅原文 / 原文 + 译文 / 仅译文）和上下顺序。第一次使用默认只显示原文（`SubtitleOnly`）。点控制栏的模式按钮按下面的顺序循环：

`仅原文` → `原文 + 译文` → `仅译文` → `仅原文` …

旁边的按钮用于上下对调。原文和译文两个区域各自最多显示 3 行，超出后丢掉最旧的内容、保留最新的 3 行，所以长句不会把窗口越撑越高。

布局保存在 `setting.json` 的 `OverlayWindow.OnlyMode` 与 `OverlayWindow.SwitchMode` 字段里，切换后立即写入；悬浮窗的开 / 关状态保存在 `OverlayWindowEnabled`，下次启动会自动恢复；窗口位置也会一并恢复。

## 下载和运行

已验证环境为 **Windows 11 x64**。

1. 从 [Releases](https://github.com/Courtoisrl/LiveCaptions-Translator/releases/latest) 下载发布包。
2. 建议放在可写目录（例如 `D:\Software\LiveCaptions-Translator`），程序会在所在目录生成配置和历史记录。
3. 双击 `LiveCaptionsTranslator.exe` 启动。

发布包分两种：带运行时（体积大，无需装 .NET）和不带运行时（体积小，需要本机有 .NET 8 桌面运行时）。

### 第一次使用前的准备

Windows LiveCaptions 需要先能正常工作：

1. 打开 Windows 实时字幕（`Win + Ctrl + L`），按提示下载语音识别文件和语言包。
2. 点 LiveCaptions 里的 **⚙️ 齿轮** → **Position** → **Overlaid on screen**。这一步不做的话，隐藏 LiveCaptions 后屏幕上会残留显示问题。
3. 在本程序设置页点 **隐藏** 按钮隐藏 LiveCaptions，之后就用本程序看字幕。
4. 需要换识别源语言时，在设置页点 **显示** 把 LiveCaptions 调出来，在里面修改。

> Windows 11 24H2 之后，源语言只能在 LiveCaptions 里改。

## 配置翻译服务

在 **设置 → 翻译接口** 里选择服务，再填对应参数。Google 和 Google2 不需要配置，其他服务需要先填好 key 或地址才能使用。

以 DeepSeek（OpenAI 兼容接口）为例：

| 字段 | 示例 |
| --- | --- |
| API URL | `https://api.deepseek.com/chat/completions` |
| 模型 | `deepseek-chat`（以服务商文档为准） |
| API Key | 你自己的 key，只保存在本地 `setting.json` |
| 目标语言 | `zh-CN` |

- 目标语言使用 BCP 47 标签（如 `zh-CN`、`ja-JP`），下拉框允许直接输入自定义值。
- 「接口间隔」控制调用频率：字幕每变化 N 次才调用一次翻译接口，数值越小越实时、消耗也越大。
- 「上下文感知」打开后会附带最近 N 句（由「上下文数」决定）作为上下文，长句和指代更准，但消耗更多 token。
- 「显示句数」同时决定日志卡片数量和悬浮窗最多显示几句；「上下文数」不能小于它，否则程序会自动调整。

## 数据保存

程序目录下会生成：

- `setting.json`：所有设置、API Key，以及 `StartMinimized`、`OverlayWindowEnabled` 这类开关状态。**里面含有 key，不要随意分享。**
- `translation_history.db`：SQLite 历史记录，包含原文和译文。

暂停翻译（仅记录原文）期间，历史记录里的译文和 API 字段会写成 `N/A` / `LogOnly`，原文照常保存。

## 从源码构建

需要 Windows 和 .NET 8 SDK，在仓库根目录执行：

```powershell
# 编译
dotnet build LiveCaptionsTranslator.csproj -c Debug

# 发布单文件（自包含，目标机器无需安装运行时）
dotnet publish LiveCaptionsTranslator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish
```

如果要发布体积更小的框架依赖版本，把 `--self-contained` 改成 `false`，目标机器需要有 .NET 8 桌面运行时。

## 已知限制

- 依赖 Windows LiveCaptions，因此只支持 Windows 11 22H2 及以上。
- 翻译质量取决于所选引擎；暂停状态下不会产生译文，属于预期行为。
- 悬浮窗默认宽度 650（可拖动左右边缘临时调整），高度随内容自动变化；原文、译文各自最多 3 行，超出部分会被丢掉而不是滚动保留全文。
- 悬浮窗的布局在切换时立即保存；如果程序被强制结束，最后一次尚未保存的窗口位置可能丢失。
- 网络延迟、接口限流都会影响字幕的实时性；翻译接口报错时，译文区会显示错误提示，原文不受影响。
- 最小化到托盘后程序仍在后台运行，需要完全退出请用托盘的 **退出**。

## 致谢与许可证

原始项目由 [SakiRinn](https://github.com/SakiRinn) 及贡献者开发，本仓库是在其基础上维护的版本，遵循 [Apache-2.0](LICENSE) 许可证并保留原作者版权。

界面使用 [WPF-UI](https://github.com/lepoco/wpfui)，托盘图标来自项目自带的 `src/LiveCaptions-Translator.ico`。
