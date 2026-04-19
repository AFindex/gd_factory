extends RefCounted

const EXCLUDE_DIRS: Array[String] = [
	".godot",
	".git",
	"addons/script_stats_dashboard",
	"tmp_gif_frames",
	"tmp_gif_frames2",
	"artifacts",
]

const EXTENSIONS: Array[String] = ["cs", "gd"]
const BAR_COLORS: Array[String] = [
	"#7aa2f7", "#bb9af7", "#73daca", "#e0af68", "#ff9e64",
	"#f7768e", "#2ac3de", "#9ece6a", "#b4f9f8", "#cfc9c2",
]
const FILE_BAR_COLORS: Array[String] = [
	"#f7768e", "#ff9e64", "#e0af68", "#9ece6a", "#73daca",
	"#2ac3de", "#7aa2f7", "#bb9af7",
]


func generate_and_open() -> void:
	print("[ScriptStats] 开始扫描项目脚本...")
	var root := _scan_directory("res://")
	print("[ScriptStats] 扫描结果: ", root.file_count, " 个文件, ", root.total_lines, " 行")
	var html := _build_html(root)
	var out_path := OS.get_cache_dir().path_join("net_factory_script_stats.html")
	var f := FileAccess.open(out_path, FileAccess.WRITE)
	if f:
		f.store_string(html)
		f.close()
		print("[ScriptStats] 报告已保存到: ", out_path)
		OS.shell_open(out_path)
	else:
		push_error("[ScriptStats] 无法写入报告文件: " + out_path)


func _scan_directory(dir_path: String) -> Dictionary:
	var result := {
		"name": _dir_name(dir_path),
		"path": dir_path,
		"file_count": 0,
		"total_lines": 0,
		"total_size": 0,
		"code_lines": 0,
		"comment_lines": 0,
		"blank_lines": 0,
		"children": {},
		"files": [],
	}

	var dir := DirAccess.open(dir_path)
	if not dir:
		return result

	var dirs := dir.get_directories()
	for subdir in dirs:
		var full_path := dir_path.path_join(subdir)
		if not _should_exclude(full_path, subdir):
			var child := _scan_directory(full_path)
			if child.file_count > 0:
				result.children[subdir] = child
				_merge_stats(result, child)

	var file_list := dir.get_files()
	for file_name in file_list:
		var ext := file_name.get_extension().to_lower()
		if ext in EXTENSIONS:
			var full_path := dir_path.path_join(file_name)
			var info := _analyze_file(full_path, ext)
			result.files.append(info)
			result.file_count += 1
			result.total_lines += info.lines
			result.total_size += info.size
			result.code_lines += info.code_lines
			result.comment_lines += info.comment_lines
			result.blank_lines += info.blank_lines

	return result


func _dir_name(dir_path: String) -> String:
	var trimmed := dir_path.trim_suffix("/")
	var name := trimmed.get_file()
	if name == "":
		return "(root)"
	return name


func _should_exclude(full_path: String, file_name: String) -> bool:
	for ex in EXCLUDE_DIRS:
		if full_path.begins_with("res://" + ex) or file_name == ex:
			return true
	return false


func _merge_stats(target: Dictionary, source: Dictionary) -> void:
	target.file_count += source.file_count
	target.total_lines += source.total_lines
	target.total_size += source.total_size
	target.code_lines += source.code_lines
	target.comment_lines += source.comment_lines
	target.blank_lines += source.blank_lines


func _analyze_file(path: String, ext: String) -> Dictionary:
	var content := FileAccess.get_file_as_string(path)
	var size := content.length()
	var lines_raw: PackedStringArray = content.split("\n")
	var total := lines_raw.size()
	var blank := 0
	var comment := 0
	var in_multiline := false

	for raw_line in lines_raw:
		var line: String = raw_line.strip_edges()
		if line == "":
			blank += 1
			continue

		if ext == "cs":
			if in_multiline:
				comment += 1
				if line.ends_with("*/"):
					in_multiline = false
				elif "*/" in line:
					in_multiline = false
				continue

			if line.begins_with("//"):
				comment += 1
				continue

			if line.begins_with("/*"):
				comment += 1
				if not line.ends_with("*/") and not ("*/" in line):
					in_multiline = true
				continue
		elif ext == "gd":
			if line.begins_with("#"):
				comment += 1
				continue

	var code := total - blank - comment
	if code < 0:
		code = 0

	return {
		"name": path.get_file(),
		"path": path,
		"ext": ext,
		"lines": total,
		"size": size,
		"code_lines": code,
		"comment_lines": comment,
		"blank_lines": blank,
	}


