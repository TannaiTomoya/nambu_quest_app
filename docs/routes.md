南部もぐり観光RPG ルーティング設計

1. 前提

Unityはゲーム画面を担当する

FastAPIは推薦、ログ記録、外部リンク転送を担当する

HTMLテンプレートを多用せず、基本はJSON APIとリダイレクトで構成する

初期版ではログイン認証を実装しない

匿名セッションIDを各リクエストに含める

テンプレート名は、将来的にFastAPIでHTMLを返す場合の候補として定義する

Unity内部の画面はWeb URLではなくUnityシーン・パネルで管理する

2. FastAPIルート一覧

No.

用途

URL

HTTPメソッド

ログイン

レスポンス

テンプレート

1

ヘルスチェック

/

GET

不要

JSON

なし

2

API稼働確認

/health

GET

不要

JSON

なし

3

セッション開始

/sessions/start

POST

不要

JSON

なし

4

プレイ結果分析

/analyze

POST

不要

JSON

なし

5

外部リンク計測

/redirect/{destination_id}

GET

不要

Redirect

なし

6

掲載中コンテンツ取得

/contents/active

GET

不要

JSON

なし

7

掲載中イベント取得

/events/active

GET

不要

JSON

なし

8

行動イベント記録

/tracking/events

POST

不要

JSON

なし

9

簡易集計表示

/dashboard

GET

将来は必要

HTML

dashboard.html

10

APIドキュメント

/docs

GET

不要

HTML

FastAPI自動生成

3. 各ルートの詳細

3.1 GET /

目的

FastAPIが起動していることを確認する。

リクエスト

なし。

レスポンス例

{
  "message": "Nambu Quest API is running"
}

呼び出し元

ブラウザ

開発者

疎通確認

3.2 GET /health

目的

Unityまたは監視処理からAPIの稼働状態を確認する。

レスポンス例

{
  "status": "ok"
}

呼び出し元

Unity起動時

開発時の接続確認

備考

MVPでは省略可能。GET / と役割が重なるため、実装を減らす場合はどちらか一方にする。

3.3 POST /sessions/start

目的

匿名セッションを発行し、QR流入元を記録する。

リクエスト項目

source

language

client_version

レスポンス項目

session_id

started_at

呼び出し元

タイトル画面

ゲーム開始時

失敗時

Unity側でローカルの仮セッションIDを生成し、ゲームを続行する。

3.4 POST /analyze

目的

プレイ結果を受け取り、関心テーマ、デジタル報酬（称号・豆絞り柄）、観光案内を返す。

リクエスト項目

session_id

visited_points

completed_tasks

selected_records

remaining_air

returned_safely

play_time_seconds

判定テーマ

equipment

air_supply

underwater_work

safety（称号グループとして扱う場合）

処理方針

1. 入力値を検証する

2. ルールベースで関心テーマを判定する（乱数を使わない）

3. テーマに紐づく称号候補から、説明可能な条件で決定的に1件選ぶ（乱数を使わない）

4. テーマに対応する豆絞り柄の有効候補3〜5件から1件をランダムに選ぶ（乱数はここだけ）

5. 推薦文・報酬結果・資料館情報を返す（抽選結果で資料館導線を制限しない）

6. プレイ結果と報酬キーを保存する

称号決定に使う条件（決定的）：

主テーマ

持ち帰り記録の選択順

returned_safely

remaining_air

safety条件

同じ入力条件であれば、同じ称号を返す。

称号の処理経路では random や choice などの乱数処理を使用しない。

レスポンス項目

theme

headline

reason

reward

destination

official_detail_path

maps_path

active_events

reward の例：

title_key

title_text

pattern_key

pattern_name

pattern_image_path

reward_theme（必要な場合）

呼び出し元

帰還確認後

通信中画面

エラー

400：入力値不正

404：セッション不明

500：サーバー内部エラー

失敗時

Unity側で固定の共通報酬（共通称号・共通豆絞り柄）と固定の資料館案内を表示する。

結果画面と資料館CTAは必ず表示する。

3.5 GET /redirect/{destination_id}

目的

外部リンククリックを記録してから、公式ページへ転送する。

パスパラメータ

destination_id

クエリパラメータ

session_id

action

action の例

official_detail_clicked

map_opened

