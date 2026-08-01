import { insertEvent, loadDestinations } from "../_lib/shared.js";

// GET /go/:destinationId?sid= — 許可リスト確認 → クリック記録 → 302

export async function onRequestGet(context) {
  const { request, env, params } = context;
  const destinationId = params.destinationId;

  if (!destinationId || destinationId === "_comment") {
    return new Response("Not Found", { status: 404 });
  }

  let destinations;
  try {
    destinations = await loadDestinations(context);
  } catch (e) {
    return new Response("Config unavailable", { status: 500 });
  }

  const targetUrl = destinations[destinationId];
  if (!targetUrl || typeof targetUrl !== "string" || !/^https:\/\//.test(targetUrl)) {
    // 任意URLは受け付けない。許可リスト外は404
    return new Response("Not Found", { status: 404 });
  }

  const url = new URL(request.url);
  const sid = url.searchParams.get("sid") || null;

  if (env.DB) {
    try {
      await insertEvent(env.DB, {
        event_id: crypto.randomUUID(),
        event: "official_link_clicked",
        schema_version: "2",
        environment: request.headers.get("host") && request.headers.get("host").endsWith(".pages.dev")
          ? "staging"
          : "production",
        session_id: sid,
        destination_id: destinationId,
        client_ts: new Date().toISOString(),
      });
    } catch (e) {
      // 計測失敗でもリダイレクトは続ける
    }
  }

  return Response.redirect(targetUrl, 302);
}
