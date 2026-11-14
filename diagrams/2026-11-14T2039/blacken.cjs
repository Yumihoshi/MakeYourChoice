// 把画板 OpenAPI/raw JSON 里所有连接线标注（caption）的文字颜色改成纯黑。
// 用法：node blacken.cjs openapi.json openapi.black.json
// 说明：caption 默认的 text_color_type=0 表示"跟随主题色"，此时 text_color 会被服务端忽略，
// 必须同时改成 text_color_type=1（自定义色）才会生效。
const fs = require("fs");

const [input, output] = process.argv.slice(2);
if (!input || !output) {
  console.error("用法: node blacken.cjs <输入.json> <输出.json>");
  process.exit(1);
}

const doc = JSON.parse(fs.readFileSync(input, "utf8"));
const nodes = Array.isArray(doc) ? doc : doc.nodes;

let patched = 0;
for (const node of nodes) {
  if (node.type !== "connector") continue;
  for (const caption of node.connector?.captions?.data ?? []) {
    caption.text_color = "#000000";
    caption.text_color_type = 1;
    caption.theme_text_color_code = -1;
    patched++;
  }
}

fs.writeFileSync(output, JSON.stringify({ nodes }, null, 0), "utf8");
console.log(`已改黑 ${patched} 条连接线标注 -> ${output}`);
