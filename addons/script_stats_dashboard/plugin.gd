@tool
extends EditorPlugin

const Generator := preload("res://addons/script_stats_dashboard/stats_generator.gd")
const TOOL_MENU_NAME := "脚本统计仪表盘"

var _button: Button

func _enter_tree() -> void:
	# 1) 顶部工具栏按钮
	_button = Button.new()
	_button.text = "📊 脚本统计"
	_button.tooltip_text = "打开脚本统计仪表盘（在浏览器中展示）"
	_button.pressed.connect(_on_open_dashboard)
	add_control_to_container(EditorPlugin.CONTAINER_TOOLBAR, _button)

	# 2) 项目 -> 工具 菜单项（更稳定，一定找得到）
	add_tool_menu_item(TOOL_MENU_NAME, _on_open_dashboard)


func _exit_tree() -> void:
	if _button:
		remove_control_from_container(EditorPlugin.CONTAINER_TOOLBAR, _button)
		_button.queue_free()
		_button = null
	remove_tool_menu_item(TOOL_MENU_NAME)


func _on_open_dashboard() -> void:
	var gen := Generator.new()
	gen.generate_and_open()
