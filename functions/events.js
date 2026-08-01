import { ACCEPTED_EVENTS, insertEvent, jsonResponse } from "./_lib/shared.js";

// POST /events — クライアント計測イベントを D1 へ保存（event_id で冪等）

export async function onRequestOptions() {
  return new Response(null, {
    status: 204,
    headers: {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "POST, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type",
    },
  });
}

export async function onRequestPost(context) {
  const { request, env } = context;
  if (!env.DB) {
    return jsonResponse({ error: "DB binding missing" }, 500);
  }

  let body;
  try {
    body = await request.json();
  } catch (e) {
    return jsonResponse({ error: "invalid json" }, 400);
  }

  const event = body && body.event;
  if (!ACCEPTED_EVENTS.has(event)) {
    return jsonResponse({ error: "unsupported event" }, 400);
  }
  if (!body.event_id || typeof body.event_id !== "string") {
    return jsonResponse({ error: "event_id required" }, 400);
  }

  try {
    await insertEvent(env.DB, {
      event_id: body.event_id,
      event: event,
      schema_version: body.schema_version,
      game_version: body.game_version,
      config_version: body.config_version,
      environment: body.environment,
      source: body.source,
      session_id: body.session_id,
      destination_id: body.destination_id || null,
      client_ts: body.client_ts,
    });
  } catch (e) {
    return jsonResponse({ error: "db write failed" }, 500);
  }

  return jsonResponse({ ok: true }, 200);
}

export async function onRequestGet() {
  return jsonResponse({ error: "method not allowed" }, 405);
}
