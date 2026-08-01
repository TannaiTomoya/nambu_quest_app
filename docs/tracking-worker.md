# 計測Worker 契約仕様

HTML版本体（`prototype/`）が送信するイベントの受け口と、外部リンクのクリック計測を担うWorkerの契約です。
実体は Cloudflare Pages Functions（+ D1）で実装する想定です（未デプロイ）。

## 構成方針

- 静的ホスティング：Cloudflare Pages（ゲーム本体）
- 計測：同一プロジェクトの Pages Functions で `/events` と `/go/` を提供
- 同一オリジンにすることで CORS 設定が不要になり、送信の確実性が上がる
- 保存先：Cloudflare D1（SQLite互換。KPIをSQLで算出するため）
- 独自ドメインは QRコード印刷前までに確定する（開発中は `*.pages.dev` でよい）

## エンドポイント

### POST /events

クライアント（`prototype/js/analytics.js`）からのイベントを受信し、D1へ保存する。

受信可能イベント：

- `game_started` — 装備準備の開始
- `game_completed` — 結果画面の表示
- `replay_started` — 2プレイ目以降の開始

リクエストボディ（JSON）：

```json
{
  "event": "game_started",
  "schema_version": "1",
  "game_version": "1.0.0",
  "config_version": "1.0.0",
  "source": "direct",
  "session_id": "<crypto.randomUUID()>",
  "client_ts": "2026-08-01T04:20:31.640Z"
}
```

処理：

1. `event` が受信可能イベントか確認（それ以外は破棄して 400）
2. `events` テーブルへ INSERT
3. 200 を返す

注意：

- 別オリジンから受ける場合のみ CORS ヘッダ（`Access-Control-Allow-Origin` と OPTIONS プリフライト応答）が必要。同一オリジン構成なら不要
- 個人情報は受け取らない（IPの永続保存もしない）
- 同一 `session_id` + `event` の重複はあり得る（再送との競合）。KPI集計は「ユニークsession数」で行うため実害はない

### GET /go/:destinationId?sid=&lt;session_id&gt;

外部公式サイトへの計測付きリダイレクト。

処理：

1. `destinationId` を許可リストで確認（リスト外は 404）
2. `official_link_clicked` を `sid` 付きで `events` テーブルへ記録
3. 許可済みURLへ 302 リダイレクト

許可リスト（正はWorker側で管理。HTML側 `game-config.json` の `DESTINATIONS` はWorker未達時のフォールバック用）：

| destinationId | リダイレクト先 |
|---|---|
| `sparta` | https://hirono.spartacamp.jp/ |
| `taiken` | https://hirono-dmo.net/2026/06/02/...（種市高校 南部もぐり潜水体験講座） |
| `coupon` | https://portal.town.hirono.iwate.jp/coupon/ |

- 任意のURLをクエリ文字列で受け取らない（オープンリダイレクト防止）
- `sid` が欠けていても記録してリダイレクトする（リンクを開くことを優先）

## D1 スキーマ

```sql
CREATE TABLE IF NOT EXISTS events (
  id             INTEGER PRIMARY KEY AUTOINCREMENT,
  event          TEXT NOT NULL,
  schema_version TEXT NOT NULL DEFAULT '1',
  game_version   TEXT,
  config_version TEXT,
  source         TEXT,
  session_id     TEXT,
  destination_id TEXT,          -- official_link_clicked のみ
  client_ts      TEXT,          -- クライアント時刻（参考値）
  received_at    TEXT NOT NULL DEFAULT (datetime('now'))
);
CREATE INDEX IF NOT EXISTS idx_events_event_session ON events (event, session_id);
```

## 主KPI

公式情報クリック率 = `official_link_clicked` のユニークsession数 ÷ `game_completed` のユニークsession数

```sql
SELECT
  CAST(
    (SELECT COUNT(DISTINCT session_id) FROM events WHERE event = 'official_link_clicked')
    AS REAL
  ) /
  (SELECT COUNT(DISTINCT session_id) FROM events WHERE event = 'game_completed')
  AS official_link_ctr;
```

閲覧方法：当面は Cloudflare ダッシュボードの D1 コンソール、または `wrangler d1 execute` で上記SQLを実行する。

## クライアント側の挙動（実装済み）

- 送信先は `prototype/js/analytics.js` 冒頭の定数2つ（`EVENTS_ENDPOINT` / `GO_BASE`）で管理
  - 開発中：`http://127.0.0.1:8787/...`
  - 本番（同一オリジン）：`/events` と `/go/` に変更する
- 送信失敗でもゲームは止まらない。失敗イベントは sessionStorage に退避し、オンライン復帰時に再送
- Workerへ1度も到達できていない場合、外部リンクは直接URLへフォールバック（リンクは必ず開ける）
- 再プレイ判定は sessionStorage のプレイ回数カウンタ（タブ限定・非永続・個人識別なし）

## 未解決事項

- Cloudflare プロジェクトの作成・デプロイ（アカウント・ドメイン確定待ち）
- 実物特典を導入する場合の正式DB移行（発行台帳・利用確認・管理者認証が必要になった時点で再検討）
