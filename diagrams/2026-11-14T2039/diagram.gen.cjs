// 关卡一流程分支图：手搭节点 + dagre 自动布线，输出飞书画板 DSL
const fs = require('fs');

const C = {
  ink: '#1F2329',
  sub: '#646A73',
  blue: '#5178C6',    // 电梯间 A 区
  purple: '#8569CB',  // B 区办公层
  green: '#509863',   // 打印室 / 好结局
  red: '#D25D5A',     // 储藏室 / 坏结局
  yellow: '#D4B45B'   // 判断节点
};

// 步骤卡片：白色底 + 分区色边框，标题加粗 + 若干说明行
function card(id, title, desc, border) {
  const children = [{
    type: 'text', width: 'fill-container', height: 'fit-content',
    text: [{ content: title, bold: true, fontSize: 15, color: C.ink }]
  }];
  for (const line of desc || []) {
    children.push({
      type: 'text', width: 'fill-container', height: 'fit-content',
      text: line, fontSize: 13, textColor: C.sub
    });
  }
  return {
    type: 'frame', id, layout: 'vertical', gap: 4, padding: [10, 14],
    width: 280, height: 'fit-content',
    fillColor: '#FFFFFF', borderColor: border, borderWidth: 2, borderRadius: 8,
    children
  };
}

function diamond(id, text) {
  return {
    type: 'diamond', id, width: 180, height: 104, text,
    fontSize: 14, textColor: C.ink,
    fillColor: '#FEF1CE', borderColor: C.yellow, borderWidth: 2
  };
}

function pill(id, text, kind) {
  const ok = kind !== 'bad';
  return {
    type: 'ellipse', id, width: 180, height: 64, text,
    fontSize: 15, textColor: C.ink,
    fillColor: ok ? '#DFF5E5' : '#FEE3E2',
    borderColor: ok ? C.green : C.red, borderWidth: 2
  };
}

const nodes = [
  pill('S', '开始', 'ok'),

  card('E1', '1 第一次位于 A', ['欢迎来到后室', '门外有声音，快开门'], C.blue),
  diamond('D1', '开门前行动'),
  card('E2', '2 打开电梯门', ['“看，我没骗你”'], C.blue),
  card('E3', '3 看到红字警告', ['先贴门听再决定'], C.blue),
  card('E4', '4 靠近电梯门 3s', ['只是风声，可以验证'], C.blue),
  card('E5', '5 THE END 最速传说', ['在电梯里待满 5 分钟'], C.red),

  card('E6', '6 出电梯，交互安全门', ['无门禁卡，打不开', '建议先去找门禁卡'], C.purple),
  card('E7', '7 第一次位于 B', ['红字吓人；蓝箭头指路'], C.purple),
  diamond('D7', 'B 区行动'),

  card('E8', '8 进入打印室', ['复印机闪紫光', '柜子绿色高光只是反光，去复印机看看'], C.green),
  card('E9', '9 蓝箭头的解释', ['蓝箭头通向隔间区，那里有卡', '红字门里也有东西，但不想让你被困'], C.purple),
  card('E10', '10 进入储藏室', ['门被锁；可远程开但需一分钟', '或找别的出口'], C.red),
  card('E11', '11 发现维护记录', ['“蓝箭头有时绕远，但安全”'], C.red),
  diamond('D8', '打印室行动'),
  card('E14', '14 柜子 A 只是反光', ['继续探索'], C.green),
  card('E12', '12 获得“临时门禁卡”', ['锁定柜子无法交互'], C.green),
  card('E13', '13 用临时门禁卡试安全门', ['没有反应，卡过期', '若未触发 14，则说柜子里没东西'], C.green),
  card('E15', '15 获得“门禁卡”', ['“你比我想的细心，我确实不知道夹层”'], C.green),
  card('E16', '16 交互安全门，有门禁卡', ['表中未写后续 / 通关结果'], C.green),
  pill('END16', '通关 / 后续', 'ok'),
  pill('END5', '结局：最速传说', 'bad')
];

