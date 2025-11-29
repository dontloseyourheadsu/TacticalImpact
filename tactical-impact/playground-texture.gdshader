shader_type spatial;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 size = vec2(10.0, 10.0);
	vec2 pos = floor(UV * size);
	float pattern = mod(pos.x + pos.y, 2.0);
	
	vec3 black = vec3(0.05, 0.05, 0.05);
	vec3 blue = vec3(0.0, 0.2, 0.8);
	
	ALBEDO = mix(black, blue, pattern);
	ROUGHNESS = 0.5;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
