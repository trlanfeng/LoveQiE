# 红、绿小汽车角色

打开 `Assets/main.unity` 并 Play。`player_red`、`player_green` 预制体分别使用红色、绿色小汽车，继续使用原来的镜像操作、格子移动、障碍与通关规则。

## 动画行为与编辑

- 车头默认朝上；开始移动时根据实际世界位移转向，支持上下左右四个方向。
- 行驶时按序循环切换 4 张 Sprite，默认 20 FPS；单格移动耗时 0.2 秒。
- 停下后显示第 0 帧，保持最后朝向；撞墙时不播放；重开或切关恢复朝上。
- 禁用角色时取消移动与动画。
- 选中任一角色预制体，在 `Car Sprite Animator` 的 `Drive Frames` 中按顺序拖入自己的序列帧；`Frames Per Second` 可调播放速度。运行组件支持任意帧数，也可只用一张静态图。
- 每一帧应朝上、画布大小一致、中心点一致。当前为 128×128 RGBA PNG，128 Pixels Per Unit，透明边缘，车体小于一格。
- 预制体的 Sprite Renderer 使用第 0 帧，所以编辑模式也能看到小汽车。

## 图片与来源

- 游戏使用：`Assets/Images/Cars/car_red_00.png` ～ `car_red_03.png`，`car_green_00.png` ～ `car_green_03.png`。
- 原始 AI 生成图：`output/imagegen/cars-source.png`。
- 完整生成提示词：`output/imagegen/car-prompt.txt`。
- 帧图集：`output/imagegen/cars-spritesheet.png`（上红下绿，每行从左向右）。
- 动态预览：`output/imagegen/cars-driving.gif`。

素材通过 imagegen CLI、`gpt-image-2`，使用本机当前配置的 base URL 与 key 生成。凭据未写入项目。提示词要求同款红绿小汽车、严格俯视、车头朝上、简洁卡通风格、纯品红底。使用 `Tools/prepare_car_sprites.py` 去除品红背景，并从每辆车的同一张原画生成轮胎纹理滚动和轻微悬挂变化的 4 帧循环，避免逐帧 AI 重绘引起车体漂移。最终 PNG 包含真实透明通道。

重新处理原画：安装 Pillow 后运行 `python Tools/prepare_car_sprites.py`。然后在编辑器执行 `Tools > Player Cars > Configure generated sprites` 导入并重新配置原始 4 帧。此命令会重设两个预制体的帧列表与速度；自定义动画后不必再运行。

## 验证与构建

`CarSpriteSetup.ConfigureAndBuildDevelopment` 为批处理入口，生成 `Builds/CarValidation/LoveQiE.exe`。运行时传入 `-nativeSmoke -smokeReport <绝对报告路径>`，自动检查两车素材、四向转向、真实换帧、停下、阻挡、移动中重开、禁用/启用，并执行全部 99 关原有通关回归。

`CarSpriteSetup.BuildRelease` 生成 `Builds/Windows/LoveQiE.exe`。`CarReports` 保存素材检查、构建报告、运行回归报告与实际摄像机截图。正常运行不会启用回归，正式构建不包含回归逻辑。

本次在团结引擎 1.6.13 / 2022.3.61t14 的隔离项目副本中完成实际编译、Windows 构建与游戏运行验证：7,130 项检查通过，99/99 关真实移动通关，0 个运行错误。`CarReports/runtime-scene-1.png` 是该构建的实际摄像机渲染。原项目的角色预制体、图片及导入设置已同步为验证过的版本。
