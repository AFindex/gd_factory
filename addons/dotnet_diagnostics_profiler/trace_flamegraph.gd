@tool
extends Control

signal node_hovered(node)
signal node_selected(node)

const ROW_HEIGHT := 26.0
const ROW_GAP := 4.0
const MIN_LABEL_WIDTH := 44.0
const HORIZONTAL_PADDING := 8.0

var _root_node: Dictionary = {}
var _visible_roots: Array = []
var _visible_blocks: Array = []
var _hovered_node: Dictionary = {}
var _selected_node: Dictionary = {}
var _max_depth := 0
var _total_weight := 0.0


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_STOP
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	size_flags_vertical = Control.SIZE_EXPAND_FILL


func set_trace_root(root_node: Dictionary, selected_node: Dictionary = {}) -> void:
	_root_node = root_node
	_selected_node = selected_node
	_hovered_node = {}
	_rebuild_layout()
	queue_redraw()


func clear_graph() -> void:
	_root_node = {}
	_visible_roots.clear()
	_visible_blocks.clear()
	_hovered_node = {}
	_selected_node = {}
	_max_depth = 0
	_total_weight = 0.0
	custom_minimum_size = Vector2(0.0, 160.0)
	queue_redraw()


func get_selected_node() -> Dictionary:
	return _selected_node


func _rebuild_layout() -> void:
	_visible_blocks.clear()
	_max_depth = 0
	_total_weight = 0.0

	if _root_node.is_empty():
		custom_minimum_size = Vector2(0.0, 160.0)
		return

	_total_weight = max(float(_root_node.get("cumulative_weight", 0.0)), 0.0)
	if _total_weight <= 0.0:
		custom_minimum_size = Vector2(0.0, 160.0)
		return

	var children: Array = _root_node.get("children", [])
	if str(_root_node.get("name", "")) == "(root)" and not children.is_empty():
		_visible_roots = children
	else:
		_visible_roots = [_root_node]

	var available_width: float = maxf(size.x - HORIZONTAL_PADDING * 2.0, 200.0)
	var x_cursor: float = HORIZONTAL_PADDING
	for node in _visible_roots:
		var width: float = available_width * (float(node.get("cumulative_weight", 0.0)) / _total_weight)
		_layout_node(node, 0, x_cursor, width)
		x_cursor += width

	var required_height: float = (_max_depth + 1) * (ROW_HEIGHT + ROW_GAP) + HORIZONTAL_PADDING
	custom_minimum_size = Vector2(0.0, maxf(required_height, 160.0))


func _layout_node(node: Dictionary, depth: int, x: float, width: float) -> void:
	if width <= 1.0:
		return

	var y: float = HORIZONTAL_PADDING + depth * (ROW_HEIGHT + ROW_GAP)
	var rect := Rect2(x, y, width, ROW_HEIGHT)
	_visible_blocks.append({
		"rect": rect,
		"node": node,
	})
	_max_depth = maxi(_max_depth, depth)

	var children: Array = node.get("children", [])
	if children.is_empty():
		return

	var total: float = max(float(node.get("cumulative_weight", 0.0)), 0.0)
	if total <= 0.0:
		return

	var child_x: float = x
	for child in children:
		var child_weight: float = float(child.get("cumulative_weight", 0.0))
		var child_width: float = width * (child_weight / total)
		_layout_node(child, depth + 1, child_x, child_width)
		child_x += child_width


