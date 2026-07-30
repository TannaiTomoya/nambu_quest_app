"""SQLite / PeeWee の接続管理。"""

from __future__ import annotations

from pathlib import Path

from peewee import SqliteDatabase

BASE_DIR: Path = Path(__file__).resolve().parent
DATA_DIR: Path = BASE_DIR / "data"
DB_PATH: Path = DATA_DIR / "nambu_quest.db"

db = SqliteDatabase(None)


def init_db() -> None:
    """DBファイルの場所を決め、外部キー制約を有効にする。"""
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    db.init(
        str(DB_PATH),
        pragmas={
            "foreign_keys": 1,
        },
    )


def connect_db() -> None:
    """リクエスト用にDB接続を開く。"""
    if db.is_closed():
        db.connect()


def close_db() -> None:
    """DB接続を閉じる。"""
    if not db.is_closed():
        db.close()


def create_tables() -> None:
    """必要なテーブルだけ作成する。"""
    from backend.models import Content

    connect_db()
    try:
        db.create_tables([Content])
    finally:
        close_db()
