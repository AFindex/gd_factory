@tool
extends RefCounted

class_name TraceSpeedscopeLoader

const SUSPICIOUS_FILE_SIZE_BYTES := 4096
const SUSPICIOUS_FRAME_COUNT := 5
const SUSPICIOUS_SAMPLE_COUNT := 2

static func load_document(path: String, status_hint: Dictionary = {}) -> Dictionary:
	var result := {
		"ok": false,
		"error": "",
		"warnings": [],
		"file_path": path,
		"document_name": "",
		"profile_count": 0,
		"frames": [],
		"profiles": [],
		"supported_profile_indices": [],
		"status_hint": status_hint
	}

	if path.is_empty():
		result.error = "没有可加载的 trace 文件路径。"
		return result

	if not FileAccess.file_exists(path):
		result.error = "trace 文件不存在：%s" % path
		return result

	var file_size := FileAccess.get_file_as_bytes(path).size()
	if file_size <= 0:
		result.error = "trace 文件为空。"
		return result

	var text := FileAccess.get_file_as_string(path)
	if text.strip_edges().is_empty():
		result.error = "trace 文件内容为空。"
		return result

	var parsed = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		result.error = "trace 文件不是合法的 speedscope JSON 对象。"
		return result

	var document: Dictionary = parsed
	var shared = document.get("shared", {})
	if typeof(shared) != TYPE_DICTIONARY:
		result.error = "trace 缺少 shared 数据。"
		return result

	var raw_frames = shared.get("frames", [])
	if typeof(raw_frames) != TYPE_ARRAY:
		result.error = "trace 缺少 shared.frames。"
		return result

	var frame_names: Array = []
	for frame_entry in raw_frames:
		if typeof(frame_entry) == TYPE_DICTIONARY:
			frame_names.append(str(frame_entry.get("name", "<unnamed frame>")))
		else:
			frame_names.append(str(frame_entry))

	var raw_profiles = document.get("profiles", [])
	if typeof(raw_profiles) != TYPE_ARRAY or raw_profiles.is_empty():
		result.error = "trace 不包含任何 profile。"
		return result

	result.document_name = str(document.get("name", path.get_file()))
	result.profile_count = raw_profiles.size()
	result.frames = frame_names

	var supported_indices: Array = []
	var saw_sampled := false
	var saw_evented := false
	for profile_index in range(raw_profiles.size()):
		var profile_result := _parse_profile(profile_index, raw_profiles[profile_index], frame_names)
		result.profiles.append(profile_result)
		if str(profile_result.get("type", "")) == "sampled":
			saw_sampled = true
		elif str(profile_result.get("type", "")) == "evented":
			saw_evented = true
		if profile_result.get("supported", false):
			supported_indices.append(profile_index)
		for warning in profile_result.get("warnings", []):
			result.warnings.append(warning)

	if str(status_hint.get("state", "")) == "completed_with_warning":
		result.warnings.append("最近一次 diagnostics 状态已标记该 trace 结果可疑，请谨慎解读。")

	if file_size < SUSPICIOUS_FILE_SIZE_BYTES:
		result.warnings.append("trace 文件非常小（%d 字节），结果可能不完整。" % file_size)

	if frame_names.size() < SUSPICIOUS_FRAME_COUNT:
		result.warnings.append("frame 数量异常少，结果可能无效。")

	if not saw_sampled and saw_evented:
		result.warnings.append("当前仓库 trace 仅包含 evented profile，Viewer 已启用兼容聚合模式。")

	result.supported_profile_indices = supported_indices
	result.ok = supported_indices.size() > 0

	if supported_indices.is_empty():
		result.error = "该 trace 没有可用的 sampled 或兼容 evented profile。"
		return result

	return result


