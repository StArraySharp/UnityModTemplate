# ADOFAI UnityMod 模板使用说明

## 1. 前置条件

请先安装：

1. Unity `6000.3.10f1`。
2. .NET SDK，并确保 `dotnet --version` 可以运行。
3. ADOFAI 本体。模板需要你本机的游戏 exe，不接受仓库里携带游戏文件。
4. UnityModManager。
5. Git。

ThunderKit 由 `Packages/manifest.json` 声明。第一次打开工程时，Unity 需要能够访问该依赖的 Git 地址。

## 2. 安装模板

如果已经克隆了模板仓库，在模板仓库根目录执行：

```powershell
dotnet new install .
dotnet new list
```

列表中应出现 `adofaimod`。

其他人可以先从 GitHub 克隆模板，再执行同样的安装命令：

```powershell
git clone https://github.com/StArraySharp/ADOFAI-UnityModTemplate.git
dotnet new install .\ADOFAI-UnityModTemplate
dotnet new list
```

`.NET` 模板安装命令不能直接把 GitHub URL 当作模板包；如果发布了 `.nupkg`，也可以下载后直接安装。

发布包也可以安装：

```powershell
dotnet new install .\artifacts\StArraySharp.ADOFAIUnityModTemplate.1.0.0.nupkg
```

## 3. 创建项目

标准命令如下：

```powershell
dotnet new adofaimod `
  -n MyCoolMod `
  -o MyCoolMod `
  -g "C:\Games\ADOFAI\A Dance of Fire and Ice.exe" `
  --author-name "Your Name" `
  --description "My ADOFAI mod" `
  --version "1.0.0"
```

参数含义：

| 参数 | 作用 |
| --- | --- |
| `-n` / `--name` | 项目名、程序集名、命名空间、Mod ID |
| `-o` / `--output` | 生成目录；这是 `dotnet new` 自带参数 |
| `-g` / `--game-path` | ADOFAI exe 的完整路径 |
| `--author-name` | 写入 `Info.json` 和 ThunderKit Manifest；`-a/--author` 被 `dotnet new` 主程序保留 |
| `--description` | 写入 `Info.json` 的 `Description` 和 ThunderKit Manifest；包装脚本提供 `-d` |
| `--version` | 写入 `Info.json` 和 ThunderKit Manifest；包装脚本提供 `-v` |

项目名只能使用字母、数字和下划线，并且不能以数字开头。例如 `MyCoolMod`、`ADOFAI_Mod`、`Mod123` 合法；`My Cool Mod`、`my-mod`、`123Mod` 不合法。

仓库里的 `New-ADOFAIMod.ps1` 会在调用 `dotnet new` 前严格检查这些内容：

```powershell
.\New-ADOFAIMod.ps1 `
  -n MyCoolMod `
  -o MyCoolMod `
  -g "C:\Games\ADOFAI\A Dance of Fire and Ice.exe" `
  -a "Your Name" `
  -d "My ADOFAI mod" `
  -v "1.0.0"
```

## 4. 第一次打开 Unity

请用 Unity Hub 或 Unity Editor 打开生成项目的根目录，不要只打开 `Assets` 文件夹。

首次导入时会发生这些事情：

1. Unity 解析项目设置和 ThunderKit 包。
2. 模板脚本读取 `ProjectSettings/ADOFAI.Template.local.txt`。
3. 脚本检查 exe 是否存在，以及 `<游戏名>_Data/Managed` 是否存在。
4. 脚本自动写入 `Assets/ThunderKitSettings/ThunderKitSettings.asset` 的 `GamePath` 和 `GameExecutable`。
5. ThunderKit Settings 自动打开，并弹出提示。
6. 你点击一次 `Import`。

导入结束后，ThunderKit 会在本地生成：

```text
Packages/A Dance of Fire and Ice/
```

它提供 `Assembly-CSharp.dll`、`UnityModManager.dll` 等开发时需要的引用。它们来自你自己的游戏安装，不应放进模板仓库。

如果第一次打开时 ThunderKit 还没有加载完，等 Unity 完成编译后执行 `Tools > ADOFAI > Reset Template Bootstrap`，脚本会再次检查并配置。

## 5. 项目目录

