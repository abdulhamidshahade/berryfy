CREATE TABLE user_roles
	(
		user_id INTEGER NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE, 
		role_id INTEGER NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE, 
		PRIMARY KEY (user_id,role_id)
	);