static func _parse_profile(profile_index: int, profile_value, frame_names: Array) -> Dictionary:
	var profile := {
		"index": profile_index,
		"name": "Profile %d" % profile_index,
		"type": "",
		"supported": false,
		"warnings": [],
		"error": "",
		"sample_count": 0,
		"max_depth": 0,
		"total_weight": 0.0,
		"root": {},
		"unit": "",
		"start_value": 0.0,
		"end_value": 0.0,
		"frame_views": []
	}

	if typeof(profile_value) != TYPE_DICTIONARY:
		profile.error = "profile 结构无效。"
		return profile

	var raw_profile: Dictionary = profile_value
	profile.name = str(raw_profile.get("name", profile.name))
	profile.type = str(raw_profile.get("type", ""))
	profile.unit = str(raw_profile.get("unit", ""))
	profile.start_value = float(raw_profile.get("startValue", 0.0))
	profile.end_value = float(raw_profile.get("endValue", 0.0))

	if profile.type == "sampled":
		return _parse_sampled_profile(profile, raw_profile, frame_names)
	if profile.type == "evented":
		return _parse_evented_profile(profile, raw_profile, frame_names)

	profile.error = "profile 类型 %s 暂不支持。" % profile.type
	return profile


static func _parse_sampled_profile(profile: Dictionary, raw_profile: Dictionary, frame_names: Array) -> Dictionary:
	profile["compatibility_mode"] = "sampled"

	var raw_samples = raw_profile.get("samples", [])
	if typeof(raw_samples) != TYPE_ARRAY:
		profile.error = "sampled profile 缺少 samples。"
		return profile

	var raw_weights = raw_profile.get("weights", [])
	if typeof(raw_weights) != TYPE_ARRAY:
		raw_weights = []

	profile.sample_count = raw_samples.size()
	if raw_samples.is_empty():
		profile.error = "sampled profile 没有样本。"
		profile.warnings.append("%s 没有可用样本。" % profile.name)
		return profile

	if raw_samples.size() < SUSPICIOUS_SAMPLE_COUNT:
		profile.warnings.append("%s 的样本数很少，结果可能不稳定。" % profile.name)

	var root := _new_node("(root)", "(root)", "root")
	var max_depth := 0
	var total_weight := 0.0

	for sample_index in range(raw_samples.size()):
		var sample_value = raw_samples[sample_index]
		if typeof(sample_value) != TYPE_ARRAY:
			continue

		var sample_stack: Array = sample_value
		if sample_stack.is_empty():
			continue

		var weight := 1.0
		if sample_index < raw_weights.size():
			weight = max(float(raw_weights[sample_index]), 0.0)
		if weight <= 0.0:
			continue

		total_weight += weight
		root["cumulative_weight"] += weight
		max_depth = maxi(max_depth, sample_stack.size())

		var current: Dictionary = root
		for depth in range(sample_stack.size()):
			var frame_index := int(sample_stack[depth])
			var frame_name := _resolve_frame_name(frame_names, frame_index)
			var child_key := str(frame_index)
			var child_lookup: Dictionary = current["child_lookup"]
			if not child_lookup.has(child_key):
				var path_id := "%s/%s" % [str(current.get("path_id", "root")), child_key]
				child_lookup[child_key] = _new_node(frame_name, frame_name, path_id)
			var child: Dictionary = child_lookup[child_key]
			child["cumulative_weight"] += weight
			if depth == sample_stack.size() - 1:
				child["self_weight"] += weight
			current = child

	_finalize_node(root)
	profile.root = root
	profile.max_depth = max_depth
	profile.total_weight = total_weight
	profile.supported = total_weight > 0.0
	if total_weight <= 0.0:
		profile.error = "sampled profile 没有正权重样本。"
	else:
		if max_depth <= 1:
			profile.warnings.append("%s 的调用栈非常浅，结果可能过短。" % profile.name)

	return profile


