@tool
extends EditorPlugin

const MENU_OPEN_PANEL := "Diagnostics: Open Process Panel"
const MENU_OPEN_DOCS := "Diagnostics: Open Profiling Docs"
const TAB_DIAGNOSTICS := 0
const TAB_VIEWER := 1

const TraceFlamegraph = preload("res://addons/dotnet_diagnostics_profiler/trace_flamegraph.gd")
const TraceSpeedscopeLoader = preload("res://addons/dotnet_diagnostics_profiler/trace_speedscope_loader.gd")

var _dialog: AcceptDialog
var _poll_timer: Timer
var _window: Window
var _file_dialog: FileDialog

var _active_pid: int = -1
var _active_status_file := ""
var _latest_trace_status: Dictionary = {}

var _root_container: MarginContainer
var _tabs: TabContainer

var _tree: Tree
var _status_label: RichTextLabel
var _duration_option: OptionButton
var _refresh_button: Button
var _attach_button: Button
var _selected_process_id: int = -1
var _selected_command_line := ""

var _viewer_refresh_button: Button
var _viewer_open_button: Button
var _profile_option: OptionButton
var _frame_option: OptionButton
var _zoom_button: Button
var _back_button: Button
var _reset_button: Button
var _viewer_status_label: RichTextLabel
var _viewer_file_label: Label
var _viewer_profile_label: Label
var _viewer_stats_label: Label
var _viewer_warning_label: Label
var _viewer_details_label: RichTextLabel
var _flamegraph: Control

var _trace_document: Dictionary = {}
var _selected_profile_index := -1
var _current_profile: Dictionary = {}
var _current_root: Dictionary = {}
var _current_zoom_stack: Array = []
var _selected_node: Dictionary = {}
var _selected_frame_index := -1


func _enter_tree() -> void:
	_dialog = AcceptDialog.new()
	_dialog.title = "Dotnet Diagnostics"
	get_editor_interface().get_base_control().add_child(_dialog)

	_poll_timer = Timer.new()
	_poll_timer.wait_time = 0.5
	_poll_timer.one_shot = false
	_poll_timer.timeout.connect(_poll_active_trace)
	get_editor_interface().get_base_control().add_child(_poll_timer)

	_build_ui()
	_window = Window.new()
	_window.title = "Dotnet Diagnostics"
	_window.min_size = Vector2i(1080, 620)
	_window.size = Vector2i(1380, 820)
	_window.visible = false
	_window.close_requested.connect(_close_panel)
	_window.add_child(_root_container)
	get_editor_interface().get_base_control().add_child(_window)

	_file_dialog = FileDialog.new()
	_file_dialog.access = FileDialog.ACCESS_FILESYSTEM
	_file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	_file_dialog.title = "打开 trace.speedscope.json"
	_file_dialog.filters = PackedStringArray([
		"*.speedscope.json ; Speedscope Trace",
		"*.json ; JSON"
	])
	_file_dialog.file_selected.connect(_on_trace_file_selected)
	get_editor_interface().get_base_control().add_child(_file_dialog)

	add_tool_menu_item(MENU_OPEN_PANEL, Callable(self, "_open_panel"))
	add_tool_menu_item(MENU_OPEN_DOCS, Callable(self, "_open_docs"))
	_refresh_processes()
	_set_viewer_state("idle", "点击“刷新最新 trace”读取仓库里最近一份 trace.speedscope.json。")


func _exit_tree() -> void:
	remove_tool_menu_item(MENU_OPEN_PANEL)
	remove_tool_menu_item(MENU_OPEN_DOCS)

	if is_instance_valid(_dialog):
		_dialog.queue_free()
	if is_instance_valid(_poll_timer):
		_poll_timer.queue_free()
	if is_instance_valid(_file_dialog):
		_file_dialog.queue_free()
	if is_instance_valid(_window):
		_window.queue_free()


func _build_ui() -> void:
	_root_container = MarginContainer.new()
	_root_container.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_root_container.add_theme_constant_override("margin_left", 12)
	_root_container.add_theme_constant_override("margin_top", 12)
	_root_container.add_theme_constant_override("margin_right", 12)
	_root_container.add_theme_constant_override("margin_bottom", 12)

	_tabs = TabContainer.new()
	_tabs.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_tabs.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_tabs.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_root_container.add_child(_tabs)

	_build_diagnostics_tab()
	_build_trace_viewer_tab()


