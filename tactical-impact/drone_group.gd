extends Node3D

@export var move_distance: float = 5.0
@export var move_speed: float = 1.0
@export var float_amplitude: float = 0.5
@export var float_speed: float = 2.0

var time_passed: float = 0.0
var start_positions: Dictionary = {}
var drones: Array = []

func _ready():
	# Wait a frame to ensure children are ready and positioned
	await get_tree().process_frame
	
	for child in get_children():
		# Check if the child is a drone (has the set_target_position method)
		if child.has_method("set_target_position"):
			drones.append(child)
			start_positions[child] = child.global_position

func _process(delta):
	time_passed += delta
	
	# Calculate offset based on sine wave for back and forth movement along Z axis
	var z_offset = sin(time_passed * move_speed) * move_distance
	var y_offset = sin(time_passed * float_speed) * float_amplitude
	var movement_vector = Vector3(0, y_offset, z_offset)
	
	for drone in drones:
		if drone in start_positions:
			var base_pos = start_positions[drone]
			var new_target = base_pos + movement_vector
			drone.set_target_position(new_target)
