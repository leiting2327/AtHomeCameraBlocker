# 掌上看家一键断网工具

> **本软件永久免费且开源。** 采用 MIT 许可证——你可以自由使用、修改和再分发，永远如此。无付费版、无专业版、无任何隐藏收费。

一个轻量级、单文件的 Windows 工具，通过系统自带的 Windows 防火墙，**一键断开 / 恢复**掌上看家采集端（`AtHomeVideoStreamer.exe`）的网络连接。

开机时静默运行并默认断网，无托盘图标、无任务栏显示。随时双击 `.exe` 打开控制面板切换网络状态。

---

[English](README.md) | 中文

## 功能特性

- **一键断网 / 恢复**：为目标程序添加或移除 Windows 防火墙出站阻止规则。
- **单文件 EXE**：无需安装、无依赖、无需额外运行时（使用 Windows 10/11 自带的 .NET Framework 4.x）。
- **开机自启**：注册一个登录时触发的计划任务，以静默模式运行并应用断网规则后退出，登录时不弹 UAC 提示。
- **无托盘、无任务栏**：后台运行时完全不可见，仅在手动双击 `.exe` 时才弹出控制面板。
- **单实例**：全局互斥锁防止重复启动。
- **中文界面**：所有界面文字均为中文。
- **自动检测**：可自动定位 `AtHomeVideoStreamer.exe`，也可手动浏览选择任意目标 `.exe`。

## 工作原理

本工具**不会**结束摄像头进程。它调用 `netsh advfirewall firewall` 为目标程序创建一条出站**阻止**规则：

```
netsh advfirewall firewall add rule name="AtHomeNetBlocker_Rule"
    dir=out program="C:\路径\AtHomeVideoStreamer.exe"
    action=block profile=any enable=yes
```

由于规则持久保存在 Windows 防火墙中，程序**不需要**一直运行。开机时静默模式（`/silent`）确保规则存在后立即退出。

恢复网络只需删除该规则：

```
netsh advfirewall firewall delete rule name="AtHomeNetBlocker_Rule"
```

## 系统要求

- Windows 10 / 11（32 位或 64 位）
- 管理员权限（修改 Windows 防火墙规则必需，`.exe` 会自动请求提权）

## 使用方法

1. 双击 `AtHomeCameraBlocker.exe`（同意管理员权限提示）。
2. 点击 **自动检测** 定位 `AtHomeVideoStreamer.exe`，或点击 **浏览...** 手动选择。
3. 点击 **断开网络** 断网，或 **恢复网络** 恢复连接。
4. 如需每次开机自动断网，勾选 **开机自启**。
5. 关闭窗口即退出程序，防火墙规则继续生效。

### 命令行参数

| 参数 | 行为 |
|---|---|
| *(无)* | 打开中文控制面板。 |
| `/silent` 或 `/autostart` | 静默应用断网规则后退出，供开机计划任务调用。 |

## 从源码构建

需要 .NET Framework 的 C# 编译器（`csc.exe`），Windows 系统自带：

```bat
build.bat
```

或手动编译：

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe /platform:anycpu /optimize+ ^
  /out:AtHomeCameraBlocker.exe ^
  /win32icon:src\app.ico ^
  /win32manifest:src\app.manifest ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.dll ^
  src\Program.cs
```

输出为单个 `AtHomeCameraBlocker.exe`（约 15 KB）。

## 项目结构

```
AtHomeCameraBlocker/
├── README.md            （英文说明）
├── README.zh-CN.md      （中文说明，本文件）
├── LICENSE              （MIT 许可证）
├── .gitignore
├── build.bat            （一键编译脚本）
└── src/
    ├── Program.cs       （完整源码，C# 5 / .NET Framework 4.x）
    ├── app.manifest     （请求管理员权限）
    └── generate-icon.ps1（可选：重新生成自定义图标）
```

## 许可证

[MIT License](LICENSE)——永久免费，开源。