# ---------- HTML 生成 ----------

func _build_html(root: Dictionary) -> String:
	var generated_at := Time.get_datetime_string_from_system(false, true)
	return _HTML_HEAD.replace("/*GENERATED_AT*/", generated_at) \
		+ _build_overview(root) \
		+ _build_type_dist(root) \
		+ _build_dir_top(root) \
		+ _build_file_top(root) \
		+ _HTML_TAIL


func _fmt_num(n: int) -> String:
	return str(n)


func _fmt_size(b: int) -> String:
	if b < 1024:
		return str(b) + " B"
	if b < 1024 * 1024:
		return str(snappedf(b / 1024.0, 0.1)) + " KB"
	return str(snappedf(b / float(1024 * 1024), 0.01)) + " MB"


func _build_overview(root: Dictionary) -> String:
	var avg := 0
	if root.file_count > 0:
		avg = root.total_lines / root.file_count
	return """
<div class="cards">
  <div class="card"><div class="label">脚本总数</div><div class="value">""" + _fmt_num(root.file_count) + """</div></div>
  <div class="card"><div class="label">代码总行数</div><div class="value">""" + _fmt_num(root.total_lines) + """</div>
    <div class="sub">
      <span class="c-code">代码 """ + _fmt_num(root.code_lines) + """</span>
      <span class="c-comment">注释 """ + _fmt_num(root.comment_lines) + """</span>
      <span class="c-blank">空行 """ + _fmt_num(root.blank_lines) + """</span>
    </div>
  </div>
  <div class="card"><div class="label">项目总大小</div><div class="value">""" + _fmt_size(root.total_size) + """</div></div>
  <div class="card"><div class="label">平均文件行数</div><div class="value">""" + _fmt_num(avg) + """</div></div>
</div>
"""


func _build_type_dist(root: Dictionary) -> String:
	var files: Array = []
	_collect_files(root, files)
	var cs_count := 0; var cs_lines := 0; var cs_size := 0
	var gd_count := 0; var gd_lines := 0; var gd_size := 0
	for f in files:
		if f.ext == "cs":
			cs_count += 1; cs_lines += f.lines; cs_size += f.size
		else:
			gd_count += 1; gd_lines += f.lines; gd_size += f.size
	var total := cs_count + gd_count
	var cs_pct := 0.0 if total == 0 else float(cs_count) / total
	var gd_pct := 0.0 if total == 0 else float(gd_count) / total
	var cs_ring := _svg_ring(40, cs_pct, "#7aa2f7", cs_count)
	var gd_ring := _svg_ring(40, gd_pct, "#bb9af7", gd_count)

	return """
<div class="grid">
  <div class="panel">
    <h2>文件类型分布</h2>
    <div class="pie-wrap">
      <div>""" + cs_ring + """<div style="text-align:center;font-size:10px;color:#7aa2f7;margin-top:2px;">C#</div></div>
      <div>""" + gd_ring + """<div style="text-align:center;font-size:10px;color:#bb9af7;margin-top:2px;">GD</div></div>
      <div class="pie-legend">
        <div class="item"><div class="dot" style="background:#7aa2f7"></div> C# """ + _fmt_num(cs_count) + """ / """ + _fmt_num(cs_lines) + """L / """ + _fmt_size(cs_size) + """</div>
        <div class="item"><div class="dot" style="background:#bb9af7"></div> GD """ + _fmt_num(gd_count) + """ / """ + _fmt_num(gd_lines) + """L / """ + _fmt_size(gd_size) + """</div>
      </div>
    </div>
  </div>
"""


