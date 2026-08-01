// Pages Functions 共通処理
// destinationId → URL の正は prototype/config/game-config.json の DESTINATIONS

export const ACCEPTED_EVENTS = new Set([
  "game_started",
  "game_completed",
  "replay_started",
  "official_link_fallback_opened",
]);

export function jsonResponse(body, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      "Content-Type": "application/json; charset=utf-8",
      "Cache-Control": "no-store",
    },
  });
}

export async function loadDestinations(context) {
  const url = new URL("/config/game-config.json", context.request.url);
  const res = await context.env.ASSETS.fetch(url);
  if (!res.ok) {
    throw new Error("DESTINATIONS fetch failed: " + res.status);
  }
  const data = await res.json();
  const dest = Object.assign({}, data.DESTINATIONS || {});
  delete dest._comment;
  return dest;
}

export async function insertEvent(db, row) {
  // INSERT OR IGNORE: 同じ event_id の再送は保存せず成功扱い
  return db
    .prepare(
      `INSERT OR IGNORE INTO events (
        event_id, event, schema_version, game_version, config_version,
        environment, source, session_id, destination_id, client_ts
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
    .bind(
      row.event_id,
      row.event,
      row.schema_version || "2",
      row.game_version || null,
      row.config_version || null,
      row.environment || null,
      row.source || null,
      row.session_id || null,
      row.destination_id || null,
      row.client_ts || null
    )
    .run();
}
