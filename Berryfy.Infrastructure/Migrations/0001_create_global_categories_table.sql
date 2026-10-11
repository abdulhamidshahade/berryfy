-- 0001_CreateCategory.sql
-- Creates the categories table (backs berryfy.domain.entities.productEntities.category).

CREATE TABLE global_categories
(
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL,
    description TEXT NOT NULL,
    image_url   TEXT NULL,
    slug VARCHAR(180) NOT NULL UNIQUE,
    parent_category_id BIGINT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    display_order INT NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_global_categories_parent
        FOREIGN KEY (parent_category_id)
        REFERENCES global_categories(id)
        ON DELETE RESTRICT
);