func _build_diagnostics_tab() -> void:
	var diagnostics_page := VBoxContainer.new()
	diagnostics_page.name = "进程诊断"
	diagnostics_page.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	diagnostics_page.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_tabs.add_child(diagnostics_page)

	var toolbar := HBoxContainer.new()
	toolbar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	diagnostics_page.add_child(toolbar)

	_refresh_button = Button.new()
	_refresh_button.text = "刷新进程"
	_refresh_button.pressed.connect(_refresh_processes)
	toolbar.add_child(_refresh_button)

	_attach_button = Button.new()
	_attach_button.text = "附加 Trace"
	_attach_button.disabled = true
	_attach_button.pressed.connect(_attach_selected_process)
	toolbar.add_child(_attach_button)

	var duration_label := Label.new()
	duration_label.text = "时长"
	toolbar.add_child(duration_label)

	_duration_option = OptionButton.new()
	_duration_option.add_item("10 秒", 10)
	_duration_option.add_item("30 秒", 30)
	_duration_option.add_item("60 秒", 60)
	_duration_option.selected = 1
	toolbar.add_child(_duration_option)

	var open_viewer_button := Button.new()
	open_viewer_button.text = "打开 Trace Viewer"
	open_viewer_button.pressed.connect(func() -> void:
		_tabs.current_tab = TAB_VIEWER
	)
	toolbar.add_child(open_viewer_button)

	var hint := Label.new()
	hint.text = "选中一个进程后再附加，避免误连到旧的 Godot 进程。Viewer 页面只负责浏览已生成的 trace。"
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	hint.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	toolbar.add_child(hint)

	var split := VSplitContainer.new()
	split.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	split.size_flags_vertical = Control.SIZE_EXPAND_FILL
	split.split_offset = 320
	diagnostics_page.add_child(split)

	var tree_panel := PanelContainer.new()
	tree_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	tree_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
	split.add_child(tree_panel)

	_tree = Tree.new()
	_tree.columns = 6
	_tree.hide_root = true
	_tree.column_titles_visible = true
	_tree.set_column_title(0, "PID")
	_tree.set_column_title(1, "创建时间")
	_tree.set_column_title(2, "已运行")
	_tree.set_column_title(3, "类型")
	_tree.set_column_title(4, "编辑器启动")
	_tree.set_column_title(5, "命令行")
	_tree.set_column_expand(0, false)
	_tree.set_column_expand(1, false)
	_tree.set_column_expand(2, false)
	_tree.set_column_expand(3, false)
	_tree.set_column_expand(4, false)
	_tree.set_column_expand(5, true)
	_tree.set_column_custom_minimum_width(0, 72)
	_tree.set_column_custom_minimum_width(1, 168)
	_tree.set_column_custom_minimum_width(2, 100)
	_tree.set_column_custom_minimum_width(3, 92)
	_tree.set_column_custom_minimum_width(4, 120)
	_tree.set_column_custom_minimum_width(5, 640)
	_tree.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_tree.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_tree.custom_minimum_size = Vector2(0, 260)
	_tree.item_selected.connect(_on_tree_item_selected)
	tree_panel.add_child(_tree)

	var status_panel := PanelContainer.new()
	status_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	status_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
	split.add_child(status_panel)

	_status_label = RichTextLabel.new()
	_status_label.bbcode_enabled = false
	_status_label.fit_content = false
	_status_label.scroll_active = true
	_status_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_status_label.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_status_label.custom_minimum_size = Vector2(0, 140)
	_status_label.text = "点击“刷新进程”读取当前项目相关的 Godot 进程。"
	status_panel.add_child(_status_label)


