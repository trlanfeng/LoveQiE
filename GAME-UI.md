# 游戏界面与星级通关

打开 `Assets/main.unity` 并 Play，即可进入新首页。Windows 成品：`Builds/Windows/LoveQiE.exe`（分发时复制整个 Windows 文件夹）。

## 游戏流程

- 首页点击「开始游戏」进入关卡选择。
- 99 个现有关卡全部可选，每页 12 关，支持前后翻页；每关显示历史最佳星级。
- 选择关卡时开始计时。WASD / 方向键控制两车，左右镜像、上下同向；也可点击底部方向按钮。
- 前 30 秒显示三星倒计时，超过 30 秒后显示到 60 秒的二星倒计时；超过 60 秒显示累计用时，仍可继续游戏。
- 两车同时到达原有顶部终点后结算：用时 ≤ 30 秒三星，30 秒 < 用时 ≤ 60 秒二星，超过 60 秒一星。恰好 30 秒为三星、恰好 60 秒为二星。
- 结算弹窗展示实际星级、用时、步数，提供「下一关」「再挑战一次」「关卡选择」。不会自动跳关；第 99 关改为「返回关卡」。
- R 或「重开」重新开始当前关，时间与步数清零。暂停按钮 / Esc 暂停，继续时排除暂停耗时。窗口失焦时自动暂停。
- 返回首页、选关、暂停和结算时均禁止游戏移动。暂停保留当前移动进度，回到选关时取消旧关移动。
- 星级通过 PlayerPrefs 保存，只保留更高纪录，不会被较低成绩覆盖。没有新增关卡锁定限制。

## 素材与维护

实际使用 `Assets/UI` 的 play、home、pause、count_time、count_steps、level_1～5、level_success、star_center 图像。蓝金配色的 UI 使用 UI Toolkit；界面和地图分开占用屏幕区域。

素材中的关卡图案带有固定编号和星星，运行时以真实编号与成绩覆盖。`Prepared/star_clean.png` 从原始 star_center 中去除边缘相邻星星碎片，未修改原图。`Prepared/JourneyUI.otf` 是 Noto Sans SC 的界面字符子集，附 SIL OFL 许可证，保证玩家机器无需额外安装中文字体。

- `Assets/_Scripts/Manager/GameManager.cs`：首页、选关、游玩、暂停、结算状态与计时/评分。
- `Assets/_Scripts/UI/GameUI.cs`：界面布局及按钮行为。
- `Assets/Resources/GameUI/GameUI.uss`：样式。
- `Assets/Resources/GameUI/Skin.asset`：引用原始 UI 图片与字体，自动包含在构建内。
- `Tools/prepare_game_ui.py`：清理星星与生成字体子集；新增中文文案后运行 `python Tools/prepare_game_ui.py`。需要 Pillow、fonttools、brotli，首次运行下载 OFL 字体源。
- `Tools > Game UI > Configure UI assets`：重新设置图像导入属性和 Skin 引用；常规打开项目、运行游戏不需要执行。

## 验证入口

引擎批处理入口 `GameUISetup.BuildDevelopment` 生成 `Builds/UIValidation/LoveQiE.exe`；`GameUISetup.BuildRelease` 生成正式 Windows 版本。

开发构建参数：`-batchmode -nativeSmoke -smokeReport <绝对路径> -screen-fullscreen 0 -screen-width 1280 -screen-height 800`。

回归使用真实 UI Toolkit 按钮事件、关卡和移动协程，覆盖首页、分页、选关、暂停、重开、三档结算与阈值、最佳纪录保存、全部 99 关路线、手动下一关与最终关返回。回归不写入正常关卡成绩。截图来自真实 UI Toolkit 离屏渲染和游戏摄像机合成；不等同于人工鼠标点击检查。

验证报告及截图保存在 `UIReports`。

2026-09-24 验证结果：团结引擎 1.6.13 / 2022.3.61t14，Windows x64 Development 与 Release 构建成功，构建错误和警告均为 0。最终 1020×768 版本回归通过 59,328 项检查、99/99 关真实路线、0 个运行错误；另外检查了 1280×800 界面。首页、关卡选择、游戏 HUD、暂停和一/二/三星结算均已检查实际渲染截图。未进行人工鼠标逐项点击或移动设备验证。
