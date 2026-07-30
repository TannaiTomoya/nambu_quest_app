# 南部もぐり観光RPG — 実装ルール（初学者向け）

このファイルは、実装時に守るルールです。  
仕様の詳細は `docs/ai_context.md` と既存の要件ドキュメントを優先してください。

---

## 共通ルール

- 一度に複数機能を実装しない
- 1タスクごとに動作確認する（`docs/tasks.md` の完了条件を使う）
- 不要な抽象化をしない（インターフェースや過剰なディレクトリ分割を避ける）
- 既存要件にない機能を追加しない
- 秘密情報や環境依存値をコードへ直接書かない（`.env` や設定に分離する）
- エラーを握りつぶさない（ログまたはユーザー向けメッセージを残す）
- 初学者が追える短い関数を優先する
- Cursorルールファイルはハイフン表記に統一する  
  - 正：`.cursor/rules/nambu-quest-core.mdc`  
  - 誤：`.cursor/rules/nambu_quest_core.mdc`

---

## Python / FastAPI

- Pythonコードは型ヒントを付ける
- FastAPIの入力は Pydantic モデルで検証する
- Unityから受け取る値を信用しない
- 空気量、プレイ時間、配列の要素数を検証する  
  - 例：`remaining_air` の範囲、`selected_records` は最大2件、`play_time_seconds` が非負
- ルーター、DB、モデルの責務を混ぜすぎない
- 初期段階ではファイル分割を増やしすぎない
- 外部URLは任意入力を受け取らず、DB登録済みURLだけを使う
- Open Redirect を防ぐ（`GET /redirect/{destination_id}` でクエリから任意URLを受け取らない）
- SQLiteの外部キー制約を有効にする
- PeeWeeのDB接続をリクエストごとに適切に開閉する
- 複数保存は必要に応じてトランザクションを使う  
  - 例：`POST /analyze` で `play_results` と完了系イベントをまとめて保存する
- 日時は統一した方針で扱う（UTC かローカルかを決め、混在させない）
- DBファイルと `.env` を Git 管理しない

### 推薦・報酬

- 関心テーマの判定に乱数を使わない
- 称号はテーマ候補から決定的に1件選ぶ（`display_order`、選択順、`returned_safely`、`remaining_air`、safety条件）
- 同じ入力条件なら同じ称号を返す
- 称号の処理経路では `random` や `choice` などの乱数処理を使用しない
- 「称号候補からランダム選択」は実装しない
- ランダム性は豆絞り柄の候補選択（テーマ対応の有効3〜5件）にのみ使う
- 報酬結果で資料館CTAの有無を切り替えない

### MVPで優先するルート

1. `GET /`
2. `POST /analyze`
3. `GET /redirect/{destination_id}`

セッション開始、イベント一覧、ダッシュボードは後回しにしてよい。

---

## PeeWee

- 内部主キー（`sessions.id`）と匿名 `session_id`（TEXT）を混同しない
- `ForeignKeyField` のモデル属性名と DB カラム名の違いを意識する  
  - 例：モデル上 `session`、DB上 `session_id`
- 配列は JSON 文字列として保存する場合、変換処理を一箇所にまとめる  
  - 対象：`visited_points` / `completed_tasks` / `selected_records` / `metadata`
- 1セッション1プレイ結果の制約を守る（`play_results.session_id` のユニーク）
- テーブルを一度に全部実装しない
- 実装順は次を基本とする  
  1. `contents`  
  2. `sessions`  
  3. `play_results`  
  4. `tracking_events`  
  5. `tourism_events`（後回し）
- `tourism_events` は後回しにする
- `updated_at` は更新処理のたびに明示的に更新する
- DateTime の初期値には、呼び出し可能な関数を使う（起動時固定にしない）

---

## Unity / C#

- ゲーム中の移動と空気管理は Unity 内で行う
- 毎フレーム API 通信しない
- API URL を複数箇所に直書きしない（定数または設定を1箇所にまとめる）
- 通信中、成功、失敗の状態を分ける
- API失敗時にも固定の共通報酬（共通称号・共通豆絞り柄）と固定資料館案内を表示する（再送は1回まで）
- JSON のフィールド名を FastAPI 側と一致させる
- シーンを増やしすぎず、確認画面はパネルを優先する  
  - 推奨：`TitleScene` / `GameScene` / `ResultScene`
  - パネル例：`TutorialPanel` / `LocationConfirmPanel` / `RecordSelectPanel` / `ReturnConfirmPanel` / `LoadingPanel` / `ErrorPanel` / `CompletePanel`
- `NullReferenceException` を避けるため、参照確認を行う
- Inspector 設定が必要な項目をコメントまたはドキュメントに残す
- AI/Codex が直接書き換えてよい Unity 資産は、原則 `.cs` と `Assets/Editor/` 配下の Editor スクリプトのみ
- `.unity` / `.prefab` をテキスト（YAML）で直接編集しない（過去に TMP・Button・参照設定の破損が発生）
- シーン・プレハブの変更は Unity Editor API（Editor スクリプト）または手動 Inspector 経由で行う
- 一時 Editor スクリプトは「用途」「再実行しても安全か（重複を起こさないか）」「実行方法」をコメントで明示する
- 再生成の用途がない使い捨て Editor スクリプトは、役目を終えたら削除する（誤実行による手動調整の上書きを防ぐ）
- 公式詳細URLや Maps URL を Unity 内に固定しない（FastAPI 経由のパスを使う）
- 称号や柄の抽選ロジックを Unity 側で再実装しない（表示と失敗時フォールバックに限定する）
- 報酬画像を置く場合はプロジェクト直下の `uploads/` を使い、`static/` 配下には置かない

---

## セキュリティ・プライバシー

- ログイン機能を作らない
- 個人情報を収集しない（氏名、住所、電話番号、メールアドレスなど）
- 匿名セッションIDに個人を識別できる値を使わない
- メールアドレスや端末固有IDを保存しない
- APIキーを Unity へ埋め込まない
- 任意URLへのリダイレクトを許可しない
- エラーレスポンスへ内部パスや秘密情報を出さない

---

## 表現・史実に関する実装時の注意

- 事故や死亡を娯楽的な演出にしない
- 南部もぐりを宝探しとして表現しない
- 実在展示の正式名称や現在の展示状況を、確認前に断定する文言で埋め込まない
- 確認前は「展示予定」「展示候補」などにとどめ、詳細は公式ページへ委ねる

---

## 変更時の報告（推奨）

コードを変更したら、次を短く残す。

1. 対象タスク番号と完了条件
2. 変更したファイル
3. 確認方法（実行した操作）
4. 未確認事項