func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, size), Color(0.10, 0.11, 0.13, 1.0), true)

	if _visible_blocks.is_empty():
		var font := get_theme_default_font()
		var font_size := get_theme_default_font_size()
		if font != null:
			draw_string(font, Vector2(16.0, 28.0), "加载 trace 后会在这里显示 flamegraph。", HORIZONTAL_ALIGNMENT_LEFT, -1.0, font_size, Color(0.78, 0.80, 0.84, 1.0))
		return

	var font := get_theme_default_font()
	var font_size := get_theme_default_font_size()
	for block in _visible_blocks:
		var rect: Rect2 = block["rect"]
		var node: Dictionary = block["node"]
		var base_color := _color_for_node(str(node.get("full_name", "")))
		if _matches_node(node, _selected_node):
			base_color = base_color.lightened(0.25)
		elif _matches_node(node, _hovered_node):
			base_color = base_color.lightened(0.12)
		draw_rect(rect, base_color, true)
		draw_rect(rect, Color(0.07, 0.08, 0.10, 0.95), false, 1.0)

		if font == null:
			continue

		var label := _format_label(node, rect.size.x)
		if label.is_empty():
			continue

		var text_y: float = rect.position.y + rect.size.y * 0.68
		draw_string(font, Vector2(rect.position.x + 6.0, text_y), label, HORIZONTAL_ALIGNMENT_LEFT, rect.size.x - 12.0, font_size, Color(0.96, 0.97, 0.99, 1.0))


func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		var mouse_event: InputEventMouseMotion = event
		_update_hover(mouse_event.position)
		return

	if event is InputEventMouseButton:
		var button_event: InputEventMouseButton = event
		if button_event.button_index == MOUSE_BUTTON_LEFT and button_event.pressed:
			var hit := _find_block(button_event.position)
			if not hit.is_empty():
				_selected_node = hit["node"]
				node_selected.emit(_selected_node)
				queue_redraw()


func _notification(what: int) -> void:
	if what == NOTIFICATION_MOUSE_EXIT:
		_hovered_node = {}
		tooltip_text = ""
		queue_redraw()
	elif what == NOTIFICATION_RESIZED:
		_rebuild_layout()
		queue_redraw()


func _update_hover(position: Vector2) -> void:
	var hit := _find_block(position)
	if hit.is_empty():
		if not _hovered_node.is_empty():
			_hovered_node = {}
			tooltip_text = ""
			queue_redraw()
		return

	var node: Dictionary = hit["node"]
	if _matches_node(node, _hovered_node):
		return

	_hovered_node = node
	tooltip_text = "%s\n累计权重: %s" % [
		str(node.get("full_name", "")),
		_format_weight(float(node.get("cumulative_weight", 0.0)))
	]
	node_hovered.emit(node)
	queue_redraw()


func _find_block(position: Vector2) -> Dictionary:
	for block in _visible_blocks:
		var rect: Rect2 = block["rect"]
		if rect.has_point(position):
			return block
	return {}


func _format_label(node: Dictionary, width: float) -> String:
	if width < MIN_LABEL_WIDTH:
		return ""

	var percentage: float = 0.0
	if _total_weight > 0.0:
		percentage = float(node.get("cumulative_weight", 0.0)) / _total_weight * 100.0

	var raw := "%s  %.1f%%" % [str(node.get("name", "")), percentage]
	return _truncate_label(raw, width - 12.0)


func _truncate_label(text: String, max_width: float) -> String:
	var font := get_theme_default_font()
	var font_size := get_theme_default_font_size()
	if font == null:
		return text
	if font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1.0, font_size).x <= max_width:
		return text

	var ellipsis := "..."
	var working := text
	while working.length() > 1:
		working = working.substr(0, working.length() - 1)
		var candidate := "%s%s" % [working, ellipsis]
		if font.get_string_size(candidate, HORIZONTAL_ALIGNMENT_LEFT, -1.0, font_size).x <= max_width:
			return candidate
	return ""


func _color_for_node(name: String) -> Color:
	var hue: float = float(abs(name.hash()) % 360) / 360.0
	return Color.from_hsv(hue, 0.58, 0.78, 1.0)


func _matches_node(left: Dictionary, right: Dictionary) -> bool:
	if left.is_empty() or right.is_empty():
		return false
	return str(left.get("path_id", "")) == str(right.get("path_id", ""))


func _format_weight(weight: float) -> String:
	if absf(weight - round(weight)) < 0.0001:
		return str(int(round(weight)))
	return "%.2f" % weight