func _build_trace_viewer_tab() -> void:
	var viewer_page := VBoxContainer.new()
	viewer_page.name = "Trace Viewer"
	viewer_page.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	viewer_page.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_tabs.add_child(viewer_page)

	var toolbar := HBoxContainer.new()
	toolbar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	viewer_page.add_child(toolbar)

	_viewer_refresh_button = Button.new()
	_viewer_refresh_button.text = "刷新最新 trace"
	_viewer_refresh_button.pressed.connect(_refresh_latest_trace)
	toolbar.add_child(_viewer_refresh_button)

	_viewer_open_button = Button.new()
	_viewer_open_button.text = "打开文件"
	_viewer_open_button.pressed.connect(_open_trace_file_dialog)
	toolbar.add_child(_viewer_open_button)

	var profile_label := Label.new()
	profile_label.text = "Profile"
	toolbar.add_child(profile_label)

	_profile_option = OptionButton.new()
	_profile_option.custom_minimum_size = Vector2(260, 0)
	_profile_option.item_selected.connect(_on_trace_profile_selected)
	toolbar.add_child(_profile_option)

	var frame_label := Label.new()
	frame_label.text = "帧范围"
	toolbar.add_child(frame_label)

	_frame_option = OptionButton.new()
	_frame_option.custom_minimum_size = Vector2(240, 0)
	_frame_option.disabled = true
	_frame_option.item_selected.connect(_on_frame_range_selected)
	toolbar.add_child(_frame_option)

	_zoom_button = Button.new()
	_zoom_button.text = "放大到选中节点"
	_zoom_button.disabled = true
	_zoom_button.pressed.connect(_zoom_to_selected_node)
	toolbar.add_child(_zoom_button)

	_back_button = Button.new()
	_back_button.text = "返回上一级"
	_back_button.disabled = true
	_back_button.pressed.connect(_navigate_back_from_zoom)
	toolbar.add_child(_back_button)

	_reset_button = Button.new()
	_reset_button.text = "重置到根"
	_reset_button.disabled = true
	_reset_button.pressed.connect(_reset_zoom_to_root)
	toolbar.add_child(_reset_button)

	var info_split := HSplitContainer.new()
	info_split.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	viewer_page.add_child(info_split)

	_viewer_file_label = Label.new()
	_viewer_file_label.text = "文件: 未加载"
	_viewer_file_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_viewer_file_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	info_split.add_child(_viewer_file_label)

	_viewer_profile_label = Label.new()
	_viewer_profile_label.text = "Profile: 未加载"
	_viewer_profile_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_viewer_profile_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	info_split.add_child(_viewer_profile_label)

	_viewer_status_label = RichTextLabel.new()
	_viewer_status_label.fit_content = true
	_viewer_status_label.scroll_active = false
	_viewer_status_label.custom_minimum_size = Vector2(0, 58)
	_viewer_status_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	viewer_page.add_child(_viewer_status_label)

	_viewer_warning_label = Label.new()
	_viewer_warning_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_viewer_warning_label.visible = false
	viewer_page.add_child(_viewer_warning_label)

	var main_split := HSplitContainer.new()
	main_split.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	main_split.size_flags_vertical = Control.SIZE_EXPAND_FILL
	main_split.split_offset = 900
	viewer_page.add_child(main_split)

	var graph_panel := PanelContainer.new()
	graph_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	graph_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
	main_split.add_child(graph_panel)

	var graph_box := VBoxContainer.new()
	graph_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	graph_box.size_flags_vertical = Control.SIZE_EXPAND_FILL
	graph_panel.add_child(graph_box)

	_viewer_stats_label = Label.new()
	_viewer_stats_label.text = "统计: 未加载"
	_viewer_stats_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_viewer_stats_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	graph_box.add_child(_viewer_stats_label)

	var graph_scroll := ScrollContainer.new()
	graph_scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	graph_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	graph_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	graph_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	graph_box.add_child(graph_scroll)

	_flamegraph = TraceFlamegraph.new()
	_flamegraph.custom_minimum_size = Vector2(0, 180)
	_flamegraph.node_selected.connect(_on_flamegraph_node_selected)
	graph_scroll.add_child(_flamegraph)

	var details_panel := PanelContainer.new()
	details_panel.custom_minimum_size = Vector2(320, 0)
	details_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	details_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
	main_split.add_child(details_panel)

	var details_box := VBoxContainer.new()
	details_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	details_box.size_flags_vertical = Control.SIZE_EXPAND_FILL
	details_panel.add_child(details_box)

	var details_title := Label.new()
	details_title.text = "节点详情"
	details_box.add_child(details_title)

	_viewer_details_label = RichTextLabel.new()
	_viewer_details_label.bbcode_enabled = false
	_viewer_details_label.fit_content = false
	_viewer_details_label.scroll_active = true
	_viewer_details_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_viewer_details_label.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_viewer_details_label.text = "选中 flamegraph 中的节点后，这里会显示完整函数名、累计权重、父节点和直接子节点摘要。"
	details_box.add_child(_viewer_details_label)


func _open_panel() -> void:
	if is_instance_valid(_window):
		_window.popup_centered_ratio(0.88)
	_refresh_processes()


func _close_panel() -> void:
	if is_instance_valid(_window):
		_window.hide()


func _viewer_ui_ready() -> bool:
	return _profile_option != null \
		and _frame_option != null \
		and _zoom_button != null \
		and _back_button != null \
		and _reset_button != null \
		and _viewer_status_label != null \
		and _viewer_file_label != null \
		and _viewer_profile_label != null \
		and _viewer_stats_label != null \
		and _viewer_warning_label != null \
		and _viewer_details_label != null


func _open_docs() -> void:
	OS.shell_open(ProjectSettings.globalize_path("res://docs/godot-editor-profiling.md"))


func _open_trace_file_dialog() -> void:
	if is_instance_valid(_file_dialog):
		_file_dialog.popup_centered_ratio(0.75)


func _attach_selected_process() -> void:
	if _selected_process_id <= 0:
		_show_message("请先在面板里选中一个要附加的 Godot 进程。")
		return

	var duration := "00:00:%02d" % int(_duration_option.get_selected_id())
	_run_trace(duration, _selected_process_id)


