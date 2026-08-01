"use strict";

// ===== ゲーム進行の共有状態 =====
const state = {
  startedAt: 0,
  stepDone: [false, false, false],
  gauge: 0,
  rank: 1,
  isRetry: false,
  selectedPoint: null,
  item: "",
  points: 0,
  timerId: null,
  timeLeft: 0,
};

// 未完了の工程のうち先頭のインデックスを返す（全完了なら -1）
function currentStepIndex() {
  return state.stepDone.findIndex(done => !done);
}
