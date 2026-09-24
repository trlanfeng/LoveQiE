# 城市人行道：TileMapDual 测试

原参考文件实际为 `res://assets/tileset_grass.png`。新图集使用相同的 4×4 Standard 顺序、无边距、无间距、RGBA 透明背景；轮廓改为城市圆角路沿，不复制草地的毛边。

## 最快测试

打开 `res://output/city-sidewalk-dual/city_sidewalk_test.tscn`，选中 `PaintHere_CitySidewalk`，使用 TileMap 编辑面板绘制：
- `(2,1)` 是完整人行道画笔（坐标从0开始，即第3列第2行）。
- `(0,3)` 是透明背景画笔（第1列第4行）。要明确恢复背景，可用这个瓦片覆盖。
- 下方的 `Asphalt` 是柏油背景层；人行道图集的透明部分会露出这层。
- TileMapDual 自动建立带半格偏移的显示层，不需要自己创建／移动第二层。

## 文件

- `res://assets/tileset_city_sidewalk.png`：128×128，单格32×32，与参考图同规格。
- `res://assets/tileset_city_sidewalk_hd.png`：512×512，单格128×128，同样16格排列，用于查看更细的路沿效果。
- `res://assets/city_asphalt_base.png`：32×32可平铺柏油背景。
- `res://output/city-sidewalk-dual/city_sidewalk.tres`：已配置好的32px TileSet，可直接赋给 TileMapDual 的 Tile Set。
- `dual-preview.png`：六种复杂形状的拼接预览。

## 只用PNG新建测试

给 TileMapDual 设置 TileSet，添加图集，选择 Standard 预设。原尺寸图的 Tile Size 和 Texture Region Size 都设为32×32；Margin和Separation都为0。使用高清版时这两个尺寸改为128×128。不要把高清版按32×32切割，否则会得到错误的16×16排列。

像素版可用Nearest；高清版可用Linear。图集全透明格需要保留，若Godot自动创建图块时跳过它，请使用TileMapDual预设创建全部16格，或直接加载提供的tres。

## 验证

修正说明：上一版手工转换四角掩码时，6个格子的图案放错了位置。旧测试只验证了插件选中的坐标，没有读取这些坐标内的PNG像素，因此旧的“规则通过”结论不充分。生成器现直接读取安装插件的Standard坐标表，并独立对照原草地参考图和输出PNG的四角透明度。

本版用项目安装的 TileMapDual / Standard 实际创建 TileSet 和 TileMapDual，检查16/16种四角状态的显示层选片，并读取Godot导入纹理的实际四角像素与逻辑状态一一核对；清除前景后的图案也通过检查。像素检查覆盖128组合法相邻连接，最大预乘RGBA通道差3/255。standard-layout-check.png为逐格参考对照；godot-actual-render.png为实际引擎渲染。人工GUI笔刷操作未逐笔验收。

完整位掩码顺序见layout.json。背景0、前景1；四角权重NW1、NE2、SW4、SE8。与Unity版本的47格八邻域图集不同，这里使用TileMapDual原生的16格四角格式。