func _run_trace(duration: String, target_process_id: int) -> void:
	if OS.get_name() != "Windows":
		_show_message("当前插件只配置了 Windows PowerShell 入口。")
		return

	if _active_pid != -1 and OS.is_process_running(_active_pid):
		_show_message("已经有一个 trace 任务在运行，请等它结束后再启动新的。")
		return

	var script_path := ProjectSettings.globalize_path("res://tools/profiling/Attach-RunningGodotTrace.ps1")
	var status_dir := ProjectSettings.globalize_path("res://artifacts/dotnet-diagnostics/plugin-status")
	DirAccess.make_dir_recursive_absolute(status_dir)
	var status_file := status_dir.path_join("trace-status-%d.json" % Time.get_unix_time_from_system())
	var args := PackedStringArray([
		"-ExecutionPolicy",
		"Bypass",
		"-File",
		script_path,
		"-Duration",
		duration,
		"-ProcessId",
		str(target_process_id),
		"-StatusFile",
		status_file
	])

	var process_id := OS.create_process("powershell", args, true)
	if process_id == -1:
		_show_message("启动 trace 脚本失败。请确认 PowerShell 可用，并查看编辑器输出日志。")
		return

	_active_pid = process_id
	_active_status_file = status_file
	_poll_timer.start()
	_status_label.text = "已启动附加任务，目标 PID=%d。\n命令行：%s\n\n任务结束后这里会显示结果。" % [target_process_id, _selected_command_line]
	_set_viewer_state("loading", "正在等待新 trace 完成。完成后会自动刷新 Viewer。")
	_show_message("已经启动附加脚本。任务结束后插件会反馈成功或失败结果。")


func _poll_active_trace() -> void:
	if _active_pid == -1:
		_poll_timer.stop()
		return

	if OS.is_process_running(_active_pid):
		return

	_poll_timer.stop()
	var status := _read_status_file(_active_status_file)
	var state := str(status.get("state", "unknown"))
	var message := str(status.get("message", "任务结束，但没有读到状态详情。"))
	var session_dir := str(status.get("session_dir", ""))
	var trace_path := str(status.get("trace_path", ""))
	var speedscope_path := str(status.get("speedscope_path", ""))
	var extra := ""

	if not session_dir.is_empty():
		extra += "\n输出目录：%s" % session_dir
	if not trace_path.is_empty():
		extra += "\nTrace 文件：%s" % trace_path

	match state:
		"completed":
			_status_label.text = "trace 完成。%s%s" % [message, extra]
			_show_message("trace 完成。\n%s%s" % [message, extra])
		"completed_with_warning":
			_status_label.text = "trace 完成，但结果可疑。%s%s" % [message, extra]
			_show_message("trace 完成，但结果可疑。\n%s%s" % [message, extra])
		"failed":
			_status_label.text = "trace 失败。%s%s" % [message, extra]
			_show_message("trace 失败。\n%s%s" % [message, extra])
		_:
			_status_label.text = "trace 任务结束。%s%s" % [message, extra]
			_show_message("trace 任务结束。\n%s%s" % [message, extra])

	if not speedscope_path.is_empty():
		_latest_trace_status = status
		_load_trace_file(speedscope_path, status)
		_tabs.current_tab = TAB_VIEWER

	_active_pid = -1
	_active_status_file = ""
	_refresh_processes()


func _refresh_processes() -> void:
	if not is_instance_valid(_tree):
		return

	_selected_process_id = -1
	_selected_command_line = ""
	_attach_button.disabled = true
	_tree.clear()

	var root := _tree.create_item()
	var output: Array = []
	var script_path := ProjectSettings.globalize_path("res://tools/profiling/Get-RelatedGodotProcesses.ps1")
	var exit_code := OS.execute("powershell", PackedStringArray([
		"-ExecutionPolicy",
		"Bypass",
		"-File",
		script_path
	]), output, true)

	if exit_code != 0:
		_status_label.text = "读取进程失败，请确认 PowerShell 可用。"
		return

	var json_text := ""
	for line in output:
		json_text += str(line)

	var parsed = JSON.parse_string(json_text)
	if parsed == null:
		_status_label.text = "读取进程失败：返回结果不是合法 JSON。"
		return

	var processes: Array = []
	if typeof(parsed) == TYPE_ARRAY:
		processes = parsed
	elif typeof(parsed) == TYPE_DICTIONARY:
		processes = [parsed]

	for process_info in processes:
		var item := _tree.create_item(root)
		var pid := int(process_info.get("process_id", 0))
		var created_at := str(process_info.get("created_at", ""))
		var age_seconds := int(process_info.get("age_seconds", 0))
		var is_editor: bool = process_info.get("is_editor", false)
		var is_editor_game: bool = process_info.get("is_editor_launched_game", false)
		var is_headless: bool = process_info.get("is_headless", false)
		var command_line := str(process_info.get("command_line", ""))

		item.set_text(0, str(pid))
		item.set_text(1, _format_created_at(created_at))
		item.set_text(2, _format_age(age_seconds))
		item.set_text(3, _describe_process_type(is_editor, is_headless))
		item.set_text(4, "是" if is_editor_game else "否")
		item.set_text(5, command_line)
		item.set_metadata(0, process_info)

	if processes.is_empty():
		_status_label.text = "没有找到当前项目相关的 Godot 进程。"
	else:
		_status_label.text = "已读取 %d 个相关 Godot 进程。选中后可直接附加 trace。" % processes.size()


