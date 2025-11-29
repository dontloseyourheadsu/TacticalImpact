extends RigidBody3D

var flying_force = Vector3.ZERO
var target_altitude = 10.0
var altitude_tolerance = 0.1
var ascend_force = 15.0
var descend_force = 0.1

func _physics_process(delta):
	var current_altitude = global_transform.origin.y
	if current_altitude < target_altitude - altitude_tolerance:
		flying_force.y = ascend_force
	elif current_altitude > target_altitude + altitude_tolerance:
		flying_force.y = -descend_force
	else:
		flying_force.y = 0.0

	apply_central_force(flying_force)

func set_target_altitude(altitude):
	target_altitude = altitude
