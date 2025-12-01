extends RigidBody3D

@export var target_position: Vector3 = Vector3.ZERO

# Orbit variables
var orbit_center: Vector3 = Vector3.ZERO
var orbit_radius: float = 0.0
var orbit_speed: float = 0.0
var orbit_angle: float = 0.0
var is_orbiting: bool = false

func _ready():
	target_position = global_position

func setup_orbit(center: Vector3, radius: float, speed: float, start_angle: float):
	orbit_center = center
	orbit_radius = radius
	orbit_speed = speed
	orbit_angle = start_angle
	is_orbiting = true
	
	# Set initial position
	var offset = Vector3(cos(orbit_angle) * orbit_radius, 0, sin(orbit_angle) * orbit_radius)
	target_position = orbit_center + offset
	global_position = target_position

func _physics_process(delta):
	if is_orbiting:
		orbit_angle += orbit_speed * delta
		var offset = Vector3(cos(orbit_angle) * orbit_radius, 0, sin(orbit_angle) * orbit_radius)
		target_position = orbit_center + offset

	var error = target_position - global_position
	
	var force = Vector3.ZERO
	
	# Hover/Anti-gravity (Counteract gravity)
	var gravity = ProjectSettings.get_setting("physics/3d/default_gravity")
	force.y += mass * gravity
	
	# Movement (PD-controller)
	var desired_velocity = error * 2.0
	var velocity_diff = desired_velocity - linear_velocity
	
	force += velocity_diff * 5.0
	
	apply_central_force(force)

func set_target_position(pos: Vector3):
	target_position = pos
	is_orbiting = false