func _refresh_latest_trace() -> void:
	var latest := _resolve_latest_trace()
	if latest.is_empty():
		_set_viewer_state("error", "没有找到可加载的 trace.speedscope.json。请先运行 diagnostics trace。")
		return

	_load_trace_file(str(latest.get("path", "")), latest.get("status", {}))


func _resolve_latest_trace() -> Dictionary:
	var best := {}
	var status_dir := ProjectSettings.globalize_path("res://artifacts/dotnet-diagnostics/plugin-status")
	if DirAccess.dir_exists_absolute(status_dir):
		for status_file_name in DirAccess.get_files_at(status_dir):
			if not status_file_name.ends_with(".json"):
				continue
			var full_path := status_dir.path_join(status_file_name)
			var status := _read_status_file(full_path)
			var state := str(status.get("state", ""))
			var speedscope_path := str(status.get("speedscope_path", ""))
			if state not in ["completed", "completed_with_warning"]:
				continue
			if speedscope_path.is_empty() or not FileAccess.file_exists(speedscope_path):
				continue
			var sort_key := int(FileAccess.get_modified_time(full_path))
			if best.is_empty() or sort_key > int(best.get("sort_key", 0)):
				best = {
					"path": speedscope_path,
					"status": status,
					"sort_key": sort_key
				}
		if not best.is_empty():
			return best

	var fallback_dir := ProjectSettings.globalize_path("res://artifacts/dotnet-diagnostics")
	if not DirAccess.dir_exists_absolute(fallback_dir):
		return {}

	var files := _collect_matching_files(fallback_dir, "trace.speedscope.json")
	for trace_path in files:
		var sort_key := int(FileAccess.get_modified_time(trace_path))
		if best.is_empty() or sort_key > int(best.get("sort_key", 0)):
			best = {
				"path": trace_path,
				"status": {},
				"sort_key": sort_key
			}

	return best


func _collect_matching_files(root_path: String, target_name: String) -> Array:
	var matches: Array = []
	var dir := DirAccess.open(root_path)
	if dir == null:
		return matches

	dir.list_dir_begin()
	while true:
		var entry := dir.get_next()
		if entry.is_empty():
			break
		if entry.begins_with("."):
			continue
		var full_path := root_path.path_join(entry)
		if dir.current_is_dir():
			matches.append_array(_collect_matching_files(full_path, target_name))
		elif entry == target_name:
			matches.append(full_path)
	dir.list_dir_end()
	return matches


func _load_trace_file(path: String, status_hint: Dictionary = {}) -> void:
	if path.is_empty():
		_set_viewer_state("error", "没有可加载的 trace 文件路径。")
		return

	_set_viewer_state("loading", "正在加载 trace 文件：%s" % path)
	var document := TraceSpeedscopeLoader.load_document(path, status_hint)
	_trace_document = document
	if _viewer_file_label != null:
		_viewer_file_label.text = "文件: %s" % path

	if not document.get("ok", false):
		if _profile_option != null:
			_profile_option.clear()
		if _frame_option != null:
			_frame_option.clear()
			_frame_option.disabled = true
		_selected_profile_index = -1
		_selected_frame_index = -1
		_current_profile = {}
		_current_root = {}
		_current_zoom_stack.clear()
		_selected_node = {}
		if _flamegraph != null:
			_flamegraph.clear_graph()
		if _zoom_button != null:
			_zoom_button.disabled = true
		if _back_button != null:
			_back_button.disabled = true
		if _reset_button != null:
			_reset_button.disabled = true
		if _viewer_profile_label != null:
			_viewer_profile_label.text = "Profile: 不可用"
		if _viewer_stats_label != null:
			_viewer_stats_label.text = "统计: 无可用 profile"
		if _viewer_warning_label != null:
			_viewer_warning_label.visible = not document.get("warnings", []).is_empty()
			_viewer_warning_label.text = "\n".join(document.get("warnings", []))
		if _viewer_details_label != null:
			_viewer_details_label.text = "无法显示 flamegraph。\n\n%s" % str(document.get("error", "未知错误"))
		_set_viewer_state("error", str(document.get("error", "无法解析 trace 文件。")))
		return

	if not _viewer_ui_ready():
		return

	_rebuild_profile_options()
	var preferred_index := _selected_profile_index
	if preferred_index == -1:
		var supported_indices: Array = document.get("supported_profile_indices", [])
		preferred_index = int(supported_indices[0]) if not supported_indices.is_empty() else 0
	_activate_profile(preferred_index)


