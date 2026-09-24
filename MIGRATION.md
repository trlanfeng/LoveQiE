# LoveQiE — 原生 Tilemap 迁移

已将原 rotorz 的 100 个关卡 prefab 转换为 Unity 原生 Grid / Tilemap / TilemapRenderer / TilemapCollider2D。正式流程使用 Scene 1～Scene 99；Scene.prefab 保留为原模板。

## 直接运行

- 本机验证环境：团结引擎 1.6.13，底层版本 2022.3.61t14。
- 编辑器打开 `Assets/main.unity`，点击 Play，从首页点击「开始游戏」进入关卡选择。界面与计时规则见 [GAME-UI.md](GAME-UI.md)。
- Windows 成品位于 `Builds/Windows/LoveQiE.exe`。分发时复制整个 Windows 文件夹。
- WASD 或方向键控制；左右输入对两个角色呈镜像，上下同向；R 重开当前关卡。
- 两个角色分别到达顶部第 7、9 列的第 1 行后弹出计时星级结算，点击「下一关」继续；第 99 关后可返回选关。

## 编辑关卡

1. 打开 `Assets/Resources/Maps/Scenes/Scene N.prefab`。
2. 选择其 `Obstacles` 子对象。
3. 打开 Window > 2D > Tile Palette，选择 `WoodPalette`。
4. 使用笔刷绘制、橡皮擦除并保存 prefab。

已启用 2D Tilemap Editor 和 2D Sprite 包。64 个原图集切片在 `Assets/NativeTiles/Textures`，原生 Tile 资源在 `Assets/NativeTiles/Tiles`，调色板为 `Assets/NativeTiles/WoodPalette.prefab`。

每张图的逻辑范围是 17 列、12 行。旧坐标 `(column,row)` 映射到原生 cell `(column,-row-1,0)`，单格 1 个世界单位。所有非空障碍格均阻挡移动，这与旧 TileManager 的实际行为一致。草地是主场景中的独立 Tilemap，不参与障碍判断。普通 Tile 保留原关卡自动拼接后的具体瓦片；继续编辑时边角由调色板选取，没有重建 Rotorz 的自动拼接规则。

## 修改范围

- 原始地图布局、瓦片索引、美术图片、草地变体、角色和终点图案保留。
- NativeLevel 负责地图范围及坐标换算；GameManager 使用 Resources 加载新 prefab；CharactorManager 使用协程实现原有格子移动和镜像方向。
- 移除了对 rotorz 与旧 DOTween DLL 的运行依赖，旧依赖和笔刷移至 Assets 以外的备份目录。
- 修复原边界判断把 column==columns / row==rows 视为有效的问题。
- 第 78、83、88、93、98 关原资产存在编辑器偏移；资产保留该变换，游戏实例统一对齐主场景地板。
- 移动期间禁止重叠输入；一方遇墙时另一方仍可移动。统一由 GameManager 接收输入，避免角色在不同 Update 时机不同步。
- 摄像机根据窗口比例完整显示地图；恢复有效构建场景；增加重开与最终通关状态。

## 验证材料

- `MigrationReports/extraction.json`：100 张地图与 10,797 个非空格提取统计。
- `MigrationReports/unity-validation.txt`：Unity 实例化全部新 prefab，逐格对比 20,400 格、瓦片资源、坐标换算、边界及旧依赖检查。
- `MigrationReports/solvability.txt`：独立 BFS 求解全部 99 个正式关卡。
- `MigrationReports/runtime-smoke.txt`：Windows 程序使用真实移动协程执行全部通关路线，并检查阻挡、镜像移动、重开、自动切关、最终完成和运行错误。
- `MigrationReports/runtime-scene-1.png`：运行程序中的实际摄像机渲染。
- `MigrationReports/build.txt` 与 `build-messages.txt`：最后一次 Windows 构建结果。
- `MigrationReports/reference-maps`：根据旧 prefab 数据与旧图集独立拼接的 100 张参考图。

## 备份与重新生成

完整修改前备份是 `MigrationBackup/before-native-tilemap.zip`，包括原 Assets、ProjectSettings 和 Packages。失效插件另存于 `MigrationBackup/LegacyAssets`。不要将 LegacyAssets 拖回 Assets。

转换工具有意读取不可变 zip 备份，而不读取已转换的新 prefab：

```powershell
python -m pip install -r Tools/requirements.txt
python Tools/export_rotorz.py
python Tools/solve_levels.py
```

然后在编辑器运行 Tools > Native Tilemap > Convert backed-up levels。此操作会用旧备份覆盖已经编辑过的 100 个地图；继续制作关卡后不要随意重跑。

只检查迁移结果可运行 Tools > Native Tilemap > Validate all levels；地图经过有意编辑后，与原备份比较不一致是预期现象。

批处理入口：`NativeTilemapMigration.Convert`（转换），`NativeTilemapMigration.FinishSetup`（设置调色板并生成 Development Build），`NativeTilemapMigration.BuildRelease`（生成正式 Windows 构建）。先退出当前项目编辑器再调用相同安装版本的编辑器可执行文件，配合 `-batchmode -projectPath <项目目录> -executeMethod <入口> -quit -logFile <日志路径>`。

Development Build 支持 `-nativeSmoke -smokeReport <报告绝对路径>`，执行自动回归后退出；普通运行不会启用回归，正式构建不包含回归逻辑。自动求解路线保存在 `Assets/Resources/Maps/solutions.json`。

项目在本机团结引擎完成实际验证；未在另装的国际版 Unity 或移动设备上验证。地图组件和运行 API 使用原生 Unity Tilemap，不再需要 Rotorz。