static func _parse_evented_profile(profile: Dictionary, raw_profile: Dictionary, frame_names: Array) -> Dictionary:
	profile["compatibility_mode"] = "evented"

	var raw_events = raw_profile.get("events", [])
	if typeof(raw_events) != TYPE_ARRAY:
		profile.error = "evented profile 缺少 events。"
		return profile

	if raw_events.is_empty():
		profile.error = "evented profile 没有事件。"
		profile.warnings.append("%s 没有可用事件。" % profile.name)
		return profile

	var root := _new_node("(root)", "(root)", "root")
	var stack: Array = []
	var max_depth := 0
	var total_weight := 0.0
	var closed_frame_count := 0
	var invocations: Array = []

	for event_value in raw_events:
		if typeof(event_value) != TYPE_DICTIONARY:
			continue
		var event: Dictionary = event_value
		var event_type := str(event.get("type", ""))
		var frame_index := int(event.get("frame", -1))
		var at := float(event.get("at", 0.0))

		if event_type == "O":
			stack.append({
				"frame_index": frame_index,
				"name": _resolve_frame_name(frame_names, frame_index),
				"start": at,
			})
			max_depth = maxi(max_depth, stack.size())
			continue

		if event_type != "C" or stack.is_empty():
			continue

		var popped: Dictionary = stack.pop_back()
		if int(popped.get("frame_index", -1)) != frame_index:
			var repaired := _repair_event_stack(stack, popped, frame_index)
			popped = repaired.get("node", popped)
			stack = repaired.get("stack", stack)

		var duration := max(at - float(popped.get("start", at)), 0.0)
		if duration <= 0.0:
			continue

		closed_frame_count += 1
		var closing_stack := stack.duplicate()
		closing_stack.append(popped)
		invocations.append({
			"stack": closing_stack.duplicate(true),
			"start": float(popped.get("start", at)),
			"end": at,
			"duration": duration,
			"leaf_name": str(popped.get("name", "")),
			"frame_marker": _extract_frame_marker_from_stack(closing_stack),
		})

		if closing_stack.size() == 1:
			root["cumulative_weight"] += duration
			total_weight += duration

		var current: Dictionary = root
		for depth in range(closing_stack.size()):
			var entry: Dictionary = closing_stack[depth]
			var child_key := str(entry.get("frame_index", -1))
			var child_lookup: Dictionary = current["child_lookup"]
			if not child_lookup.has(child_key):
				var path_id := "%s/%s" % [str(current.get("path_id", "root")), child_key]
				child_lookup[child_key] = _new_node(str(entry.get("name", "")), str(entry.get("name", "")), path_id)
			var child: Dictionary = child_lookup[child_key]
			if depth == closing_stack.size() - 1:
				child["cumulative_weight"] += duration
				child["self_weight"] += duration
			current = child

	if total_weight <= 0.0:
		total_weight = max(profile.end_value - profile.start_value, 0.0)
		root["cumulative_weight"] = total_weight

	_finalize_node(root)
	profile.root = root
	profile.max_depth = max_depth
	profile.sample_count = closed_frame_count
	profile.total_weight = total_weight
	profile.supported = total_weight > 0.0
	profile.frame_views = _build_evented_frame_views(invocations)
	if not profile.supported:
		profile.error = "evented profile 无法聚合出可用持续时间。"
	else:
		profile.warnings.append("%s 使用 evented 兼容聚合，权重表示持续时间而不是 sampled 权重。" % profile.name)
		if profile.frame_views.is_empty():
			profile.warnings.append("%s 没有识别到明确的帧边界，将只显示聚合视图。" % profile.name)

	return profile


static func _repair_event_stack(stack: Array, popped: Dictionary, frame_index: int) -> Dictionary:
	var repaired_stack := stack.duplicate()
	var repaired_node := popped
	for reverse_index in range(repaired_stack.size() - 1, -1, -1):
		var candidate: Dictionary = repaired_stack[reverse_index]
		if int(candidate.get("frame_index", -1)) == frame_index:
			repaired_node = candidate
			repaired_stack = repaired_stack.slice(0, reverse_index)
			return {
				"stack": repaired_stack,
				"node": repaired_node,
			}
	return {
		"stack": repaired_stack,
		"node": repaired_node,
	}


static func _new_node(name: String, full_name: String, path_id: String) -> Dictionary:
	return {
		"name": name,
		"full_name": full_name,
		"path_id": path_id,
		"cumulative_weight": 0.0,
		"self_weight": 0.0,
		"children": [],
		"child_lookup": {},
		"child_count": 0
	}