func _rebuild_profile_options() -> void:
	if _profile_option == null:
		return
	_profile_option.clear()
	var profiles: Array = _trace_document.get("profiles", [])
	for profile in profiles:
		var label := "%s (%s)" % [str(profile.get("name", "Profile")), str(profile.get("type", ""))]
		if not profile.get("supported", false):
			label += " - 暂不支持"
		_profile_option.add_item(label)
		_profile_option.set_item_metadata(_profile_option.item_count - 1, int(profile.get("index", 0)))


func _activate_profile(profile_index: int) -> void:
	if not _viewer_ui_ready():
		return

	var profiles: Array = _trace_document.get("profiles", [])
	for option_index in range(_profile_option.item_count):
		if int(_profile_option.get_item_metadata(option_index)) == profile_index:
			_profile_option.select(option_index)
			break

	for profile in profiles:
		if int(profile.get("index", -1)) != profile_index:
			continue

		_selected_profile_index = profile_index
		_selected_frame_index = -1
		_current_profile = profile
		_current_zoom_stack.clear()
		_selected_node = {}
		_viewer_profile_label.text = "Profile: %s | 类型: %s" % [
			str(profile.get("name", "Profile")),
			str(profile.get("type", ""))
		]

		_rebuild_frame_options(profile)

		if not profile.get("supported", false):
			_current_root = {}
			if _flamegraph != null:
				_flamegraph.clear_graph()
			_zoom_button.disabled = true
			_back_button.disabled = true
			_reset_button.disabled = true
			_viewer_stats_label.text = _format_profile_stats(profile)
			_viewer_warning_label.visible = true
			_viewer_warning_label.text = str(profile.get("error", "该 profile 暂不支持。"))
			_viewer_details_label.text = "当前 profile 类型为 %s，当前 Viewer 暂不支持该类型。" % str(profile.get("type", ""))
			_set_viewer_state("warning", "当前 profile 暂不支持，可切换到其他可用 profile。")
			return

		_apply_profile_root_view(profile, {})
		_viewer_stats_label.text = _format_profile_stats(profile)
		var warnings: Array = []
		warnings.append_array(_trace_document.get("warnings", []))
		warnings.append_array(profile.get("warnings", []))
		_viewer_warning_label.visible = not warnings.is_empty()
		_viewer_warning_label.text = "\n".join(warnings)

		if int(profile.get("sample_count", 0)) <= 0:
			_viewer_details_label.text = "trace 内容为空或不支持。"
			_set_viewer_state("warning", "当前 profile 没有可用样本。")
			return

		_viewer_details_label.text = "选中 flamegraph 中的节点后，这里会显示完整函数名、累计权重、父节点和直接子节点摘要。"
		_back_button.disabled = true
		_reset_button.disabled = true
		_zoom_button.disabled = true
		_set_viewer_state("ready", "解析完成，可浏览 flamegraph 并切换 profile。")
		return


func _rebuild_frame_options(profile: Dictionary) -> void:
	if _frame_option == null:
		return

	_frame_option.clear()
	_frame_option.add_item("全部聚合")
	_frame_option.set_item_metadata(0, -1)

	var frame_views: Array = profile.get("frame_views", [])
	for frame_view in frame_views:
		var label := str(frame_view.get("label", "Frame"))
		_frame_option.add_item(label)
		_frame_option.set_item_metadata(_frame_option.item_count - 1, int(frame_view.get("index", -1)))

	_frame_option.disabled = frame_views.is_empty()
	_frame_option.select(0)


func _apply_profile_root_view(profile: Dictionary, frame_view: Dictionary) -> void:
	_current_zoom_stack.clear()
	_selected_node = {}
	if frame_view.is_empty():
		_selected_frame_index = -1
		_current_root = profile.get("root", {})
	else:
		_selected_frame_index = int(frame_view.get("index", -1))
		_current_root = frame_view.get("root", {})

	if _flamegraph != null:
		_flamegraph.set_trace_root(_current_root)


func _get_active_base_root() -> Dictionary:
	if _current_profile.is_empty():
		return {}
	if _selected_frame_index < 0:
		return _current_profile.get("root", {})
	var frame_views: Array = _current_profile.get("frame_views", [])
	for frame_view in frame_views:
		if int(frame_view.get("index", -1)) == _selected_frame_index:
			return frame_view.get("root", {})
	return _current_profile.get("root", {})


