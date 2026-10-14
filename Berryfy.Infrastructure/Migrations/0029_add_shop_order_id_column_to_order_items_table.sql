alter table order_items
add column shop_order_id bigint not null references shop_orders(id) on delete restrict