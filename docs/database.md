南部もぐり観光RPG データベース設計

1. 設計方針

このMVPでは、次の4種類の情報を保存します。

匿名セッション

1プレイごとの結果

クリックなどの行動イベント

資料館・Google Maps・イベントなどの観光コンテンツ

初期実装ではSQLiteを使用し、PythonのPeeWeeから操作する想定です。

個人名、メールアドレス、電話番号などの個人情報は保存しません。1回のプレイごとに匿名のセッションIDを発行し、ゲーム開始から公式ページ遷移までを同じセッションとして追跡します。

2. テーブル一覧

テーブル名

役割

sessions

1回のゲーム利用を識別する

play_results

探索・帰還・推薦結果を1プレイ単位で保存する

tracking_events

ゲーム開始、完了、公式ページクリックなどを時系列で保存する

contents

資料館、Google Maps、周辺観光情報などの掲載先を管理する

tourism_events

期間限定イベントを管理する

MVPでは、最初の4テーブルで十分です。イベント補助カードを実装する場合のみ tourism_events を追加します。

3. リレーション概要

sessions
  ├── 1対1 ── play_results
  └── 1対多 ── tracking_events

contents
  └── 1対多 ── tracking_events

tourism_events
  └── 必要に応じて tracking_events から参照

1回のセッションには、原則1件のプレイ結果があります。一方、クリックや表示などの行動は複数回発生するため、tracking_events は1セッションに対して複数件保存します。

4. 各テーブルの詳細

4.1 sessions

役割

1回のゲーム利用を識別するためのテーブルです。

名前やメールアドレスではなく、ランダムな匿名IDを使います。これにより、受付QRから来た人がゲームを完了し、資料館ページを開いたかを同じ流れとして確認できます。

カラム

カラム名

データ型

必須

説明

id

INTEGER

Yes

内部用の連番ID

session_id

TEXT

Yes

外部から使う匿名セッションID

source

TEXT

No

流入元。例：hotel_a_front

language

TEXT

Yes

表示言語。初期値は ja

client_version

TEXT

No

Unityアプリのバージョン

started_at

DATETIME

Yes

セッション開始日時

completed_at

DATETIME

No

ゲーム完了日時

created_at

DATETIME

Yes

DB登録日時

updated_at

DATETIME

Yes

最終更新日時

キー・制約

主キー：id

ユニーク制約：session_id

外部キー：なし

インデックス候補

session_id

source

started_at

なぜ必要か

session_id がないと、ゲーム開始、完了、公式ページクリックが同じ人の一連の行動か分かりません。

4.2 play_results

役割

1回のプレイ結果を保存するテーブルです。

どの地点を訪れ、どの記録を持ち帰り、どの推薦テーマになったかを保存します。

カラム

カラム名

データ型

必須

説明

id

INTEGER

Yes

主キー

session_id

INTEGER

Yes

sessions.id を参照

visited_points

TEXT

Yes

訪問地点をJSON文字列で保存

completed_tasks

TEXT

Yes

完了作業をJSON文字列で保存

selected_records

TEXT

Yes

持ち帰り記録をJSON文字列で保存

remaining_air

INTEGER

Yes

帰還時の残り空気

returned_safely

BOOLEAN

Yes

安全帰還したか

play_time_seconds

INTEGER

Yes

プレイ時間

theme

TEXT

Yes

推薦テーマ（プレイ内容から決定。乱数なし）

headline

TEXT

Yes

結果画面の見出し

reason

TEXT

Yes

推薦理由

title_key

TEXT

No

選ばれた称号キー（テーマから決定的に1件。乱数なし）

tenugui_pattern_key

TEXT

No

選ばれた豆絞り柄キー（候補内ランダム）

reward_theme

TEXT

No

報酬グループ（theme と同一運用でも可。実装時に方針を固定する）

reward_generated_at

DATETIME

No

報酬確定日時

destination_id

INTEGER

No

contents.id を参照

created_at

DATETIME

Yes

保存日時

キー・制約

主キー：id

外部キー：

session_id → sessions.id

destination_id → contents.id

ユニーク制約：

session_id

1セッションにつき1プレイ結果とするため、session_id をユニークにします。

インデックス候補

session_id

theme

title_key

tenugui_pattern_key

destination_id

created_at

なぜ必要か

tracking_events だけでは、プレイヤーが何を選んだかを分析しにくいためです。このテーブルには、そのプレイ全体の確定結果をまとめて保存します。

称号と豆絞り柄の扱い

