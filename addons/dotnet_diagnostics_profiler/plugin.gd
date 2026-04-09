@tool
extends EditorPlugin

const MENU_OPEN_PANEL := "Diagnostics: Open Process Panel"
const MENU_OPEN_DOCS := "Diagnostics: Open Profiling Docs"

var _dialog: AcceptDialog
var _poll_timer: Timer
var _window: Window
var _active_pid: int = -1
var _active_status_file := ""
var _root_container: MarginContainer
var _dock: VBoxContainer
var _tree: Tree
var _status_label: RichTextLabel
var _duration_option: OptionButton
var _refresh_button: Button
var _attach_button: Button
var _selected_process_id: int = -1
var _selected_command_line := ""

func _enter_tree() -> void:
    _dialog = AcceptDialog.new()
    _dialog.title = "Dotnet Diagnostics"
    get_editor_interface().get_base_control().add_child(_dialog)

    _poll_timer = Timer.new()
    _poll_timer.wait_time = 0.5
    _poll_timer.one_shot = false
    _poll_timer.timeout.connect(_poll_active_trace)
    get_editor_interface().get_base_control().add_child(_poll_timer)

    _build_dock()
    _window = Window.new()
    _window.title = "Dotnet Diagnostics"
    _window.min_size = Vector2i(960, 420)
    _window.size = Vector2i(1200, 520)
    _window.visible = false
    _window.add_child(_root_container)
    get_editor_interface().get_base_control().add_child(_window)

    add_tool_menu_item(MENU_OPEN_PANEL, Callable(self, "_open_panel"))
    add_tool_menu_item(MENU_OPEN_DOCS, Callable(self, "_open_docs"))
    _refresh_processes()


func _exit_tree() -> void:
    remove_tool_menu_item(MENU_OPEN_PANEL)
    remove_tool_menu_item(MENU_OPEN_DOCS)

    if is_instance_valid(_dialog):
        _dialog.queue_free()

    if is_instance_valid(_poll_timer):
        _poll_timer.queue_free()

    if is_instance_valid(_window):
        _window.queue_free()


func _build_dock() -> void:
    _root_container = MarginContainer.new()
    _root_container.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    _root_container.add_theme_constant_override("margin_left", 12)
    _root_container.add_theme_constant_override("margin_top", 12)
    _root_container.add_theme_constant_override("margin_right", 12)
    _root_container.add_theme_constant_override("margin_bottom", 12)

    _dock = VBoxContainer.new()
    _dock.name = "Diagnostics"
    _dock.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    _dock.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    _dock.size_flags_vertical = Control.SIZE_EXPAND_FILL
    _root_container.add_child(_dock)

    var toolbar := HBoxContainer.new()
    toolbar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    _dock.add_child(toolbar)

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

    var hint := Label.new()
    hint.text = "选中一个进程后再附加，避免误连到旧的 Godot 进程。"
    hint.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    toolbar.add_child(hint)

    var split := VSplitContainer.new()
    split.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    split.size_flags_vertical = Control.SIZE_EXPAND_FILL
    split.split_offset = 320
    _dock.add_child(split)

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
    _status_label.custom_minimum_size = Vector2(0, 120)
    _status_label.text = "点击“刷新进程”读取当前项目相关的 Godot 进程。"
    status_panel.add_child(_status_label)



func _open_panel() -> void:
    if is_instance_valid(_window):
        _window.popup_centered_ratio(0.8)
    _refresh_processes()


func _open_docs() -> void:
    OS.shell_open(ProjectSettings.globalize_path("res://docs/godot-editor-profiling.md"))


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
        var is_editor := bool(process_info.get("is_editor", false))
        var is_editor_game := bool(process_info.get("is_editor_launched_game", false))
        var is_headless := bool(process_info.get("is_headless", false))
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