func _svg_ring(r: int, pct: float, color: String, count: int) -> String:
	var c := 2 * PI * r
	var dash := pct * c
	var rem := c - dash
	return "<svg width=\"100\" height=\"100\" viewBox=\"0 0 100 100\"><circle cx=\"50\" cy=\"50\" r=\"" + str(r) + "\" fill=\"none\" stroke=\"#222\" stroke-width=\"10\"/><circle cx=\"50\" cy=\"50\" r=\"" + str(r) + "\" fill=\"none\" stroke=\"" + color + "\" stroke-width=\"10\" stroke-dasharray=\"" + str(dash) + " " + str(rem) + "\" stroke-linecap=\"butt\" transform=\"rotate(-90 50 50)\"/><text x=\"50\" y=\"48\" text-anchor=\"middle\" fill=\"#e0e0e0\" font-size=\"14\" font-weight=\"bold\" font-family=\"monospace\">" + str(roundi(pct * 100)) + "%</text><text x=\"50\" y=\"62\" text-anchor=\"middle\" fill=\"#555\" font-size=\"9\" font-family=\"monospace\">" + _fmt_num(count) + "</text></svg>"


func _build_dir_top(root: Dictionary) -> String:
	var dirs: Array = []
	_collect_dirs(root, dirs)
	dirs.sort_custom(func(a, b): return a.total_lines > b.total_lines)
	var top := dirs.slice(0, 10)
	var maxv := 1
	if top.size() > 0:
		maxv = top[0].total_lines
	var html := "<div class=\"panel\"><h2>目录代码量 TOP 10</h2>"
	for i in range(top.size()):
		var d: Dictionary = top[i]
		var pct := float(d.total_lines) / maxv * 100.0
		html += "<div class=\"bar-row\"><div class=\"bar-label\" title=\"" + d.path + "\">" + d.name + "</div><div class=\"bar-track\"><div class=\"bar-fill\" style=\"width:" + str(snappedf(pct, 0.1)) + "%;background:" + BAR_COLORS[i % BAR_COLORS.size()] + "\"></div></div><div class=\"bar-val\">" + _fmt_num(d.total_lines) + "</div></div>"
	html += "</div></div>"
	return html


func _build_file_top(root: Dictionary) -> String:
	var files: Array = []
	_collect_files(root, files)
	files.sort_custom(func(a, b): return a.lines > b.lines)
	var top := files.slice(0, 50)
	var maxv := 1
	if top.size() > 0:
		maxv = top[0].lines

	var default_show := 15
	var rows_html := ""
	for i in range(top.size()):
		var fi: Dictionary = top[i]
		var pct := float(fi.lines) / maxv * 100.0
		var disp := "flex" if i < default_show else "none"
		var copy_line: String = str(i + 1).pad_zeros(2) + ". " + fi.name + " | " + _fmt_num(fi.lines) + " 行 | " + _fmt_size(fi.size) + " | " + fi.path
		rows_html += "<div class=\"bar-row file-top-row\" style=\"display:" + disp + ";\" data-text=\"" + copy_line.replace("\"", "&quot;") + "\"><div class=\"bar-label\" title=\"" + fi.path + "\">" + fi.name + "</div><div class=\"bar-track\"><div class=\"bar-fill\" style=\"width:" + str(snappedf(pct, 0.1)) + "%;background:" + FILE_BAR_COLORS[i % FILE_BAR_COLORS.size()] + "\"></div></div><div class=\"bar-val\">" + _fmt_num(fi.lines) + "</div></div>"

	var html := "<div class=\"grid grid-grow\"><div class=\"panel\"><h2>目录层级总览</h2><div class=\"tree-wrap\">" + _render_tree(root) + "</div></div>"
	html += "<div class=\"panel\"><h2>最大文件 TOP <select id=\"file-top-sel\" class=\"top-sel\" onchange=\"updateFileTop()\"><option value=\"10\">10</option><option value=\"15\" selected>15</option><option value=\"20\">20</option><option value=\"30\">30</option><option value=\"50\">50</option></select><button class=\"copy-btn\" onclick=\"copyFileTop()\">复制</button></h2>"
	html += "<div class=\"file-list-wrap\">" + rows_html + "</div>"
	html += "</div></div>"

	html += "<script>function updateFileTop(){var s=document.getElementById('file-top-sel');var n=s?parseInt(s.value):15;var r=document.querySelectorAll('.file-top-row');r.forEach(function(e,i){e.style.display=i<n?'flex':'none';});}function copyFileTop(){var s=document.getElementById('file-top-sel');var n=s?parseInt(s.value):15;var r=document.querySelectorAll('.file-top-row');var t='最大文件 TOP '+n+'\\n';var c=0;r.forEach(function(e){if(c>=n)return;t+=e.getAttribute('data-text')+'\\n';c++;});navigator.clipboard?navigator.clipboard.writeText(t).then(function(){alert('已复制')}):function(){var a=document.createElement('textarea');a.value=t;document.body.appendChild(a);a.select();document.execCommand('copy');document.body.removeChild(a);alert('已复制')}();}</script>"
	return html


