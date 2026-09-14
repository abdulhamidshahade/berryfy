-- 0001_CreateCategory.sql
-- Creates the categories table (backs berryfy.domain.entities.productEntities.category).

CREATE TABLE categories
(
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL,
    description TEXT NOT NULL,
    image_url   TEXT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);