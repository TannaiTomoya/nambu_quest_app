"use strict";

// ===== 設定読み込み =====
// config/game-config.json を fetch し、ゲーム全体で共有するグローバル定数を構築する。
// ゲーム数値・文言・景品・地点の変更は JSON 側で行い、JSは触らない。
// 注意: file:// 直開きでは fetch が失敗するため、ローカルサーバー経由で開くこと。

let CONFIG = null;       // 調整パラメータ（制限時間・閾値など）
let TICKETS = null;      // 招待券・クーポンの定義（実URLは持たず destinationId のみ）
let POINTS_DATA = null;  // 潜水地点の定義
let ITEMS = null;        // 通常アイテム
let DESTINATIONS = null; // destinationId → 直接URL（Worker未達時のフォールバック用）
let STRINGS = null;      // 表示文言（文化監修前の暫定を含む）
let GAME_CONFIG_META = { config_version: "unknown", features: {} };

async function loadGameConfig() {
  const res = await fetch("config/game-config.json", { cache: "no-store" });
  if (!res.ok) {
    throw new Error("設定ファイルの読み込みに失敗: HTTP " + res.status);
  }
  const data = await res.json();
  CONFIG = data.CONFIG;
  TICKETS = data.TICKETS;
  POINTS_DATA = data.POINTS_DATA;
  ITEMS = data.ITEMS;
  DESTINATIONS = data.DESTINATIONS;
  STRINGS = data.STRINGS;
  GAME_CONFIG_META = {
    config_version: data.config_version || "unknown",
    features: data.features || {},
  };
  return data;
}

// destinationId に対応する直接URLを返す（フォールバック用）
function directDestinationUrl(destinationId) {
  return (DESTINATIONS && DESTINATIONS[destinationId]) || "#";
}