```text
Assets/
├── Editor/
│   ├── TemplateBootstrap.cs
│   ├── TemplateBootstrap.Editor.asmdef
│   └── BuildMod.cs
├── Scenes/
├── Scripts/
│   ├── <ProjectName>.asmdef
│   ├── Main.cs
│   ├── ModSettings.cs
│   ├── Patches.cs
│   └── ResourceLoader.cs
└── Resources/
    ├── Prefabs/
    ├── Textures/
    ├── Materials/
    ├── Shaders/
    ├── Audio/
    ├── Fonts/
    ├── UI/
    ├── Animations/
    └── ScriptableObjects/
```

项目名会同时替换到：

- Unity `productName`
- `Assets/Scripts/<ProjectName>.asmdef` 的文件名和内部 `name`
- asmdef 的 `rootNamespace`
- C# 命名空间
- `Info.json` 的 `Id`、DLL 文件名和 `EntryMethod`
- ThunderKit Manifest 的 Mod 名称

Unity 打开项目后会自动生成 `.slnx`、`.csproj`、`Library`、`Temp` 等编辑器文件；这些不属于模板源代码，也已经被 `.gitignore` 排除。

## 6. 模板脚本与资源使用

模板自带的脚本是一套最小 Mod 骨架，不包含示例场景、示例按钮或 Hello World 逻辑。你可以按下面的职责理解它们：

### 6.1 `Main.cs`：Mod 入口和生命周期

文件位于 `Assets/Scripts/Main.cs`。`Info.json` 中的 `EntryMethod` 指向 `Main.Load`，因此 UnityModManager 加载 Mod 时会先调用这个方法。

`Main.Load` 会：

1. 保存 UnityModManager 提供的 Mod 信息和日志对象。
2. 读取 `ModSettings.cs` 中的设置。
3. 注册启用、禁用和设置界面的回调。
4. 创建一个属于当前 Mod 的 Harmony 实例。

用户启用 Mod 时，`Main` 会执行：

```text
Harmony.PatchAll()        扫描并应用当前程序集中的 Harmony 补丁
ResourceLoader.LoadAll()  加载 scenes.assets 和 resources.assets
```

如果资源包加载失败，模板会自动撤销刚才应用的补丁，并让 UnityModManager 认为启用失败。用户禁用 Mod 时，模板会撤销当前 Mod 的补丁并释放资源。

一般情况下，不需要修改 `Main.cs`。只有在 Mod 启用或禁用时需要创建、销毁自己的对象，或者需要执行额外初始化时，才在这里增加逻辑。

### 6.2 `ModSettings.cs`：Mod 设置

文件位于 `Assets/Scripts/ModSettings.cs`。这里的 `Settings` 类表示 UnityModManager 中这个 Mod 的设置，不是 ADOFAI 游戏的全局设置。

模板当前没有任何设置字段，所以 UnityModManager 中不会显示示例选项。需要设置时，可以添加字段并在 `OnGUI` 中绘制：

```csharp
using UnityEngine;
using UnityModManagerNet;

namespace MyCoolMod
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public bool EnableFeature = true;

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            EnableFeature = GUILayout.Toggle(
                EnableFeature,
                "Enable feature");
        }

        public void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Save(modEntry);
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public static Settings Load(UnityModManager.ModEntry modEntry)
        {
            return Load<Settings>(modEntry);
        }
    }
}
```

`OnGUI` 负责显示控件，`OnSaveGUI` 负责保存，`Load` 负责下次启动时读取。`Main.Load` 已经自动调用 `Settings.Load`，不需要你手动读取设置。

### 6.3 `Patches.cs`：Harmony 补丁

文件位于 `Assets/Scripts/Patches.cs`。当前的 `Patches` 类只是一个空的提示文件，真正的补丁可以写在这里，也可以放到其他 `.cs` 文件中。

例如，下面的补丁会在目标方法执行前运行：

```csharp
using HarmonyLib;

namespace MyCoolMod
{
    [HarmonyPatch(typeof(SomeGameType), nameof(SomeGameType.SomeMethod))]
    internal static class SomeMethodPatch
    {
        private static void Prefix()
        {
            // 在游戏方法执行前运行
        }
    }
}
```

