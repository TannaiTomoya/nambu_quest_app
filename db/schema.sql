-- D1: 計測イベント（schema_version 2）
-- 冪等性の要は event_id の UNIQUE。再送の2件目以降は INSERT OR IGNORE で無視する。

CREATE TABLE IF NOT EXISTS events (
  id             INTEGER PRIMARY KEY AUTOINCREMENT,
  event_id       TEXT NOT NULL UNIQUE,
  event          TEXT NOT NULL,
  schema_version TEXT NOT NULL DEFAULT '2',
  game_version   TEXT,
  config_version TEXT,
  environment    TEXT,
  source         TEXT,
  session_id     TEXT,
  destination_id TEXT,
  client_ts      TEXT,
  received_at    TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_events_event_session ON events (event, session_id);
CREATE INDEX IF NOT EXISTS idx_events_environment ON events (environment);
