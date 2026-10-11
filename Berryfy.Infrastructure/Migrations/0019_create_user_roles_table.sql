CREATE TABLE user_roles
	(
		user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE, 
		role_id INTEGER NOT NULL REFERENCES roles(id) ON DELETE CASCADE, 
		PRIMARY KEY (user_id,role_id)
	);