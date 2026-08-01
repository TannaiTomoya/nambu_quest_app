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

設計上の約束：

- session_id は1プレイごとの `crypto.randomUUID()`。個人情報は送らない
- 送信失敗でもゲームは止まらない。失敗イベントは sessionStorage に退避しオンライン復帰時に再送
- Workerに到達できない場合、外部リンクは直接URLへフォールバック（リンクは必ず開ける）
- 流入元の計測はゲームURLの `?src=` で行う（例: QRコードに `?src=event` を付与）

主KPI：公式情報クリック率 = `official_link_clicked` のユニークsession数 ÷ `game_completed` のユニークsession数

送信先は `prototype/js/analytics.js` 冒頭の定数で管理しています（開発中: `http://127.0.0.1:8787`、本番: Cloudflare Pages Functions の同一オリジンを想定）。**Worker本体は未デプロイ**で、契約仕様のみ確定しています。

## 公開の想定

- ホスティング：Cloudflare Pages（静的配信 + Pages Functions で計測を同居）
- 保存先：Cloudflare D1（SQLiteベース。KPIをSQLで算出）
- 独自ドメインはQRコード印刷前までに確定
- 会場利用はスマートフォンのモバイル通信を前提とする

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
