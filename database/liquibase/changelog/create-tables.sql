CREATE TABLE "messages"
(
    "id"           SERIAL PRIMARY KEY,
    "content"      text NOT NULL,
    "creator_name" text NOT NULL,
    "created_at"   timestamp DEFAULT CURRENT_TIMESTAMP
);
