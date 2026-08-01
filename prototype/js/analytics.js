"use strict";

// ===== イベント計測クライアント =====
// 送信イベント: game_started / game_completed / replay_started
// （official_link_clicked は Worker が GET /go/:destinationId で記録する）
//
// 設計上の約束:
// - 送信失敗でもゲームを止めない（例外はすべて握りつぶす）
// - 個人情報を送らない。localStorage に永続的な個人識別IDを保存しない
// - session_id は1プレイ（=1ページ読込）ごとの crypto.randomUUID()
// - 再プレイ判定は sessionStorage のプレイ回数カウンタ（タブ限定・非永続）
// - 送信失敗イベントは sessionStorage に退避し、オンライン復帰時に再送する
// - source はゲームURLの ?src= から取得（QR別の流入計測用。既定は "direct"）

const ANALYTICS = (() => {
  // ===== 送信先はここ1か所で管理 =====
  // 開発: ローカルWorker（wrangler dev 等）
  // 本番: Cloudflare Pages Functions（同一オリジン）なら "/events" と "/go/" に変更する
  const EVENTS_ENDPOINT = "http://127.0.0.1:8787/events";
  const GO_BASE = "http://127.0.0.1:8787/go/";

  const SCHEMA_VERSION = "1";
  const GAME_VERSION = "1.0.0";
  const QUEUE_KEY = "nambu_event_queue";
  const PLAY_COUNT_KEY = "nambu_play_count";

  let enabled = true;
  let trackedLinksEnabled = true;
  let configVersion = "unknown";
  // 1回でもイベント送信に成功したら true。外部リンクを /go/ 経由にするかの判断に使う
  let workerReachable = false;
  const sentOnce = new Set();

  const sessionId = (typeof crypto !== "undefined" && crypto.randomUUID)
    ? crypto.randomUUID()
    : "s-" + Date.now() + "-" + Math.floor(Math.random() * 1e9);

  const source = (() => {
    try {
      const raw = new URLSearchParams(location.search).get("src") || "";
      return /^[a-z0-9_-]{1,32}$/.test(raw) ? raw : "direct";
    } catch (e) {
      return "direct";
    }
  })();

  function baseEvent(name) {
    return {
      event: name,
      schema_version: SCHEMA_VERSION,
      game_version: GAME_VERSION,
      config_version: configVersion,
      source: source,
      session_id: sessionId,
      client_ts: new Date().toISOString(),
    };
  }

  function readQueue() {
    try { return JSON.parse(sessionStorage.getItem(QUEUE_KEY)) || []; } catch (e) { return []; }
  }

  function writeQueue(queue) {
    try { sessionStorage.setItem(QUEUE_KEY, JSON.stringify(queue.slice(-20))); } catch (e) { /* 容量超過等は無視 */ }
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

  async function flushQueue() {
    const queue = readQueue();
    if (!queue.length) return;
    writeQueue([]);
    const remain = [];
    for (const payload of queue) {
      if (!(await postEvent(payload))) remain.push(payload);
    }
    if (remain.length) writeQueue(remain);
  }

  async function send(name) {
    if (!enabled || sentOnce.has(name)) return;
    sentOnce.add(name);
    const payload = baseEvent(name);
    if (await postEvent(payload)) {
      flushQueue();
    } else {
      writeQueue(readQueue().concat([payload]));
    }
  }

  function playCount() {
    try { return Number(sessionStorage.getItem(PLAY_COUNT_KEY)) || 0; } catch (e) { return 0; }
  }

  return {
    init(meta) {
      configVersion = (meta && meta.config_version) || "unknown";
      enabled = !(meta && meta.features && meta.features.analytics === false);
      trackedLinksEnabled = !(meta && meta.features && meta.features.trackedLinks === false);
      try { window.addEventListener("online", flushQueue); } catch (e) { /* noop */ }
      if (enabled) flushQueue();
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
