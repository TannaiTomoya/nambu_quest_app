# 計測Worker 契約仕様（schema_version: 2）

HTML版本体（`prototype/`）が送信するイベントの受け口と、外部リンクのクリック計測を担うWorkerの契約です。
実体は Cloudflare Pages Functions（+ D1）で実装する想定です（未デプロイ）。

## 構成方針

- 静的ホスティング：Cloudflare Pages（ゲーム本体）
- 計測：同一プロジェクトの Pages Functions で `/events` と `/go/` を提供
- 同一オリジンにすることで CORS 設定が不要になり、送信の確実性が上がる
- 保存先：Cloudflare D1（SQLite互換。KPIをSQLで算出するため）
- ステージングは Pages のプレビュー環境（`*.pages.dev`）を使い、D1バインディングを本番と別DBにする
- 独自ドメインは QRコード印刷前までに確定する

### リンク先定義の単一ソース化

destinationId → 実URL の対応表は **`prototype/config/game-config.json` の `DESTINATIONS` を唯一の定義**とし、
Pages Functions もこの JSON を読んで許可リストとして使う（ビルド時 import または自オリジンの静的アセット fetch）。
クライアントとWorkerでリンク先がずれる事故を構造的に防ぐ。

## 冪等性（重複保存の防止）

クライアントは送信をoutbox方式（先にキューへ保存→成功したら削除）で行うため、
「Worker側は保存成功したが応答がクライアントへ届かなかった」場合に**同じイベントが再送される**。

- 各イベントはクライアントが生成時に1回だけ発行する `event_id`（UUID）を持つ。再送しても同じ値
- Worker は `INSERT OR IGNORE` で保存し、同じ `event_id` の2件目以降を無視する
- これにより再送・pagehide時の多重フラッシュがあっても保存は1件のまま

## エンドポイント

### POST /events

クライアント（`prototype/js/analytics.js`）からのイベントを受信し、D1へ保存する。

受信可能イベント：

- `game_started` — 装備準備の開始
- `game_completed` — 結果画面の表示
- `replay_started` — 2プレイ目以降の開始
- `official_link_fallback_opened` — Worker未達時にフォールバックの直接リンクを開いた（`destination_id` 付き。復旧後に再送で届く）

リクエストボディ（JSON）：

```json
{
  "event": "game_started",
  "event_id": "<イベント生成時のUUID。再送でも不変>",
  "schema_version": "2",
  "game_version": "1.0.0",
  "config_version": "1.0.0",
  "environment": "production",
  "source": "direct",
  "session_id": "<crypto.randomUUID()>",
  "client_ts": "2026-08-01T04:20:31.640Z"
}
```

`environment` はクライアントがホスト名から自動判別する（`127.0.0.1`/`localhost` → `dev`、`*.pages.dev` → `staging`、それ以外 → `production`）。
テスト・ステージング操作が本番KPIへ混ざるのを防ぐため、集計時は `environment = 'production'` で絞る。

処理：

1. `event` が受信可能イベントか確認（それ以外は破棄して 400）
2. `events` テーブルへ `INSERT OR IGNORE`（`event_id` 重複は無視）
3. 200 を返す

注意：

- 別オリジンから受ける場合のみ CORS ヘッダ（`Access-Control-Allow-Origin` と OPTIONS プリフライト応答）が必要。同一オリジン構成なら不要
- 個人情報は受け取らない（IP・User-Agent の永続保存もしない）

### GET /go/:destinationId?sid=&lt;session_id&gt;

外部公式サイトへの計測付きリダイレクト。

処理：

1. `destinationId` を許可リスト（`game-config.json` の `DESTINATIONS`）で確認（リスト外は 404）
2. `official_link_clicked` を `sid` 付きで `events` テーブルへ記録（`event_id` はWorkerが発行）
3. 許可済みURLへ 302 リダイレクト

現在の destinationId：`sparta` / `taiken` / `coupon`（実URLは `game-config.json` 参照）

- 任意のURLをクエリ文字列で受け取らない（オープンリダイレクト防止）
- `sid` が欠けていても記録してリダイレクトする（リンクを開くことを優先）

## D1 スキーマ

```sql
CREATE TABLE IF NOT EXISTS events (
  id             INTEGER PRIMARY KEY AUTOINCREMENT,
  event_id       TEXT NOT NULL UNIQUE,  -- 冪等性の要。再送の2件目以降を弾く
  event          TEXT NOT NULL,
  schema_version TEXT NOT NULL DEFAULT '2',
  game_version   TEXT,
  config_version TEXT,
  environment    TEXT,                  -- dev / staging / production
  source         TEXT,
  session_id     TEXT,
  destination_id TEXT,                  -- official_link_clicked / official_link_fallback_opened のみ
  client_ts      TEXT,                  -- クライアント時刻（参考値）
  received_at    TEXT NOT NULL DEFAULT (datetime('now'))
);
CREATE INDEX IF NOT EXISTS idx_events_event_session ON events (event, session_id);
```

保存は `INSERT OR IGNORE INTO events (...) VALUES (...)` で行う。

## 主KPI

公式情報クリック率 =（`official_link_clicked` または `official_link_fallback_opened` のユニークsession数）÷ `game_completed` のユニークsession数

フォールバック経由のクリックも分子に含めることで、Worker障害時にクリック率が実際より低く出るのを防ぐ。

```sql
SELECT
  CAST(
    (SELECT COUNT(DISTINCT session_id) FROM events
      WHERE event IN ('official_link_clicked', 'official_link_fallback_opened')
        AND environment = 'production')
    AS REAL
  ) /
  (SELECT COUNT(DISTINCT session_id) FROM events
    WHERE event = 'game_completed'
      AND environment = 'production')
  AS official_link_ctr;
```

集計はすべて「ユニーク `session_id` 数」基準のため、万一の重複保存にも影響されない。
閲覧方法：当面は Cloudflare ダッシュボードの D1 コンソール、または `wrangler d1 execute` で上記SQLを実行する。

## クライアント側の挙動（実装済み）

- 送信先は `prototype/js/analytics.js` 冒頭の定数2つ（`EVENTS_ENDPOINT` / `GO_BASE`）で管理
  - 開発中：`http://127.0.0.1:8787/...`
  - 本番（同一オリジン）：`/events` と `/go/` に変更する
- outbox方式：イベントは先に sessionStorage のキューへ保存し、送信成功後に削除。
  オンライン復帰・pagehide・タブ非表示のタイミングで再送する
- 送信失敗でもゲームは止まらない
- Workerへ1度も到達できていない場合、外部リンクは直接URLへフォールバックし、
  クリック時に `official_link_fallback_opened` をキューへ記録（復旧後に再送）
- 再プレイ判定は sessionStorage のプレイ回数カウンタ（タブ限定・非永続・個人識別なし）
- `?src=` で流入元を記録（`[a-z0-9_-]{1,32}` 以外は `direct`）。正式な命名は `場所_年` 形式（例 `reception_2026`）

## source 台帳

| source | 用途 |
|---|---|
| `direct` | src指定なし（既定） |
| （QR配布時に `reception_2026` 等を追記） | |

## 未解決事項

- Cloudflare プロジェクトの作成・デプロイ（アカウント・ドメイン確定待ち）
- イベントデータの保存期間（実証期間+1年を目安に別途確定）
- 実物特典を導入する場合の正式DB移行（発行台帳・利用確認・管理者認証が必要になった時点で再検討）
