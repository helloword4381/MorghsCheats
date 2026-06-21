# Morgh's Cheats v3.1

《骑马与砍杀2：霸主》(Mount & Blade II: Bannerlord) 作弊模组-1.3.15

## 功能特性

### 💰 资源管理
- **金币/声望/影响力一键增加** - 通过 MCM 菜单设置数量并点击按钮
- **一键全属性满级** - 将所有英雄的属性、技能、专长加满

### 🤝 关系管理
- **伙伴好感度→100** - 将所有家族伙伴好感度设为100
- **领主好感度+N** - 增加本王国所有领主的好感度

### ⚡ 自动功能
- **锁3倍速** - 大地图始终以3倍速运行
- **总督自动建造** - 城市总督自动建造建筑
- **自动换装** - 为所有伙伴装备最强骑射套装
- **伙伴/部队上限加成** - 增加家族可招募人数

### 🔫 战场作弊
- **F6 AI托管+快进** - 战场上按F6启用AI托管并加速
- **小键盘1~6召唤精英** - 召唤各国精英兵种
- **HP倍率调整** - 调整主角和士兵的HP倍率

### 🎖️ 特殊功能
- **杀敌晋升** - 兵种累计击杀达到阈值后自动晋升为家族伙伴NPC
- **自由相机** - 战场上启用自由相机视角

## 安装方法

### 依赖模组（必须安装）
1. **Bannerlord.Harmony** - .NET运行时补丁库
2. **Bannerlord.ButterLib** - 通用工具库
3. **Bannerlord.UIExtenderEx** - UI扩展框架
4. **Bannerlord.MBOptionScreen** - 设置界面框架 (MCMv5)

### 安装步骤
1. 下载最新 Release 的 `MorghsCheats.dll`
2. 将 DLL 文件放入：
   ```
   Mount & Blade II Bannerlord\Modules\MorghsCheats\bin\Win64_Shipping_Client\
   ```
3. 确保以上4个依赖模组已安装在游戏目录
4. 启动游戏，在模组列表中启用 Morgh's Cheats

## 编译说明

### 环境要求
- .NET Framework 4.7.2
- Visual Studio 2019 或更高版本
- Bannerlord 游戏目录

### 编译步骤
1. 克隆仓库：
   ```bash
   git clone https://github.com/helloword4381/MorghsCheats.git
   ```

2. 打开 `MorghsCheats-NEW-qikan-1.3.15\MorghsCheats.csproj`

3. 修改 `.csproj` 中的游戏目录路径：
   ```xml
   <PropertyGroup>
     <GameDir>D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord</GameDir>
   </PropertyGroup>
   ```

4. 编译项目：
   ```bash
   dotnet build -c Release
   ```

5. 编译输出位于：
   ```
   bin\Win64_Shipping_Client\MorghsCheats.dll
   ```

## 自动发布说明

本仓库使用 **GitHub Actions** 实现自动编译和发布：

### 触发条件
推送 `v*` 格式的标签时自动触发，例如：
```bash
git tag -a v3.1.0 -m "Release v3.1.0"
git push origin v3.1.0
```

### 自动流程
1. ✅ 检出代码
2. ✅ 设置 MSBuild 和 NuGet
3. ✅ 还原依赖包
4. ✅ 编译 Release 版本
5. ✅ 创建 GitHub Release
6. ✅ 上传 `MorghsCheats.dll` 到 Release

### 查看构建状态
- 访问 [Actions 页面](https://github.com/helloword4381/MorghsCheats/actions) 查看构建进度
- 构建成功后，访问 [Releases 页面](https://github.com/helloword4381/MorghsCheats/releases) 下载 DLL

## 使用说明

### MCM 菜单操作
1. 进入游戏
2. 按 `Esc` 打开菜单
3. 点击 "Mod Options"
4. 找到 "Morgh's Cheats"
5. 使用各项功能

### 快捷键说明
| 快捷键 | 功能 |
|--------|------|
| `F6` | AI托管+快进 |
| `小键盘1~6` | 召唤各国精英兵种 |

## 项目结构

```
MorghsCheats-NEW-qikan-1.3.15/
├── src/
│   ├── MorghsCheatsSubModule.cs       # 模组入口
│   ├── CheatService.cs                # 作弊服务（一键加点等）
│   ├── ModSettings.cs                 # 运行时设置
│   ├── Patches/
│   │   └── CheatPatches.cs          # Harmony 补丁（锁速、上限等）
│   ├── Settings/
│   │   ├── MCMConfig.cs             # MCM 设置界面
│   │   └── MCMConfigProvider.cs     # 安全读取 MCM 配置
│   └── Utils/
│       └── LogService.cs             # 调试日志
├── MorghsCheats.csproj              # 项目文件
└── build.ps1                        # 编译脚本
```

## 技术架构

- **Harmony 补丁** - 使用 Harmony 库动态修改游戏方法
- **MCMv5 设置界面** - 使用 MBOptionScreen 提供友好的设置界面
- **CampaignBehavior** - 使用官方行为系统处理游戏事件
- **MissionBehavior** - 使用官方任务系统处理战场逻辑

## 常见问题

### Q: 模组加载后没有 MCM 菜单？
**A:** 确保已安装 `Bannerlord.MBOptionScreen` 模组，并在游戏启动前启用。

### Q: 编译时提示缺少 DLL？
**A:** 确保 `.csproj` 中的游戏目录路径正确，且游戏已安装。

### Q: 战场上按 F6 没有反应？
**A:** 确保在战场内（Mission 进行中），且已启用 AI 托管功能。

## 贡献指南

欢迎提交 Issue 和 Pull Request！

### 开发环境设置
1. Fork 本仓库
2. 创建开发分支：
   ```bash
   git checkout -b feature/your-feature
   ```
3. 提交更改：
   ```bash
   git commit -m "Add: your feature"
   ```
4. 推送分支：
   ```bash
   git push origin feature/your-feature
   ```
5. 创建 Pull Request

### 代码规范
- 使用 4 空格缩进
- 所有公共方法添加 XML 注释
- 使用 `LogService` 记录调试信息
- 所有异常处理必须记录日志

## 许可证

本项目使用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情

## 致谢

- **Bannerlord 模组社区** - 提供 Harmony、ButterLib 等基础库
- **MCM 团队** - 提供强大的设置界面框架
- **所有贡献者** - 感谢你们的付出

## 联系方式

- **GitHub Issues:** [提交问题](https://github.com/helloword4381/MorghsCheats/issues)
- **邮箱:** jiangflow@foxmail.com

---

**⚠️ 免责声明：** 本模组仅供学习和娱乐使用，使用作弊可能影响游戏体验，请谨慎使用。