称号は同じ入力条件なら同じ title_key になる設計とする

豆絞り柄は同一テーマでも tenugui_pattern_key が変わりうる

称号候補・柄候補のマスタは、MVPでは JSON 設定ファイルで管理してよい（テーブル追加は必須ではない）

保存形式の注意

SQLiteには配列型がないため、以下はJSON文字列として保存します。

visited_points

completed_tasks

selected_records

例：

["near", "middle"]

4.3 tracking_events

役割

ゲーム内外の行動を、発生順に1件ずつ保存します。

例：

ゲーム開始

ゲーム完了

推薦表示

公式ページクリック

Google Maps起動

訪問候補保存

カラム

カラム名

データ型

必須

説明

id

INTEGER

Yes

主キー

session_id

INTEGER

Yes

sessions.id を参照

event_name

TEXT

Yes

行動イベント名

destination_id

INTEGER

No

contents.id を参照

tourism_event_id

INTEGER

No

tourism_events.id を参照

metadata

TEXT

No

補足情報をJSON文字列で保存

occurred_at

DATETIME

Yes

行動発生日時

created_at

DATETIME

Yes

DB登録日時

キー・制約

主キー：id

外部キー：

session_id → sessions.id

destination_id → contents.id

tourism_event_id → tourism_events.id

ユニーク制約：

MVPでは必須なし

インデックス候補

session_id

event_name

occurred_at

(session_id, event_name) の複合インデックス

なぜ必要か

プレイ結果の行にクリック済みフラグを追加するだけでは、複数回のクリックや発生順を確認できません。

イベントを別テーブルにすると、以下を集計しやすくなります。

game_started
game_completed
recommendation_displayed
official_detail_clicked
map_opened
saved_for_later

重複記録への注意

同じボタンを連続で押した場合、同じイベントが複数回保存される可能性があります。

初期版では、以下のどちらかで対応します。

同一セッション・同一イベントを数秒以内に重複保存しない

集計時にユニークなセッション数で数える

4.4 contents

役割

資料館、Google Maps、周辺観光情報など、Unityへ返す掲載先を管理します。

URLをUnity内に直接書かず、FastAPI側で変更できるようにするために必要です。

カラム

カラム名

データ型

必須

説明

id

INTEGER

Yes

主キー

destination_key

TEXT

Yes

外部向け識別子

category

TEXT

Yes

種別

title_ja

TEXT

Yes

日本語タイトル

summary_ja

TEXT

Yes

日本語説明

official_url

TEXT

Yes

公式詳細ページURL

maps_url

TEXT

No

Google Maps URL

is_active

BOOLEAN

Yes

掲載中か

display_order

INTEGER

Yes

表示順

created_at

DATETIME

Yes

登録日時

updated_at

DATETIME

Yes

更新日時

categoryの例

museum

map

tourism

event_list

キー・制約

主キー：id

ユニーク制約：destination_key

外部キー：なし

インデックス候補

destination_key

category

is_active

(is_active, display_order) の複合インデックス

なぜ必要か

URLや紹介文をUnityに埋め込むと、変更のたびにUnityを再ビルドする必要があります。

contents で管理すれば、FastAPI側の変更だけでリンク先や説明を更新できます。

4.5 tourism_events

役割

期間限定イベントを最大3件まで補助カードとして表示するためのテーブルです。

主推薦は資料館であり、イベントは補助導線として扱います。

カラム

カラム名

データ型

必須

説明

id

INTEGER

Yes

主キー

event_key

TEXT

Yes

外部向け識別子

title_ja

TEXT

Yes

イベント名

summary_ja

TEXT

Yes

短い説明

start_date

DATE

Yes

開始日

end_date

DATE

Yes

終了日

official_url

TEXT

Yes

公式ページURL

approved

BOOLEAN

Yes

担当職員が承認済みか

is_active

BOOLEAN

Yes

手動掲載状態

display_order

INTEGER

Yes

表示順

created_at

DATETIME

Yes

登録日時

updated_at

DATETIME

Yes

更新日時

キー・制約

主キー：id

ユニーク制約：event_key

外部キー：なし

インデックス候補

event_key

approved

is_active

start_date

end_date

(approved, is_active, start_date, end_date) の複合インデックス

表示条件

approved = true
かつ
is_active = true
かつ
start_date <= 今日
かつ
今日 <= end_date

なぜ必要か

イベントは開催期間があるため、通常の観光コンテンツとは管理方法が異なります。

終了したイベントを自動で非表示にするため、別テーブルに分けます。

