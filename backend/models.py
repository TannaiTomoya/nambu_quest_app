"""PeeWeeモデル定義。"""

from __future__ import annotations

from datetime import datetime, timezone

from peewee import (
    AutoField,
    BooleanField,
    CharField,
    DateTimeField,
    IntegerField,
    Model,
    TextField,
)

from backend.database import db


def utc_now() -> datetime:
    """日時の初期値用（呼び出し時に評価される）。"""
    return datetime.now(timezone.utc)


class BaseModel(Model):
    """全モデル共通のベース。"""

    class Meta:
        database = db


class Content(BaseModel):
    """観光案内（資料館など）の掲載先。"""

    id = AutoField()
    destination_key = CharField(unique=True)
    category = CharField()
    title_ja = CharField()
    summary_ja = TextField()
    official_url = TextField()
    maps_url = TextField(null=True)
    is_active = BooleanField(default=True)
    display_order = IntegerField(default=0)
    created_at = DateTimeField(default=utc_now)
    updated_at = DateTimeField(default=utc_now)

    class Meta:
        table_name = "contents"
