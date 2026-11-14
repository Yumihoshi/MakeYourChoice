# 关卡一 · 流程分支图（飞书画板源件）

画板：`Osufwa2jihLavQbxiCxcF3s7nEb`（wiki《做出选择！！！》→ 关卡设计）

## 重新生成 / 追加

```powershell
node diagram.gen.cjs                       # 生成 diagram.json（DSL）
npx -y @larksuite/whiteboard-cli@^0.2.13 -i diagram.json -o diagram.png -l coords.json   # 渲染自检
npx -y @larksuite/whiteboard-cli@^0.2.13 -i diagram.json --to openapi --format json -o openapi.json
node blacken.cjs openapi.json openapi.black.json                                          # 连接线标注改纯黑
lark-cli whiteboard +update --whiteboard-token Osufwa2jihLavQbxiCxcF3s7nEb `
  --source "@./openapi.black.json" --input_format raw `
  --idempotent-token "<新的唯一 token>" --as user           # 不加 --overwrite = 增量追加
```

## 修改已有节点属性（颜色等）

画板 OpenAPI 没有"删除/更新单个节点"的接口，改属性只能整板重建：

1. `lark-cli whiteboard +export --output-type raw --output live.json`
2. 改 `live.json`（原图 `image` 节点带着 token，会被原样重建，坐标不变；已实测 token 可复用）
3. `+update --input_format raw --source @live.json --overwrite`

注意：连接线标注（caption）的 `text_color_type` 必须是 `1`（自定义色）才会生效，`0` 表示跟随主题色。

## 文件

| 文件 | 说明 |
| --- | --- |
| `diagram.gen.cjs` | 生成器：节点文案/配色/布局都在这里；`node diagram.gen.cjs [图表底部 y]`，传参才加底部图例 |
| `diagram.json` / `openapi.json` | DSL 与 OpenAPI 中间产物 |
| `blacken.cjs` | 连接线标注改黑（见上） |
| `live.json` | 画板当前状态的 raw 导出（改属性时的工作副本） |
| `final.jpg` | 最近一次画板预览截图 |