5. 推奨する最小構成

ハッカソンで最初に実装するテーブルは以下です。

sessions
play_results
tracking_events
contents

tourism_events は余裕があれば追加します。

この順番にする理由は、最初に必要なのが以下だからです。

プレイを識別する

プレイ結果を保存する

公式ページ遷移を記録する

URLをFastAPI側で管理する

6. 集計できる指標

ゲーム完了率

game_completed のユニークsession数
÷
game_started のユニークsession数

公式詳細ページ遷移率

official_detail_clicked のユニークsession数
÷
game_completed のユニークsession数

Google Maps起動率

map_opened のユニークsession数
÷
game_completed のユニークsession数

流入元別完了率

特定sourceの完了セッション数
÷
特定sourceの開始セッション数

7. PeeWeeで実装する場合の注意点

7.1 SQLite接続

PeeWeeではSQLiteのDBファイルを1つ指定します。

例：

data/nambu_quest.db

相対パスは起動場所によって変わるため、実装時はファイルの絶対位置を基準に組み立てます。

7.2 ForeignKeyFieldの命名

PeeWeeでは以下のように外部キーを定義すると、DB上では通常 session_id のようなカラムになります。

設計上のモデル名とDBカラム名を混同しないようにします。

例：

モデル上：session
DB上：session_id

7.3 JSONFieldについて

SQLiteとPeeWeeの組み合わせでは、配列データをTEXTとしてJSON保存する方が初学者には分かりやすいです。

保存対象：

visited_points

completed_tasks

selected_records

metadata

読み込み時にJSONからPythonのlistやdictへ戻します。

7.4 BooleanField

SQLiteでは真偽値が内部的に0と1で保存されます。

PeeWeeの BooleanField を使えば、Python側では True / False として扱えます。

7.5 DateTimeFieldの初期値

created_at などは、アプリ起動時の日時で固定されないよう注意します。

日時そのものではなく、呼び出し可能な関数を初期値として指定する必要があります。

7.6 updated_at

PeeWeeは更新時刻を自動更新しません。

更新処理のたびに updated_at を明示的に更新します。

7.7 Foreign key制約

SQLiteでは外部キー制約が無効になっている場合があります。

接続時に外部キー制約を有効にする設定が必要です。

これをしないと、存在しないセッションIDを持つプレイ結果が保存される可能性があります。

7.8 テーブル作成

開発初期は、アプリ起動時に必要テーブルを作成しても構いません。

ただし、本格運用ではテーブル変更を安全に行うため、マイグレーション手段が必要になります。

ハッカソンでは以下で十分です。

開発用DBを削除して作り直せる

テストデータと本番データを分ける

テーブル構造を変更したらDBを再作成する

7.9 N+1問題

MVPのデータ量では大きな問題になりにくいですが、外部キー先を大量に繰り返し取得する場合は注意します。

初期版では複雑な最適化より、クエリを分かりやすく保つことを優先します。

7.10 トランザクション

POST /analyze で以下を同時に保存する場合は、まとめて成功・失敗させます。

play_results

game_completedイベント

recommendation_displayedイベント

途中だけ保存されると集計がずれるため、PeeWeeのトランザクションを使う想定です。

7.11 同時書き込み

SQLiteは大量の同時書き込みに向いていません。

ただし、ハッカソンの10〜20人規模のテストには十分です。

将来、多数の宿泊施設で同時利用する場合はPostgreSQLへの移行を検討します。

7.12 Open Redirect対策

GET /redirect/{destination_id} では、利用者から任意のURLを受け取ってはいけません。

必ず以下の流れにします。

destination_idを受け取る
↓
contentsテーブルからURLを検索
↓
登録済みURLだけへ転送

8. 実装順

初学者向けの推奨順です。

contents

sessions

play_results

tracking_events

tourism_events

最初に contents を作ると、固定の資料館情報を返すAPIを早く確認できます。

9. 未実装にするもの

初期版では以下は作りません。

ユーザーアカウント

メールアドレス保存

管理者権限

複雑な監査ログ

全移動座標ログ

動画や画面録画

AI推薦用の学習データ

Instagram自動連携

大規模な分析基盤

10. MVP完了条件

匿名セッションを保存できる

1プレイ結果を保存できる

選ばれた称号キーと豆絞り柄キーを保存できる

同じ入力条件なら同じ title_key が保存される

公式詳細ページクリックを保存できる

Google Maps起動を保存できる

contents から公式URLを取得できる

完了率と遷移率を集計できる

個人情報を保存していない
