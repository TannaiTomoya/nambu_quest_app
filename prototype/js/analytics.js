"use strict";

// ===== イベント計測クライアント =====
// 送信イベント:
//   game_started / game_completed / replay_started
//   official_link_fallback_opened（Worker未達でフォールバックの直接リンクを開いたとき）
// official_link_clicked は Worker が GET /go/:destinationId で記録する。
//
// 設計上の約束:
// - 送信失敗でもゲームを止めない（例外はすべて握りつぶす）
// - 個人情報を送らない。localStorage に永続的な個人識別IDを保存しない
// - session_id は1プレイ（=1ページ読込）ごとの crypto.randomUUID()
// - event_id はイベント生成時に1回だけ発行するUUID。再送しても同じ値のため、
//   Worker側（D1のUNIQUE制約）で重複保存を防げる
// - 送信はoutbox方式: 先に sessionStorage のキューへ書き、成功したら消す。
//   送信中にタブを閉じてもイベントが消えない
// - pagehide / タブ非表示 / オンライン復帰でキューを再送する
// - environment は hostname から自動判別（dev / staging / production）。
//   ステージング操作が本番KPIへ混ざるのを防ぐ
// - source はゲームURLの ?src= から取得（QR別の流入計測用。既定は "direct"）

const ANALYTICS = (() => {
  // ===== 送信先はここ1か所で管理 =====
  // 開発: ローカルWorker（wrangler dev 等）
  // 本番: Cloudflare Pages Functions（同一オリジン）なら "/events" と "/go/" に変更する
  const EVENTS_ENDPOINT = "http://127.0.0.1:8787/events";
  const GO_BASE = "http://127.0.0.1:8787/go/";

  const SCHEMA_VERSION = "2";
  const GAME_VERSION = "1.0.0";
  const QUEUE_KEY = "nambu_event_queue";
  const PLAY_COUNT_KEY = "nambu_play_count";

  let enabled = true;
  let trackedLinksEnabled = true;
  let configVersion = "unknown";
  // 1回でもイベント送信に成功したら true。外部リンクを /go/ 経由にするかの判断に使う
  let workerReachable = false;
  let flushing = false;
  const sentOnce = new Set();

  function uuid() {
    return (typeof crypto !== "undefined" && crypto.randomUUID)
      ? crypto.randomUUID()
      : "u-" + Date.now() + "-" + Math.floor(Math.random() * 1e9);
  }

  const sessionId = uuid();

  // ステージング/本番のデータ混在を防ぐため、環境はホスト名から機械的に決める
  const ENVIRONMENT = (() => {
    try {
      const host = location.hostname;
      if (host === "localhost" || host === "127.0.0.1") return "dev";
      if (host.endsWith(".pages.dev")) return "staging";
      return "production";
    } catch (e) {
      return "unknown";
    }
  })();

  const source = (() => {
    try {
      const raw = new URLSearchParams(location.search).get("src") || "";
      return /^[a-z0-9_-]{1,32}$/.test(raw) ? raw : "direct";
    } catch (e) {
      return "direct";
    }
  })();

  function baseEvent(name, extra) {
    const payload = {
      event: name,
      event_id: uuid(),
      schema_version: SCHEMA_VERSION,
      game_version: GAME_VERSION,
      config_version: configVersion,
      environment: ENVIRONMENT,
      source: source,
      session_id: sessionId,
      client_ts: new Date().toISOString(),
    };
    return Object.assign(payload, extra || {});
  }

  function readQueue() {
    try { return JSON.parse(sessionStorage.getItem(QUEUE_KEY)) || []; } catch (e) { return []; }
  }

  function writeQueue(queue) {
    try { sessionStorage.setItem(QUEUE_KEY, JSON.stringify(queue.slice(-20))); } catch (e) { /* 容量超過等は無視 */ }
  }

  function enqueue(payload) {
    writeQueue(readQueue().concat([payload]));
  }

  function removeFromQueue(eventId) {
    writeQueue(readQueue().filter(p => p.event_id !== eventId));
  }

  // keepalive付きfetchで送信する。ページ遷移・タブを閉じる直前でも送信が継続される
  async function postEvent(payload) {
    try {
      const res = await fetch(EVENTS_ENDPOINT, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
        keepalive: true,
      });
      if (res.ok) {
        workerReachable = true;
        return true;
      }
    } catch (e) {
      // 計測失敗でゲームを止めない
    }
    return false;
  }

  // 送信に成功したらキューから消す。失敗ならキューに残ったまま（後で再送）
  async function deliver(payload) {
    if (await postEvent(payload)) {
      removeFromQueue(payload.event_id);
      return true;
    }
    return false;
  }

  async function flushQueue() {
    if (flushing) return;
    flushing = true;
    try {
      for (const payload of readQueue()) {
        await deliver(payload);
      }
    } finally {
      flushing = false;
    }
  }

  // outbox方式: 先にキューへ書いてから送信を試みる。
  // 万一送信中にページが消えても、次回読込時に再送される（event_idでWorker側が重複排除）
  async function send(name, extra) {
    if (!enabled || sentOnce.has(name)) return;
    sentOnce.add(name);
    const payload = baseEvent(name, extra);
    enqueue(payload);
    if (await deliver(payload)) flushQueue();
  }

  function playCount() {
    try { return Number(sessionStorage.getItem(PLAY_COUNT_KEY)) || 0; } catch (e) { return 0; }
  }

  // フォールバック（直接URL）で外部リンクを開いたクリックを後送信用に記録する。
  // /go/ 経由のクリックはWorkerが official_link_clicked として記録するため対象外
  function watchFallbackLinkClicks() {
    document.addEventListener("click", (e) => {
      try {
        const link = e.target && e.target.closest ? e.target.closest("a[data-destination-id]") : null;
        if (!link) return;
        const href = link.getAttribute("href") || "";
        if (href.indexOf(GO_BASE) === 0) return; // 計測URL経由なのでWorker側で記録される
        send("official_link_fallback_opened", { destination_id: link.dataset.destinationId });
      } catch (err) {
        // 計測失敗でリンクを止めない
      }
    }, true);
  }

  return {
    init(meta) {
      configVersion = (meta && meta.config_version) || "unknown";
      enabled = !(meta && meta.features && meta.features.analytics === false);
      trackedLinksEnabled = !(meta && meta.features && meta.features.trackedLinks === false);
      if (!enabled) return;
      try {
        window.addEventListener("online", flushQueue);
        window.addEventListener("pagehide", flushQueue);
        document.addEventListener("visibilitychange", () => {
          if (document.visibilityState === "hidden") flushQueue();
        });
      } catch (e) { /* noop */ }
      watchFallbackLinkClicks();
      flushQueue();
    },

    // ゲーム開始時に1回だけ呼ぶ。2プレイ目以降は replay_started も送る
    gameStarted() {
      if (sentOnce.has("game_started")) return;
      if (playCount() >= 1) send("replay_started");
      try { sessionStorage.setItem(PLAY_COUNT_KEY, String(playCount() + 1)); } catch (e) { /* noop */ }
      send("game_started");
    },

    gameCompleted() {
      send("game_completed");
    },

    // 外部リンクのhrefを組み立てる。
    // Workerへ到達できているときは /go/:destinationId?sid= 経由（Workerがクリックを記録して302）、
    // 到達できていないときは直接URLへフォールバックし、リンクは必ず開けるようにする
    ticketHref(destinationId) {
      if (enabled && trackedLinksEnabled && workerReachable) {
        return GO_BASE + encodeURIComponent(destinationId) + "?sid=" + encodeURIComponent(sessionId);
      }
      return directDestinationUrl(destinationId);
    },
  };
})();