`Main.cs` 中的 `Harmony.PatchAll` 会扫描整个 Mod 程序集，因此不需要手动注册这个补丁。补丁目标类型必须来自 ThunderKit 导入的 ADOFAI 游戏程序集。

### 6.4 `ResourceLoader.cs`：AssetBundle 资源管理

文件位于 `Assets/Scripts/ResourceLoader.cs`。它负责读取 ThunderKit 构建出的两个文件：

```text
scenes.assets
resources.assets
```

`Assets/Resources/` 是 Unity 工程中的资源目录，最终由 ThunderKit 打包成 `resources.assets`；它不是 Mod 发布后可以直接访问的源代码目录。`ResourceLoader` 使用 AssetBundle 从 Mod 安装目录读取打包后的资源。

它提供四个主要方法：

| 方法 | 用途 |
| --- | --- |
| `LoadAll(modPath)` | 加载 Mod 目录下的两个资源包；任意一个失败都会返回 `false` |
| `LoadAsset<T>(assetName)` | 从 `resources.assets` 加载 Prefab、Texture、AudioClip 等资源 |
| `GetScenePaths()` | 获取 `scenes.assets` 中包含的场景路径 |
| `UnloadAll()` | Mod 禁用时关闭资源包并释放资源 |

加载 Prefab、图片和音频的例子：

```csharp
GameObject panel = ResourceLoader.LoadAsset<GameObject>("MyPanel");
Texture2D icon = ResourceLoader.LoadAsset<Texture2D>("Icon");
AudioClip sound = ResourceLoader.LoadAsset<AudioClip>("ClickSound");
```

资源名称区分大小写，必须与资源打包后的名称一致。如果返回 `null`，请先检查资源是否放在正确目录、ThunderKit 是否重新构建，以及传入的名称是否正确。

场景不能用 `LoadAsset<GameObject>` 加载，而应该先取得场景路径，再交给 Unity 的场景管理器：

```csharp
using UnityEngine.SceneManagement;

string[] scenePaths = ResourceLoader.GetScenePaths();
if (scenePaths.Length > 0)
{
    SceneManager.LoadScene(
        scenePaths[0],
        LoadSceneMode.Additive);
}
```

模板会在 Mod 启用时自动调用 `LoadAll`，在 Mod 禁用时自动调用 `UnloadAll`。因此普通 Mod 不需要手动调用这两个方法。只有在你需要延迟加载、重新加载或自定义资源生命周期时，才需要直接使用它们。

当前模板要求构建结果同时包含两个资源包，即使 Mod 暂时没有资源。如果以后要制作纯代码 Mod，需要同时调整 `ResourceLoader` 和构建窗口的文件检查逻辑。

### 6.5 `<ProjectName>.asmdef`：程序集定义

文件位于 `Assets/Scripts/`，生成项目后会和项目同名，例如 `Creplay.asmdef`。

它决定：

- Mod DLL 的名称。
- C# 默认命名空间。
- 需要引用哪些 ADOFAI 和 UnityModManager DLL。
- ThunderKit 导入完成前是否允许编译 Mod 代码。

其中 `ADOFAI_GAME_IMPORTED` 约束表示只有 ThunderKit 导入 `Assembly-CSharp.dll` 后，Mod 程序集才会启用编译。不要把游戏 DLL 手动复制到项目中。

### 6.6 `Info.json`：UnityModManager 清单

文件位于 `Assets/Info.json`。它告诉 UnityModManager：

- Mod 的 ID 和显示名称。
- 作者、描述和版本。
- 最终 DLL 的名称。
- 启动入口方法。

例如生成 `Creplay` 项目后，入口会是：

```json
"AssemblyName": "Creplay.dll",
"EntryMethod": "Creplay.Main.Load"
```

项目创建参数会自动写入这些字段。通常不需要手动修改 DLL 名称或入口方法。

### 6.7 编辑器脚本

`Assets/Editor/TemplateBootstrap.cs` 和 `Assets/Editor/BuildMod.cs` 只在 Unity 编辑器中运行，不会进入最终 Mod DLL。

`TemplateBootstrap.cs` 负责首次打开工程时读取游戏 exe、配置 ThunderKit、打开 Settings，并在 Import 完成后允许 Mod 程序集编译。更换游戏安装位置后，可以执行 `Tools > ADOFAI > Reset Template Bootstrap` 重新配置。

