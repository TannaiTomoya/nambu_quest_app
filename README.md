# 南部もぐり観光RPG

岩手県洋野町に伝わる潜水技術「南部もぐり」を題材にした、短時間プレイのゲームアプリです。
南部もぐりを知らない人に疑似体験を通じて関心を持ってもらい、種市歴史民俗資料館などの公式観光情報へ誘導することを目的としています。

## 構成

**HTML版（`prototype/`）が唯一の本体です。** Unity版とFastAPI backendは旧設計として `main` から削除済みです（下記「旧実装」参照）。

| ディレクトリ | 内容 |
|---|---|
| `prototype/` | ゲーム本体（HTML / CSS / JS、外部ライブラリ非依存） |
| `docs/` | 設計変更の記録・計測Workerの契約仕様 |

```
prototype/
├── index.html              # 画面マークアップ
├── css/app.css             # スタイル
├── js/
│   ├── config-loader.js    # game-config.json の読み込み
│   ├── analytics.js        # イベント計測（送信先はこのファイル冒頭で管理）
│   ├── game-state.js       # 共有状態
│   ├── ui.js               # 画面描画
│   └── app.js              # 進行フローと初期化
├── config/game-config.json # ゲーム数値・地点・景品・表示文言・feature flags
└── assets/nanbu_return_scene.mp4  # 帰還演出の動画
```

## ゲームループ（1プレイ45〜60秒）

1. 装備準備タイムアタック（12秒。3工程を順番にタップ → 残り時間で最終安全確認を連打）
2. 準備ランクの決定（ゲージ50%でランク2、85%でランク3）
3. 潜水地点の選択（ランクが高いほど遠くへ潜れる）
4. 潜水・宝箱の獲得（地点ごとに招待券・クーポンが確定で出る）
5. 装備解除・帰還演出（動画。タップでスキップ可能）
6. 結果表示 →「もう一度潜る」で再プレイ

## 動かし方

設定JSONのfetchと動画再生のため、**`file://` 直開きは不可**です。必ずローカルサーバー経由で開いてください。

```bash
python3 -m http.server 8080
# PC:               http://127.0.0.1:8080/prototype/
# 同一ネットワークのスマートフォン: http://<PCのIPアドレス>:8080/prototype/
```

数値・文言・景品の調整は `prototype/config/game-config.json` を編集します。JSは触らなくてよい構成です。

## 計測

クライアントは次のイベントを送信します（詳細な契約は [`docs/tracking-worker.md`](docs/tracking-worker.md)）。

- `game_started` / `game_completed` / `replay_started` — `POST /events`
- `official_link_clicked` — 外部リンクを `GET /go/:destinationId?sid=` 経由にすることでWorker側が記録
- `official_link_fallback_opened` — Worker未達時にフォールバックの直接リンクを開いたクリック（復旧後に再送され、クリック率の取りこぼしを防ぐ）

設計上の約束：

- session_id は1プレイごとの `crypto.randomUUID()`。個人情報は送らない
- **冪等性**：全イベントに生成時1回だけ発行する `event_id`（UUID）を付与。再送しても同じ値のため、Worker側（D1の `event_id` UNIQUE制約 + `INSERT OR IGNORE`）で重複保存を防げる
- **outbox方式**：イベントは先に sessionStorage のキューへ保存し、送信成功後に削除。送信中にタブを閉じても消えず、オンライン復帰・pagehide・タブ非表示のタイミングで再送
- 送信失敗でもゲームは止まらない
- Workerに到達できない場合、外部リンクは直接URLへフォールバック（リンクは必ず開ける）
- **environment** をホスト名から自動判別して全イベントに付与（`127.0.0.1`→`dev`、`*.pages.dev`→`staging`、独自ドメイン→`production`）。ステージング操作が本番KPIへ混ざらない
- 流入元の計測はゲームURLの `?src=` で行う（例: QRコードに `?src=reception_2026` を付与）

主KPI：公式情報クリック率 =（`official_link_clicked` または `official_link_fallback_opened` のユニークsession数）÷ `game_completed` のユニークsession数（`environment='production'` で絞る）