static func _build_evented_frame_views(invocations: Array) -> Array:
	var markers: Array = []
	for invocation in invocations:
		if not str(invocation.get("frame_marker", "")).is_empty():
			markers.append(invocation)

	if markers.is_empty():
		return []

	markers.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return float(a.get("end", 0.0)) < float(b.get("end", 0.0))
	)

	var frame_views: Array = []
	var previous_end := -INF
	for marker_index in range(markers.size()):
		var marker: Dictionary = markers[marker_index]
		var marker_end := float(marker.get("end", 0.0))
		var bucket_invocations: Array = []
		for invocation in invocations:
			var invocation_end := float(invocation.get("end", 0.0))
			if invocation_end > previous_end and invocation_end <= marker_end:
				bucket_invocations.append(invocation)

		var aggregated := _aggregate_invocations(bucket_invocations)
		if float(aggregated.get("total_weight", 0.0)) <= 0.0:
			previous_end = marker_end
			continue

		frame_views.append({
			"index": frame_views.size(),
			"name": "Frame %d" % frame_views.size(),
			"marker_name": str(marker.get("frame_marker", "")),
			"start": previous_end if previous_end > -INF else float(marker.get("start", 0.0)),
			"end": marker_end,
			"duration": float(aggregated.get("total_weight", 0.0)),
			"root": aggregated.get("root", {}),
			"total_weight": aggregated.get("total_weight", 0.0),
			"max_depth": aggregated.get("max_depth", 0),
			"label": "Frame %d | %s" % [frame_views.size(), _summarize_marker_name(str(marker.get("frame_marker", "")))],
		})
		previous_end = marker_end

	return frame_views


static func _aggregate_invocations(invocations: Array) -> Dictionary:
	var root := _new_node("(root)", "(root)", "root")
	var max_depth := 0
	var total_weight := 0.0

	for invocation in invocations:
		var stack_entries: Array = invocation.get("stack", [])
		var duration := max(float(invocation.get("duration", 0.0)), 0.0)
		if stack_entries.is_empty() or duration <= 0.0:
			continue

		total_weight += duration
		root["cumulative_weight"] += duration
		max_depth = maxi(max_depth, stack_entries.size())

		var current: Dictionary = root
		for depth in range(stack_entries.size()):
			var entry: Dictionary = stack_entries[depth]
			var child_key := str(entry.get("frame_index", -1))
			var child_lookup: Dictionary = current["child_lookup"]
			if not child_lookup.has(child_key):
				var path_id := "%s/%s" % [str(current.get("path_id", "root")), child_key]
				child_lookup[child_key] = _new_node(str(entry.get("name", "")), str(entry.get("name", "")), path_id)
			var child: Dictionary = child_lookup[child_key]
			child["cumulative_weight"] += duration
			if depth == stack_entries.size() - 1:
				child["self_weight"] += duration
			current = child

	_finalize_node(root)
	return {
		"root": root,
		"total_weight": total_weight,
		"max_depth": max_depth,
	}


static func _is_frame_marker_name(name: String) -> bool:
	return name.contains("._Process(") \
		or name.contains("._PhysicsProcess(") \
		or name.contains(".ProcessFrame(") \
		or name.contains(".PhysicsProcessFrame(")


static func _extract_frame_marker_from_stack(stack_entries: Array) -> String:
	for entry in stack_entries:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var name := str(entry.get("name", ""))
		if _is_frame_marker_name(name):
			return name
	return ""


static func _summarize_marker_name(name: String) -> String:
	if name.contains("._PhysicsProcess(") or name.contains(".PhysicsProcessFrame("):
		return "_PhysicsProcess"
	if name.contains("._Process(") or name.contains(".ProcessFrame("):
		return "_Process"
	return name


static func _finalize_node(node: Dictionary) -> void:
	var children: Array = []
	var child_lookup: Dictionary = node.get("child_lookup", {})
	for child in child_lookup.values():
		_finalize_node(child)
		children.append(child)
	children.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		var weight_diff := float(a.get("cumulative_weight", 0.0)) - float(b.get("cumulative_weight", 0.0))
		if absf(weight_diff) < 0.0001:
			return str(a.get("name", "")) < str(b.get("name", ""))
		return float(a.get("cumulative_weight", 0.0)) > float(b.get("cumulative_weight", 0.0))
	)
	node["children"] = children
	node["child_count"] = children.size()
	node.erase("child_lookup")


static func _resolve_frame_name(frame_names: Array, frame_index: int) -> String:
	if frame_index >= 0 and frame_index < frame_names.size():
		return str(frame_names[frame_index])
	return "[invalid frame %d]" % frame_index
