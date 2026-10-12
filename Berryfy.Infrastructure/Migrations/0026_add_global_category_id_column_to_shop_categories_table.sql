Alter table shop_categories
add column global_category_id int references global_categories(id);