`BuildMod.cs` 对应菜单 `Tools > Build Mod`。它执行选中的 ThunderKit Pipeline，检查 DLL 和两个资源包是否生成，然后把下面四个文件复制到 `Mods/<ProjectName>/`：

```text
<ProjectName>.dll
Info.json
scenes.assets
resources.assets
```

它不会复制 ADOFAI 游戏 DLL，也不会自动启动游戏。

## 7. 添加 Harmony 补丁

在 `Assets/Scripts/Patches.cs` 或其他脚本中添加补丁。例如：

```csharp
using HarmonyLib;

namespace MyCoolMod
{
    [HarmonyPatch(typeof(SomeGameType), nameof(SomeGameType.SomeMethod))]
    internal static class SomeMethodPatch
    {
        private static void Prefix()
        {
            // Your code here.
        }
    }
}
```

`Main.cs` 在 Mod 启用时执行当前程序集的 `PatchAll`，禁用时只撤销本 Mod 使用的 Harmony 补丁。

## 8. 添加资源

- Unity 场景放到 `Assets/Scenes/`。它们会进入 `scenes.assets`。
- Prefab 放到 `Assets/Resources/Prefabs/`。
- Texture 放到 `Assets/Resources/Textures/`。
- Material 放到 `Assets/Resources/Materials/`。
- Shader 放到 `Assets/Resources/Shaders/`。
- 音频放到 `Assets/Resources/Audio/`。
- 字体放到 `Assets/Resources/Fonts/`。
- UI 资源放到 `Assets/Resources/UI/`。
- Animation 资源放到 `Assets/Resources/Animations/`。
- ScriptableObject 放到 `Assets/Resources/ScriptableObjects/`。

`Assets/Resources/` 下的内容会进入 `resources.assets`。代码通过 `ResourceLoader.LoadAsset<T>(assetName)` 访问资源；模板不预设任何资源名称。

## 9. 构建和部署

1. 确认 ThunderKit 已经导入本机游戏包。
2. 在 Unity 中打开 `Tools > Build Mod`。
3. 选择 ThunderKit Pipeline。
4. 确认输出目录，默认是 `<ADOFAI目录>/Mods/`。
5. 点击 `Build Mod`。

ThunderKit 会构建程序集和两个资源包；模板窗口随后只复制以下四个文件：

```text
<ProjectName>.dll
Info.json
scenes.assets
resources.assets
```

构建失败会同时写入 Unity Console 并弹出错误窗口。窗口不会复制 ADOFAI 游戏 DLL，也不会启动游戏。

## 10. 常见错误

### 游戏路径错误

`--game-path` 必须是 exe 文件本身，而不是游戏目录。确认旁边存在：

```text
<游戏 exe 名称去掉 .exe>_Data/Managed/
```

### Unity 版本不匹配

用 Unity `6000.3.10f1` 打开。不同版本可能改变包解析、程序集导入或 AssetBundle 构建结果。

### ThunderKit 没有生成游戏包

检查 ThunderKit Settings 的 `GamePath` 和 `GameExecutable`，然后点击 `Import`。如果配置窗口没有自动打开，执行 `Tools > ThunderKit > Settings`。

### asmdef 引用缺失

先完成 ThunderKit Import，再等待 Unity 编译。模板的 asmdef 依赖由游戏包提供；不要手动把 ADOFAI DLL 复制到仓库。

### Mod DLL 没有复制到游戏目录

确认输出目录是 ADOFAI 的 `Mods` 目录，并检查 ThunderKit Pipeline 生成了：

```text
ThunderKit/Libraries/<ProjectName>.dll
ThunderKit/AssetBundleStaging/scenes.assets
ThunderKit/AssetBundleStaging/resources.assets
```

如果缺少其中任何一个文件，构建窗口会停止并显示具体缺失项。

## 11. 为什么不能提交游戏 DLL

游戏 DLL 属于本机安装内容，会随着游戏版本、平台和安装位置变化。提交它们会让模板变大、绑定某台机器的游戏版本，并可能造成许可证和分发问题。ThunderKit 已经负责从用户自己的 ADOFAI 安装生成 Unity 可用的游戏包，所以模板只保留引用声明和导入配置。
