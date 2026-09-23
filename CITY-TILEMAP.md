# 城市 Tilemap 图集与新版小汽车

打开 `Assets/main.unity` 运行即可看到城市道路、街区建筑和重新绘制的红绿小汽车。地图仍使用原来 99 关的格子布局、镜像控制、碰撞和终点规则。

## 资源入口

| 文件 | 内容 |
| --- | --- |
| `Assets/CityTiles/Textures/city_terrain.png` | 40 个切片：4 种柏油路面、16 种人行道路沿组合、16 种道路连接标线、2 种斑马线、2 种停车位 |
| `Assets/CityTiles/Textures/city_buildings.png` | 8 栋建筑：砖红/蓝灰/鼠尾草绿屋顶住宅、平顶公寓、咖啡店、面包店、诊所、商店 |
| `Assets/CityTiles/Textures/city_props.png` | 树、路灯、长椅、花坛，4 个透明装饰 |
| `Assets/CityTiles/Textures/city_cars.png` | 红、绿小汽车各 4 帧，行驶序列帧 |
| `Assets/CityTiles/city-atlas.json` | 切片名称及像素坐标清单，坐标原点在左下方 |
| `Assets/CityTiles/Tiles/` | 52 个原生 Unity Tile 资源，已经绑定 Sprite |
| `Assets/CityTiles/CityPalette.prefab` | 全部地面、建筑和装饰的 Tile Palette |
| `Assets/CityTiles/CityBuildingsPalette.prefab` | 单独的建筑和装饰 Tile Palette |
| `Assets/CityTiles/CityTilemapDemo.prefab` | 已经拼好的第一关城市样例，可在 Prefab Mode 中查看各层 |
| `Assets/Resources/City/CityTheme.asset` | 正式游戏使用的城市主题配置 |

`CityReports/city-atlas-overview.png` 是带名称的资源总览，背景仅用于展示透明素材，不属于游戏切片。`CityReports/city-cars-driving.gif` 是新汽车与建筑组合的动画预览。

## 切片参数

- 每张切片 **128×128**，**128 Pixels Per Unit**，对应一个 1×1 世界单位的地图格。
- 中心锚点 `(0.5, 0.5)`，Full Rect；Bilinear 过滤，无 mipmap，无压缩。
- 每个切片四周有 **2 像素边缘延展**，图集单元间距为 **132**。已保存 Sprite Editor 切片，不需要手动切图。
- 手工重新切片时使用偏移 `(2,2)`、切片大小 `(128,128)`、间距 `(4,4)`；不要按无间隔 128 网格切图。
- 路面为不透明背景；标线、建筑、装饰和汽车保留透明通道。
- 道路标线与人行道的后缀为连接掩码：北=1、东=2、南=4、西=8，相加后补成两位数。例如 `lane_05` 是南北，`lane_10` 是东西，`lane_03` 是北东转角，`lane_15` 是十字；`sidewalk_03` 在北、东两边有路沿。
- 路面 4 种变化的四周像素已做精确接缝检查；道路标线是独立透明层，可按需要擦除或换成斑马线。
- 人行道采用参考图中的抬高路台效果：约 22 像素外圆角、7 像素石材压顶、5 像素南侧立面，搭配左上方高光和贴地阴影。圆角切去的区域带柏油底色；连续街区内部边不会加端盖。原有 16 种边掩码、切片坐标及 Tile 引用保持一致。这组四邻接切片支持街区外圆角，未扩展为包含对角判定的内凹圆角系统。

## 用 Tile Palette 编辑

1. 打开 **Window > 2D > Tile Palette**，选择 `CityPalette` 或 `CityBuildingsPalette`。
2. 在新建的 Grid 中分别创建 `Roads`、`Sidewalks`、`Markings`、`Buildings`、`Decorations` Tilemap。
3. 建议 Sorting Order 分别为 `-8, -6, -5, 2, 3`，玩家为 `20`。每个建筑的图像完整位于一个格内，不遮挡邻格车辆。
4. 先铺 asphalt 或 sidewalk，再在对应层放置标线、建筑与装饰。建筑 Tile 默认 Grid Collider，其他 Tile 不带碰撞；仅 Tile 设置不会自动创建 TilemapCollider2D，独立新地图需要自行给障碍层添加组件并接入关卡逻辑。