const edges = [
  ['S', 'E1'],
  ['E1', 'D1'],
  ['D1', 'E2', '直接交互电梯门'],
  ['D1', 'E3', '回头看向红字「别开」'],
  ['D1', 'E5', '未开门，在电梯里待 5 分钟'],
  ['E3', 'E4'],
  ['E4', 'E2', '验证后开门'],
  ['E2', 'E6'],
  ['E6', 'E7'],
  ['E7', 'D7'],
  ['D7', 'E8', '跟着蓝箭头进入打印室'],
  ['D7', 'E9', '对蓝箭头交互追问'],
  ['D7', 'E10', '进红字门'],
  ['E9', 'D7'],
  ['E10', 'E11', '打开柜子 B'],
  ['E11', 'D7', '离开，继续探索'],
  ['E8', 'D8'],
  ['D8', 'E14', '第一次交互柜子 A'],
  ['D8', 'E12', '交互复印机'],
  ['E14', 'D8', '继续探索'],
  ['E12', 'E13'],
  ['E13', 'E15', '第二次交互柜子 A'],
  ['E15', 'E16'],
  ['E16', 'END16'],
  ['E5', 'END5']
];

// 图例：色块 + 文案，说明四种分区色与判断色的含义
function legendItem(color, label) {
  return {
    type: 'frame', layout: 'horizontal', gap: 8, padding: 0,
    width: 'fit-content', height: 'fit-content', alignItems: 'center',
    fillColor: '#FFFFFF', borderColor: '#FFFFFF', borderWidth: 0,
    children: [
      {
        type: 'rect', width: 16, height: 16, fillColor: color,
        borderColor: color, borderWidth: 2, borderRadius: 3, text: ''
      },
      {
        type: 'text', width: 'fit-content', height: 'fit-content',
        text: label, fontSize: 14, textColor: C.ink
      }
    ]
  };
}

function legend(bottomY) {
  return {
    type: 'frame', id: 'legend',
    x: 1698, y: bottomY + 56,
    layout: 'horizontal', gap: 28, padding: [14, 22],
    width: 'fit-content', height: 'fit-content', alignItems: 'center',
    fillColor: '#FFFFFF', borderColor: '#DEE0E3', borderWidth: 2, borderRadius: 8,
    children: [
      { type: 'text', width: 'fit-content', height: 'fit-content', text: '分区图例', fontSize: 14, textColor: C.sub },
      legendItem(C.blue, '电梯间 A 区'),
      legendItem(C.purple, 'B 区办公层'),
      legendItem(C.green, '打印室'),
      legendItem(C.red, '储藏室（红字门）'),
      legendItem(C.yellow, '行动判断')
    ]
  };
}

const chartBottom = Number(process.argv[2] || 0);

const doc = {
  version: 2,
  nodes: [
    {
      type: 'text', id: 'title',
      x: 1698, y: -210, width: 920, height: 'fit-content',
      text: [{ content: '关卡一 · 流程分支图', bold: true, fontSize: 26, color: C.ink }],
      textAlign: 'left'
    },
    {
      type: 'frame', id: 'root',
      x: 1650, y: -76,
      layout: 'dagre',
      width: 'fit-content', height: 'fit-content',
      padding: 48,
      fillColor: '#F0F4FC', borderColor: C.blue, borderWidth: 2, borderRadius: 16,
      layoutOptions: {
        rankdir: 'TB',
        nodesep: 70,
        ranksep: 165,
        edges
      },
      children: nodes
    }
  ]
};

if (chartBottom > 0) doc.nodes.push(legend(chartBottom));

fs.writeFileSync(__dirname + '/diagram.json', JSON.stringify(doc, null, 2), 'utf8');
console.log('nodes:', nodes.length, 'edges:', edges.length, 'legend:', chartBottom > 0);
