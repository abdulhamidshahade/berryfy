ALTER TABLE order_items
ADD COLUMN order_id BIGINT REFERENCES orders(id);