正式游戏的地图编辑继续使用 `Assets/Resources/Maps/Scenes/Scene N.prefab` 中的 **Obstacles** 层：非空格阻挡移动，空格可走。在 Obstacles 上可用 `CityBuildingsPalette` 绘制建筑 Tile 来增加阻挡，擦除 Tile 即可恢复通路。注意：当前格子移动判定依据 `HasTile`，所以在 Obstacles 上刷树、路灯等任何非空 Tile 也会阻挡，不以 Tile 自身 Collider Type 决定。

选中关卡根节点或 Obstacles，执行 **Tools > City Tilemap > Rebuild selected level preview**，可以在编辑器内生成城市预览。运行时 `CityLevelVisuals` 根据 Obstacles 重新生成道路、人行道、建筑与装饰，障碍层本身只隐藏 Renderer，保留 Tile 数据和碰撞。中央隔离带使用树、路灯、花坛、长椅。

这是**基于障碍格生成的默认城市外观**：建筑种类按位置确定，所有正式关卡在载入时重建；不是实时 RuleTile 笔刷。若要逐格指定建筑外观，在独立地图/样例中手工编辑五个视觉层即可；正式关卡要长期保留这种手工外观，需要调整 `CityLevelVisuals.Rebuild` 的生成规则。单独给运行时生成层刷图不会改变碰撞，也会在下次加载时被重新生成。

## 汽车风格与动画

两辆车已改为与建筑相配的柔和手绘风格：砖红和鼠尾草绿车漆、细描边、哑光质感、蓝灰车窗和暖色金属细节。`Assets/Prefabs/player_red.prefab` 和 `player_green.prefab` 已引用新图集中的四帧。

`Car Sprite Animator` 的 `Drive Frames` 可替换为任意长度的帧列表，`Frames Per Second` 默认 20。移动时切换帧、根据实际位移转向，停下保留朝向并回到第 0 帧，重开恢复朝上。新帧由同一张原画处理轮胎纹理及轻微悬挂变化得到。原版高饱和汽车仍保存在 `Assets/Images/Cars`，当前预制体不再使用它们。

## 来源与重新生成

建筑和汽车通过 imagegen CLI / `gpt-image-2`，使用会话授权的当前 base URL 和 key，以 `output/imagegen/city-game-preview.png` 为风格参考重新生成。原画为 `city-buildings-source.png` 与 `city-cars-source.png`，完整提示词分别为 `city-tiles-buildings-prompt.txt` 与 `city-cars-prompt.txt`。本次服务返回的原画已含透明通道，处理时保留透明边缘并去除极低 alpha 杂点。

道路材质采样自批准的城市预览；路面接缝、路沿、标线、打包边距通过脚本按精确像素规则处理。图集不是直接把整张概念图等分裁切，因此不会带入被建筑遮挡的道路或概念图里变化过的关卡结构。凭据未保存到项目。

1. 安装 Pillow，运行 `python Tools/build_city_tiles.py` 重新打包 4 张图集并生成总览。
2. 在团结编辑器执行 **Tools > City Tilemap > Import atlases and configure city**，创建/更新切片、Tile、主题、调色板、样例和汽车引用；同名切片保留 Sprite ID。
3. 此设置命令会重建主题配置、调色板和样例并重置汽车为 4 帧/20 FPS。手工定制这些生成资产后不要随意重跑。
4. 只验证可执行 **Tools > City Tilemap > Validate city assets and all levels**。

批处理入口：`CityTilemapSetup.ConfigureAndBuildDevelopment` 生成 `Builds/CityValidation/LoveQiE.exe`；运行参数 `-batchmode -nativeSmoke -smokeReport <报告绝对路径>` 执行自动回归。`CityTilemapSetup.BuildRelease` 生成 `Builds/Windows/LoveQiE.exe`。

`CityReports` 保存图像接缝检查、Unity 图集与全部关卡结构检查、构建结果和运行回归材料。旧 `MigrationReports`、`CarReports` 保留旧版本报告，城市版本应以 `CityReports` 为准。

本次验证环境为团结引擎 1.6.13 / 2022.3.61t14。已完成 60 个切片和两个 Tile Palette 的资产检查、99 关 / 20,196 个格子的编辑器结构检查，以及 Windows 实际游戏中的 48,030 项检查、99/99 关自动通关，0 个运行错误。`CityReports/runtime-scene-1.png` 为游戏实际摄像机渲染。Tile Palette 资产已生成并验证，未进行人工 GUI 笔刷操作验收。