func _format_profile_stats(profile: Dictionary) -> String:
	var frames: Array = _trace_document.get("frames", [])
	var current_frame_label := "全部聚合"
	if _selected_frame_index >= 0:
		var frame_views: Array = profile.get("frame_views", [])
		for frame_view in frame_views:
			if int(frame_view.get("index", -1)) == _selected_frame_index:
				current_frame_label = str(frame_view.get("label", current_frame_label))
				break
	return "统计: frame %d | sample %d | profile %d | 最大栈深度 %d" % [
		frames.size(),
		int(profile.get("sample_count", 0)),
		int(_trace_document.get("profile_count", 0)),
		int(profile.get("max_depth", 0))
	] + " | 视图: %s" % current_frame_label


func _set_viewer_state(state: String, message: String) -> void:
	if _viewer_status_label == null:
		return
	var prefix := "Trace Viewer"
	match state:
		"loading":
			prefix = "正在加载"
		"parsing":
			prefix = "正在解析"
		"ready":
			prefix = "已就绪"
		"warning":
			prefix = "注意"
		"error":
			prefix = "错误"
		_:
			prefix = "Trace Viewer"
	_viewer_status_label.text = "%s\n%s" % [prefix, message]


func _on_tree_item_selected() -> void:
	var item := _tree.get_selected()
	if item == null:
		_selected_process_id = -1
		_selected_command_line = ""
		_attach_button.disabled = true
		return

	var process_info: Dictionary = item.get_metadata(0)
	_selected_process_id = int(process_info.get("process_id", -1))
	_selected_command_line = str(process_info.get("command_line", ""))
	_attach_button.disabled = _selected_process_id <= 0
	_status_label.text = "已选中 PID=%d。命令行：%s" % [_selected_process_id, _selected_command_line]


func _on_trace_file_selected(path: String) -> void:
	_tabs.current_tab = TAB_VIEWER
	_load_trace_file(path, {})


func _on_trace_profile_selected(option_index: int) -> void:
	if _profile_option == null:
		return
	if option_index < 0 or option_index >= _profile_option.item_count:
		return
	var profile_index := int(_profile_option.get_item_metadata(option_index))
	_set_viewer_state("parsing", "正在构建 profile 视图：%s" % _profile_option.get_item_text(option_index))
	_activate_profile(profile_index)


func _on_frame_range_selected(option_index: int) -> void:
	if _frame_option == null or _current_profile.is_empty():
		return
	if option_index < 0 or option_index >= _frame_option.item_count:
		return

	var frame_index := int(_frame_option.get_item_metadata(option_index))
	var frame_views: Array = _current_profile.get("frame_views", [])
	var selected_view: Dictionary = {}
	for frame_view in frame_views:
		if int(frame_view.get("index", -1)) == frame_index:
			selected_view = frame_view
			break

	_apply_profile_root_view(_current_profile, selected_view)
	_viewer_stats_label.text = _format_profile_stats(_current_profile)
	_update_details_panel()
	_set_viewer_state("ready", "已切换到 %s。" % _frame_option.get_item_text(option_index))


func _on_flamegraph_node_selected(node: Dictionary) -> void:
	_selected_node = node
	if _zoom_button != null:
		_zoom_button.disabled = _selected_node.is_empty() or _matches_node(_selected_node, _current_root)
	_update_details_panel()


func _zoom_to_selected_node() -> void:
	if _selected_node.is_empty():
		return
	if _matches_node(_selected_node, _current_root):
		return

	_current_zoom_stack.append(_current_root)
	_current_root = _selected_node
	_selected_node = _current_root
	if _flamegraph != null:
		_flamegraph.set_trace_root(_current_root, _selected_node)
	if _back_button != null:
		_back_button.disabled = _current_zoom_stack.is_empty()
	if _reset_button != null:
		_reset_button.disabled = _current_zoom_stack.is_empty()
	if _zoom_button != null:
		_zoom_button.disabled = true
	_update_details_panel()
	_set_viewer_state("ready", "已放大到选中节点的子树。")


func _navigate_back_from_zoom() -> void:
	if _current_zoom_stack.is_empty():
		return
	_current_root = _current_zoom_stack.pop_back()
	_selected_node = _current_root
	if _flamegraph != null:
		_flamegraph.set_trace_root(_current_root, _selected_node)
	if _back_button != null:
		_back_button.disabled = _current_zoom_stack.is_empty()
	if _reset_button != null:
		_reset_button.disabled = _current_zoom_stack.is_empty()
	if _zoom_button != null:
		_zoom_button.disabled = true
	_update_details_panel()
	_set_viewer_state("ready", "已返回上一级视图。")