event_clicked

処理

セッションIDを確認

クリックイベントを保存

登録済みURLを取得

外部URLへ302リダイレクト

呼び出し元

結果・推薦画面

セキュリティ

転送先は事前登録したURLだけに限定する

任意URLをクエリで受け取らない

3.6 GET /contents/active

目的

現在掲載可能な資料館・観光情報を取得する。

レスポンス項目

destination_id

title

summary

official_url

maps_url

active

呼び出し元

Unity起動時

結果画面表示前

MVPでの扱い

POST /analyze のレスポンスに含める場合は、初期版では省略可能。

3.7 GET /events/active

目的

開催中の承認済みイベントを最大3件返す。

表示条件

approved = true

start_date <= today

today <= end_date

レスポンス項目

event_id

title

summary

start_date

end_date

official_url

呼び出し元

結果・推薦画面

MVPでの扱い

余裕がある場合のみ実装する。

3.8 POST /tracking/events

目的

ゲーム開始、推薦表示、保存などの行動イベントを記録する。

リクエスト項目

session_id

event_name

destination_id

occurred_at

event_name の例

game_started

game_completed

recommendation_displayed

saved_for_later

呼び出し元

タイトル画面

結果・推薦画面

MVPでの扱い

POST /analyze と /redirect で必要なイベントを記録できる場合は省略可能。

3.9 GET /dashboard

目的

検証結果を簡易表示する。

表示内容

ゲーム開始数

ゲーム完了数

公式詳細ページ遷移数

Google Maps起動数

流入元別の件数

完了率

遷移率

ログイン

初期版ではローカル利用のみ。外部公開する場合は認証必須。

テンプレート

dashboard.html

MVPでの扱い

余裕があれば追加する。必須ではない。

4. Unity内部ルーティング

Unity内部ではHTTP URLではなく、シーンとパネルで遷移する。

画面

シーン / パネル

遷移先

タイトル

TitleScene

TutorialPanel

操作説明

TutorialPanel

GameScene

探索

GameScene

各確認パネル

地点確認

LocationConfirmPanel

GameScene

記録選択

RecordSelectPanel

ReturnConfirmPanel

帰還確認

ReturnConfirmPanel

LoadingPanel

通信中

LoadingPanel

ResultScene または ErrorPanel

結果

ResultScene

外部ブラウザ、TitleScene

エラー

ErrorPanel

ResultScene

完了

CompletePanel

TitleScene

5. 画面とAPIの対応

Unity画面

FastAPIルート

タイミング

タイトル画面

POST /sessions/start

ゲーム開始時

探索画面

なし

Unity内で完結

地点確認

なし

Unity内で完結

記録選択

なし

Unity内で完結

帰還確認

POST /analyze

帰還確定後

結果画面

/redirect/{destination_id}

外部リンク押下時

結果画面

GET /events/active

補助イベント表示時

完了画面

POST /tracking/events

必要な場合のみ

6. MVPで実装する最小ルート

初学者向けの最小構成は以下とする。

GET /

POST /analyze

GET /redirect/{destination_id}

セッション開始、イベント一覧、ダッシュボードは後回しにできる。

最小フロー

Unityでゲーム開始
  ↓
Unity内で探索・空気管理・記録選択
  ↓
POST /analyze
  ↓
関心テーマ・決定的な称号・ランダムな豆絞り柄・資料館案内をUnityに表示
  ↓
GET /redirect/museum
  ↓
クリック記録
  ↓
公式詳細ページへ転送

7. URL命名規則

複数形を基本とする

動詞より名詞を優先する

ただし分析処理は /analyze を採用する

小文字とスラッシュを使用する

Unity側に公式URLを直接書かない

外部URLはFastAPI側で管理する

8. 注意事項

FastAPIの /docs と、プロジェクト内の docs/ フォルダは別物

ログイン機能は初期版では不要

Unityゲーム画面にWebテンプレートは使わない

HTMLテンプレートは将来の簡易ダッシュボードだけでよい

API通信失敗時でもゲーム本編を完了できるようにする

通信失敗時も固定の共通報酬と資料館CTAを表示する

テーマ判定と称号決定に乱数を使わない

ランダム処理は豆絞り柄の候補選択にだけ使う

外部URLは必ず登録済みURLへ限定する
