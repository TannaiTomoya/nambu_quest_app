"""南部もぐり観光RPG — FastAPI最小起動。"""

from __future__ import annotations

from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request, Response

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
