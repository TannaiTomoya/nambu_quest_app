"""南部もぐり観光RPG — FastAPI最小起動。"""

from __future__ import annotations

from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request, Response
from pydantic import BaseModel, Field

from backend.database import close_db, connect_db, create_tables, init_db


@asynccontextmanager
async def lifespan(_app: FastAPI) -> AsyncIterator[None]:
    """起動時にDBを準備し、終了時に接続を閉じる。"""
    init_db()
    create_tables()
    yield
    close_db()


app = FastAPI(
    title="Nambu Quest API",
    lifespan=lifespan,
)


@app.middleware("http")
async def open_close_db(request: Request, call_next) -> Response:
    """リクエストごとにPeeWeeの接続を開閉する。"""
    connect_db()
    try:
        return await call_next(request)
    finally:
        close_db()


@app.get("/")
def root() -> dict[str, str]:
    """起動確認用トップページ。"""
    return {"message": "南部もぐりRPG"}


class AnalyzeRequest(BaseModel):
    """疎通確認用の最小リクエスト。フィールド名はUnity側と一致させる。"""

    session_id: str = Field(min_length=1, max_length=64)
    visited_points: list[str] = Field(default_factory=list)
    selected_records: list[str] = Field(default_factory=list, max_length=2)
    remaining_air: int = Field(ge=0, le=100)
    returned_safely: bool


@app.post("/analyze")
def analyze(request_body: AnalyzeRequest) -> dict[str, str]:
    """Unity との疎通確認用。固定レスポンスのみ返し、DB保存は行わない。

    推薦判定・称号決定・play_results 保存は後続タスクで実装する。
    """
    return {
        "status": "ok",
        "headline": "通信テスト成功",
        "message": "UnityとFastAPIが接続されました",
    }