func _render_tree(node: Dictionary) -> String:
	var html := ""
	for k in node.children.keys():
		var ch: Dictionary = node.children[k]
		html += "<details open><summary class=\"tree-summary\"><span class=\"dir-icon\">[+] </span>" + ch.name
		html += " <span class=\"meta\">" + str(ch.file_count) + "F " + _fmt_num(ch.total_lines) + "L " + _fmt_size(ch.total_size) + "</span></summary><div class=\"tree-children\">"
		html += _render_tree(ch)
		for j in range(ch.files.size()):
			var fi: Dictionary = ch.files[j]
			html += "<div class=\"file-row\"><span class=\"file-icon\">" + (">" if fi.ext == "cs" else "*") + "</span> " + fi.name
			html += " <span class=\"meta\">" + _fmt_num(fi.lines) + "L " + _fmt_size(fi.size) + "</span></div>"
		html += "</div></details>"
	return html


func _collect_dirs(node: Dictionary, list: Array) -> void:
	if node.file_count > 0:
		list.append(node)
	for k in node.children:
		_collect_dirs(node.children[k], list)


func _collect_files(node: Dictionary, list: Array) -> void:
	for f in node.files:
		list.append(f)
	for k in node.children:
		_collect_files(node.children[k], list)


const _HTML_HEAD := """<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>SCRIPT STATS</title>
<style>
:root {
  --bg: #0a0a0a;
  --bg-card: #111111;
  --border: #333333;
  --text: #888888;
  --text-bright: #e0e0e0;
  --accent: #7aa2f7;
  --green: #73daca;
  --yellow: #e0af68;
  --font-mono: "Courier New", Consolas, "Lucida Console", monospace;
}
* { box-sizing: border-box; margin: 0; padding: 0; }
body {
  background: var(--bg);
  color: var(--text);
  font-family: var(--font-mono);
  font-size: 13px;
  line-height: 1.4;
  padding: 14px;
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
  gap: 10px;
}
header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 4px;
  border-bottom: 2px solid var(--border);
  flex-shrink: 0;
}
header h1 {
  font-size: 16px;
  color: var(--text-bright);
  text-transform: uppercase;
  letter-spacing: 2px;
}
header .meta {
  font-size: 11px;
  color: #444;
}
.cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 6px;
  flex-shrink: 0;
}
.card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  padding: 8px;
}
.card .label {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #555;
  margin-bottom: 2px;
}
.card .value {
  font-size: 22px;
  font-weight: bold;
  color: var(--text-bright);
}
.card .sub {
  font-size: 11px;
  margin-top: 2px;
  color: #444;
}
.card .sub span { margin-right: 6px; }
.card .sub .c-code { color: var(--green); }
.card .sub .c-comment { color: var(--yellow); }
.card .sub .c-blank { color: #444; }

.grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 6px;
  flex-shrink: 0;
}
@media (max-width: 800px) {
  .grid { grid-template-columns: 1fr; }
}
.grid-grow {
  flex: 1;
  min-height: 0;
  margin-bottom: 0;
  overflow: hidden;
}

.panel {
  background: var(--bg-card);
  border: 1px solid var(--border);
  padding: 8px;
  display: flex;
  flex-direction: column;
  min-height: 0;
}
.panel h2 {
  font-size: 12px;
  color: var(--text-bright);
  text-transform: uppercase;
  letter-spacing: 1px;
  margin-bottom: 6px;
  padding-bottom: 2px;
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  gap: 4px;
}

.pie-wrap { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.pie-legend { display: flex; flex-direction: column; gap: 3px; }
.pie-legend .item { display: flex; align-items: center; gap: 6px; font-size: 12px; }
.dot { width: 10px; height: 10px; }

.bar-row { display: flex; align-items: center; margin-bottom: 3px; font-size: 12px; }
.bar-label { width: 130px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: var(--text-bright); }
.bar-track { flex: 1; height: 10px; background: #1a1a1a; border: 1px solid var(--border); overflow: hidden; }
.bar-fill { height: 100%; }
.bar-val { width: 55px; text-align: right; color: #555; padding-left: 4px; }

.tree-wrap { flex: 1; overflow: auto; font-size: 12px; min-height: 0; }
.tree-wrap::-webkit-scrollbar { width: 6px; }
.tree-wrap::-webkit-scrollbar-track { background: #111; border-left: 1px solid var(--border); }
.tree-wrap::-webkit-scrollbar-thumb { background: #333; border: 1px solid #444; }
.tree-wrap::-webkit-scrollbar-thumb:hover { background: #444; }
.tree-wrap::-webkit-scrollbar-button { display: none; }
.file-list-wrap { flex: 1; overflow: auto; font-size: 12px; min-height: 0; }
.file-list-wrap::-webkit-scrollbar { width: 6px; }
.file-list-wrap::-webkit-scrollbar-track { background: #111; border-left: 1px solid var(--border); }
.file-list-wrap::-webkit-scrollbar-thumb { background: #333; border: 1px solid #444; }
.file-list-wrap::-webkit-scrollbar-thumb:hover { background: #444; }
.file-list-wrap::-webkit-scrollbar-button { display: none; }
.top-sel {
  background: #111;
  color: var(--text-bright);
  border: 1px solid var(--border);
  font-family: var(--font-mono);
  font-size: 11px;
  margin-left: 4px;
  margin-right: 4px;
  padding: 0 2px;
}
details { margin: 1px 0; }
details > summary { list-style: none; cursor: pointer; display: flex; align-items: center; gap: 4px; padding: 1px 2px; }
details > summary::-webkit-details-marker { display: none; }
details > summary:hover { background: #1a1a1a; }
.tree-children { padding-left: 10px; border-left: 1px solid var(--border); margin-left: 2px; }
.dir-icon { color: var(--accent); }
.file-row { display: flex; align-items: center; gap: 4px; padding: 1px 2px; }
.file-row:hover { background: #1a1a1a; }
.file-icon { color: #555; }
.meta { margin-left: auto; color: #444; font-size: 11px; white-space: nowrap; }

.copy-btn {
  margin-left: auto;
  background: transparent;
  color: var(--text);
  border: 1px solid var(--border);
  padding: 1px 6px;
  font-size: 11px;
  font-family: var(--font-mono);
  cursor: pointer;
  text-transform: uppercase;
}
.copy-btn:hover { background: var(--border); color: var(--text-bright); }

footer {
  text-align: center;
  font-size: 11px;
  color: #222;
  flex-shrink: 0;
}
</style>
</head>
<body>
<header>
  <h1>SCRIPT STATS</h1>
  <div class="meta">/*GENERATED_AT*/</div>
</header>
"""

const _HTML_TAIL := """
<footer>SCRIPT STATS DASHBOARD // GENERATED BY GODOT</footer>
</body>
</html>"""
