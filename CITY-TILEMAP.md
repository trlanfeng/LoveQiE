# 城市自动地形与汽车素材

人行道现在使用 Unity 官方 **2D Tilemap Extras 3.1.3 / Rule Tile**。选择一个画笔即可连续绘制街区，外圆角、内凹角、直边、单格、T 形、十字、细桥和封闭环自动匹配。擦除格子会重新计算周围的边角。

`city_terrain.png` 已从 256 种人行道缩减为 **47 种 Blob 人行道**。这 47 个形状覆盖所有 256 种八邻格状态：只有两条相邻直边都是人行道时，才需要检查夹角处的对角格。无需手动选角，也不把 47 张图分别作为画笔。所有形状保持相同光照方向，不用旋转／镜像来节省图片。

## 直接编辑现有关卡

1. 打开 `Assets/Resources/Maps/Scenes/Scene 1.prefab`（或其他 Scene N）进入 Prefab Mode。
2. 打开 **Window > 2D > Tile Palette**，选择 **CityTerrainPalette**。
3. 将 **Active Tilemap** 设为该关卡的 **Obstacles**。
4. 选中调色板中唯一的人行道画笔，直接绘制。新画入的格子成为障碍街区，城市道路、人行道、建筑预览自动更新。
5. 用橡皮擦去格子，会恢复道路并自动更新周围圆角。**Ctrl+Z / Ctrl+Y** 撤销、重做也会同步预览。保存 Prefab 即可用于游戏。

第一次在旧关卡绘制或擦除时会自动创建城市预览层。若只想先查看效果，选中根节点，执行 **Tools > City Tilemap > Rebuild selected level preview**；此命令保留作初始化／手动恢复，不是每次画完都要执行的步骤。也可以直接打开已经准备好的 `Assets/CityTiles/CityTilemapDemo.prefab`。

`Obstacles` 是碰撞和布局的唯一来源：非空为障碍，空格为道路。正式关卡请在这一层画，不要把柏油瓦片刷到 Obstacles 上（任何非空瓦片都会阻挡）。旧关卡中的原有障碍 Tile 与新画笔可以混用，不必重新画 99 关。

`City Roads`、`City Sidewalks`、`City Markings`、`City Buildings`、`City Decorations` 是从 Obstacles 派生的视觉层。编辑 Obstacles 或加载关卡时它们会自动更新；建筑按位置分配，不在这些生成层保存逐格手工美术定制。玩家碰撞、镜像控制、终点与原关卡布局继续使用原逻辑。

## 在独立 Tilemap 上使用自动地形

新建 Grid 和 Tilemap，使用 **CityTerrainPalette** 的同一画笔即可自动连接，不需要 NativeLevel 或城市预览组件。人行道 Tilemap 下方另铺一层柏油，再单独绘制建筑／装饰。推荐排序：Roads=-8、Sidewalks=-6、Markings=-5、Buildings=2、Decorations=3。

画笔连接同一个 Tilemap 中的所有非空格，因此不同种类的装饰应放在各自的层。独立地图边缘按空地处理；NativeLevel 的地图外按障碍处理，与游戏移动边界一致。Rule Tile 默认 Grid Collider；独立地图如需物理碰撞，还需在对应对象上添加 TilemapCollider2D。

## 资源入口

| 文件 | 内容 |
| --- | --- |
| `Assets/CityTiles/Tiles/CitySidewalk.asset` | 一个自动地形画笔，47 条可在 Inspector 查看和编辑的 Rule Tile 规则 |
| `Assets/CityTiles/CityTerrainPalette.prefab` | 日常画地形使用的单画笔调色板 |
| `Assets/CityTiles/CityPalette.prefab` | 自动人行道、柏油、标线、建筑与装饰，共 37 个画笔 |
| `Assets/CityTiles/CityBuildingsPalette.prefab` | 8 栋建筑和 4 个装饰 |
| `Assets/CityTiles/Textures/city_terrain.png` | 1056×1188，71 个切片：47 人行道、4 柏油、16 标线、2 斑马线、2 停车位 |
| `Assets/CityTiles/Textures/city_buildings.png` | 8 栋建筑 |
| `Assets/CityTiles/Textures/city_props.png` | 树、路灯、长椅、花坛 |
| `Assets/CityTiles/Textures/city_cars.png` | 红、绿汽车各 4 帧 |
| `Assets/CityTiles/city-atlas.json` | 91 个 Sprite 的名称和切片坐标，左下原点 |
| `Assets/Resources/City/CityTheme.asset` | 正式游戏的城市主题 |
| `CityReports/blob47/complex-corners.png` | L、T、十字、环形、斜向接触和单格细桥的拼接预览 |

所有切片为 **128×128 / 128 PPU**，一个切片对应 1×1 世界格。中心锚点、Full Rect、Bilinear、无 mipmap、无压缩。每边延展 2px，图集步长 132px。Sprite Editor 已切好；手动切片参数是大小128、间距4、偏移2。

人行道文件名仍用道路邻域位掩码 N1/E2/S4/W8/NE16/SE32/SW64/NW128，但仅保留 47 个规范化编号，所以编号不连续。编号供生成器和规则引用，画地图不用计算这些值。地形外圆角半径18px，使用统一的五种四分之一格轮廓拼合；路沿高光、立面色带、阴影与石板纹理保持固定光照方向。

## 汽车与重新生成

红绿汽车仍使用与建筑匹配的柔和手绘风格。`Assets/Prefabs/player_red.prefab` 和 `player_green.prefab` 引用四帧序列，20 FPS；移动时播放并转向，停下保持方向、回到第一帧。

1. 安装 `Tools/requirements.txt`，运行 `python Tools/build_city_tiles.py` 生成四张图集和资源总览。
2. 运行 `python Tools/preview_city_terrain.py` 检查全部邻域、相邻格和复杂形状，报告输出到 `CityReports/blob47`。
3. 在团结编辑器执行 **Tools > City Tilemap > Import atlases and configure city**，更新切片、47条规则、37个画笔、3个调色板、主题、汽车和样例。这个生成命令会覆盖这些生成资源的手工定制；日常画关卡不需要执行。
4. 只检查资源可执行 **Tools > City Tilemap > Validate city assets and all levels**。

生成器保留同名 Sprite 的 ID，移除过时的 256 个独立 sidewalk Tile 资产。正式 99 关的障碍资产没有改写。CitySidewalkRuleTile 继承官方 RuleTile，仅扩展旧障碍格的兼容和有限地图边界；自动邻格刷新使用官方实现。CityLivePreview 监听 Obstacles 编辑和撤销／重做，将城市预览同步到关卡。

本次素材调整使用本地像素几何生成，没有重新调用图片服务。建筑、汽车原画和城市参考图仍保存在 output/imagegen。

## 验证入口

CityTilemapSetup.ConfigureAndBuildDevelopment 构建 Windows 开发版；以 `-batchmode -nativeSmoke -smokeReport <绝对路径>` 运行自动通关回归。CityTilemapSetup.BuildRelease 构建发布版。

本版报告以 CityReports/blob47 为准，旧 corner-connectivity 目录仅保存之前256格方案的历史结果。编辑器验证实际调用 Tilemap.SetTile、擦除、Undo、Redo 检查 Rule Tile 和预览；不等同于人工在 GUI 中逐笔验收。