func _reset_zoom_to_root() -> void:
	if _current_profile.is_empty():
		return
	_current_zoom_stack.clear()
	_current_root = _get_active_base_root()
	_selected_node = {}
	if _flamegraph != null:
		_flamegraph.set_trace_root(_current_root)
	if _back_button != null:
		_back_button.disabled = true
	if _reset_button != null:
		_reset_button.disabled = true
	if _zoom_button != null:
		_zoom_button.disabled = true
	_update_details_panel()
	_set_viewer_state("ready", "已重置到根视图。")


func _update_details_panel() -> void:
	if _viewer_details_label == null:
		return
	if _selected_node.is_empty():
		_viewer_details_label.text = "选中 flamegraph 中的节点后，这里会显示完整函数名、累计权重、父节点和直接子节点摘要。"
		return

	var full_name := str(_selected_node.get("full_name", ""))
	var cumulative_weight := float(_selected_node.get("cumulative_weight", 0.0))
	var total_weight := max(float(_current_root.get("cumulative_weight", 0.0)), 0.0)
	var percent := 0.0
	if total_weight > 0.0:
		percent = cumulative_weight / total_weight * 100.0

	var parent_name := _find_parent_name(_current_root, _selected_node)
	var child_summary := _build_child_summary(_selected_node)
	var zoom_path := _build_zoom_path()

	_viewer_details_label.text = "完整函数名:\n%s\n\n累计权重: %s (%.2f%%)\n父节点: %s\n当前缩放路径: %s\n\n直接子节点摘要:\n%s" % [
		full_name,
		_format_weight(cumulative_weight),
		percent,
		parent_name,
		zoom_path,
		child_summary
	]


func _build_child_summary(node: Dictionary) -> String:
	var children: Array = node.get("children", [])
	if children.is_empty():
		return "无直接子节点。"

	var lines: Array = []
	var total := max(float(node.get("cumulative_weight", 0.0)), 0.0)
	for child in children.slice(0, 8):
		var child_weight := float(child.get("cumulative_weight", 0.0))
		var ratio := 0.0
		if total > 0.0:
			ratio = child_weight / total * 100.0
		lines.append("- %s | %s | %.2f%%" % [
			str(child.get("name", "")),
			_format_weight(child_weight),
			ratio
		])
	if children.size() > 8:
		lines.append("- 其余 %d 个子节点未展开" % (children.size() - 8))
	return "\n".join(lines)


func _build_zoom_path() -> String:
	var names: Array = []
	for node in _current_zoom_stack:
		names.append(str(node.get("name", "")))
	if not _current_root.is_empty():
		names.append(str(_current_root.get("name", "")))
	return " > ".join(names) if not names.is_empty() else "(root)"


func _find_parent_name(root: Dictionary, target: Dictionary) -> String:
	if root.is_empty() or target.is_empty():
		return "(root)"
	if _matches_node(root, target):
		return "(root)"
	var found := _find_parent_node(root, target)
	if found.is_empty():
		return "(root)"
	return str(found.get("full_name", "(root)"))


func _find_parent_node(node: Dictionary, target: Dictionary) -> Dictionary:
	for child in node.get("children", []):
		if _matches_node(child, target):
			return node
		var nested := _find_parent_node(child, target)
		if not nested.is_empty():
			return nested
	return {}


func _matches_node(left: Dictionary, right: Dictionary) -> bool:
	if left.is_empty() or right.is_empty():
		return false
	return str(left.get("path_id", "")) == str(right.get("path_id", ""))


func _format_weight(weight: float) -> String:
	if absf(weight - round(weight)) < 0.0001:
		return str(int(round(weight)))
	return "%.2f" % weight


func _format_age(age_seconds: int) -> String:
	if age_seconds < 60:
		return "%ds" % age_seconds
	if age_seconds < 3600:
		return "%dm %02ds" % [age_seconds / 60, age_seconds % 60]
	return "%dh %02dm" % [age_seconds / 3600, (age_seconds % 3600) / 60]


func _format_created_at(created_at: String) -> String:
	if created_at.is_empty():
		return ""
	var simplified := created_at
	var plus_index := simplified.find("+")
	if plus_index != -1:
		simplified = simplified.substr(0, plus_index)
	return simplified.replace("T", " ")


func _describe_process_type(is_editor: bool, is_headless: bool) -> String:
	if is_editor:
		return "编辑器"
	if is_headless:
		return "Headless"
	return "游戏"


func _read_status_file(path: String) -> Dictionary:
	if path.is_empty() or not FileAccess.file_exists(path):
		return {}
	var content := FileAccess.get_file_as_string(path)
	var parsed = JSON.parse_string(content)
	if typeof(parsed) == TYPE_DICTIONARY:
		return parsed
	return {}


func _show_message(text: String) -> void:
	if is_instance_valid(_dialog):
		_dialog.dialog_text = text
		_dialog.popup_centered()