destinationId → 実URL の対応表は `game-config.json` の `DESTINATIONS` が唯一の定義で、Worker側もこれを許可リストとして読む想定です（クライアントとWorkerのリンク先ずれを構造的に防止）。

送信先は `prototype/js/analytics.js` 冒頭の定数で管理しています（開発中: `http://127.0.0.1:8787`、本番: Cloudflare Pages Functions の同一オリジンを想定）。**Worker本体は未デプロイ**で、契約仕様のみ確定しています。

## 公開の想定

- ホスティング：Cloudflare Pages（静的配信 + Pages Functions で計測を同居）
- 保存先：Cloudflare D1（SQLiteベース。KPIをSQLで算出）
- 独自ドメインはQRコード印刷前までに確定
- 会場利用はスマートフォンのモバイル通信を前提とする

## Cloudflare ステージング（Pages + D1）

リポジトリ構成：

| パス | 役割 |
|---|---|
| `prototype/` | Pages の静的配信ルート（`pages_build_output_dir`） |
| `functions/` | Pages Functions（`POST /events`・`GET /go/:destinationId`） |
| `db/schema.sql` | D1 テーブル定義（`event_id` UNIQUE） |
| `wrangler.toml` | プロジェクト名・D1バインディング |

### 初回セットアップ

```bash
npm install
npx wrangler login

# D1 作成（出力の database_id を wrangler.toml に記入）
npx wrangler d1 create nambu-quest-events

# スキーマ適用（リモート）
npm run db:migrate:remote

# Pages プロジェクト作成（未作成時）
npx wrangler pages project create nambu-quest

# ステージング相当でデプロイ（*.pages.dev）
npx wrangler pages deploy prototype --project-name=nambu-quest
```

ダッシュボードで Pages プロジェクトに D1 バインディング `DB` → `nambu-quest-events` を紐づける（production / preview 両方）。

### ローカルでの一体起動

```bash
npm run db:migrate:local
npm run dev
# → http://127.0.0.1:8788/ （ポートは Wrangler の表示に従う）
```

計測の送信先は同一オリジンの `/events`・`/go/` です。`python3 -m http.server` だけでは Functions が動かないため、計測確認は `npm run dev` か Pages 上で行ってください。

### 冪等性の確認（同一 event_id × 3）

```bash
EID=$(uuidgen | tr '[:upper:]' '[:lower:]')
# ステージングURLに合わせてホストを差し替え
for i in 1 2 3; do
  curl -s -o /dev/null -w "%{http_code} " -X POST "https://<project>.pages.dev/events" \
    -H 'Content-Type: application/json' \
    -d "{\"event\":\"game_completed\",\"event_id\":\"$EID\",\"schema_version\":\"2\",\"environment\":\"staging\",\"session_id\":\"idempotency-test\",\"client_ts\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}"
done
echo
npx wrangler d1 execute nambu-quest-events --remote \
  --command "SELECT COUNT(*) AS n FROM events WHERE event_id='$EID';"
# 期待値: n = 1
```

## 旧実装

| 内容 | 場所 |
|---|---|
| Unity版（自由移動・旧設計） | ブランチ `archive/t2-free-movement` |
| Unity版（新ループ移植） | ブランチ `archive/unity-prep-loop` |
| FastAPI backend・旧docs | `main` の履歴（削除コミット `c552fb3` の直前）|

設計変更の経緯は [`docs/design-change-log.md`](docs/design-change-log.md) を参照してください。

## 注意事項

- 装備工程の名称・順番、装備解除を手伝う人数など、ゲーム内の文化的な記述は**暫定**です。種市歴史民俗資料館や種市高校への確認前に確定させないでください（文言は `game-config.json` に分離済みで、監修後の差し替えが容易です）
- 招待券・クーポンは演出とリンク誘導の検証用です。実物特典としての配布条件は未確定で、リンク先運営者への掲載連絡も未実施です
- 帰還演出の動画は生成AI（Kling）で作成した仮素材です
