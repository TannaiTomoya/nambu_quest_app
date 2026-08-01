"use strict";

// ===== 進行フローと初期化 =====
// 画面遷移の順序: intro → prep（工程タップ→連打）→ rank → select → dive → chest → removal → result

// ===== 画面1+2: 装備準備 =====
function startPrep() {
  ANALYTICS.gameStarted(); // 2回目以降の呼び出しは内部で無視される
  state.startedAt = Date.now();
  state.stepDone = [false, false, false];
  state.gauge = 0;
  state.isRetry = false;
  show("s-prep");
  setPrepMashMode(false);
  renderSteps();
  runTimer(CONFIG.PREP_TIME, onPrepTimeout);
}

function startRetry() {
  state.isRetry = true;
  show("s-prep");
  setPrepMashMode(false);
  renderSteps();
  runTimer(CONFIG.RETRY_TIME, onPrepTimeout);
}

function runTimer(seconds, onEnd) {
  clearInterval(state.timerId);
  state.timeLeft = seconds;
  updateTimerLabel();
  state.timerId = setInterval(() => {
    state.timeLeft -= 0.1;
    if (state.timeLeft <= 0) {
      state.timeLeft = 0;
      clearInterval(state.timerId);
      updateTimerLabel();
      onEnd();
      return;
    }
    updateTimerLabel();
  }, 100);
}

function tapStep(index) {
  // 順番どおりのタップだけ受け付ける（順序を伝えるための仕様）
  if (index !== currentStepIndex()) return;
  state.stepDone[index] = true;
  renderSteps();
  if (currentStepIndex() === -1) {
    if (state.isRetry) {
      // 再挑戦で必須工程が完了：ランク1で先へ進む（連打はなし）
      clearInterval(state.timerId);
      decideRank();
    } else {
      // 連打フェーズへ
      setPrepMashMode(true);
    }
  }
}

function tapMash() {
  state.gauge = Math.min(100, state.gauge + CONFIG.TAP_GAIN);
  document.getElementById("gauge").style.width = state.gauge + "%";
  const prepScreen = document.getElementById("s-prep");
  prepScreen.classList.remove("is-pumping");
  void prepScreen.offsetWidth;
  prepScreen.classList.add("is-pumping");
  if (typeof setTimeout === "function") {
    setTimeout(() => prepScreen.classList.remove("is-pumping"), 180);
  }
}

function onPrepTimeout() {
  if (currentStepIndex() !== -1) {
    // 必須工程が未完了
    if (state.isRetry) {
      // 再挑戦でも未完了 → 準備画面へ戻す（潜らせない）
      startPrep();
      return;
    }
    show("s-timeout");
    return;
  }
  decideRank();
}

// ===== 画面3: 準備ランク =====
function decideRank() {
  if (state.gauge >= CONFIG.RANK3_GAUGE) {
    state.rank = 3;
  } else if (state.gauge >= CONFIG.RANK2_GAUGE) {
    state.rank = 2;
  } else {
    state.rank = 1;
  }

  document.getElementById("rank-stars").textContent = "★".repeat(state.rank) + "☆".repeat(3 - state.rank);
  document.getElementById("rank-text").innerHTML =
    "準備ランク " + state.rank + "<br><br>" + STRINGS.rankTexts[state.rank];
  show("s-rank");
}

// ===== 潜水演出 =====
function startDive() {
  show("s-dive");
  const depthTargets = {
    1: 18,
    2: 36,
    3: 58,
  };
  const maxDepth = depthTargets[state.selectedPoint.rank] || 18;
  const phases = [
    {
      title: "潜水開始",
      text: "送気を確認しながら、海面からゆっくり降りていく。",
      depth: Math.round(maxDepth * 0.25),
      fill: 26,
      marker: "降下中",
    },
    {
      title: "海中探索",
      text: "潮の流れを読み、海底へのルートをたどる。",
      depth: Math.round(maxDepth * 0.65),
      fill: 62,
      marker: "探索中",
    },
    {
      title: "目的地到着",
      text: state.selectedPoint.name + "に到着。海底に反応がある。",
      depth: maxDepth,
      fill: 92,
      marker: "発見",
    },
  ];
  document.getElementById("dive-air").textContent = state.selectedPoint.air;
  document.getElementById("dive-rank").textContent = "★".repeat(state.selectedPoint.rank);
  let i = 0;
  renderDivePhase(phases[0]);
  let diveTimerId;
  diveTimerId = setInterval(() => {
    i++;
    if (i >= phases.length) {
      clearInterval(diveTimerId);
      resetChestView();
      document.getElementById("chest-location").textContent = state.selectedPoint.name;
      document.getElementById("chest-next").style.display = "none";
      show("s-chest");
      return;
    }
    renderDivePhase(phases[i]);
  }, 1050);
}

// ===== 画面5: 宝箱 =====
function openChest() {
  if (document.getElementById("chest-next").style.display !== "none") return;
  state.points = CONFIG.POINTS_MIN +
    Math.floor(Math.random() * (CONFIG.POINTS_MAX - CONFIG.POINTS_MIN + 1));
  // 発表用: 地点に招待券が設定されていれば必ずその券を出す。
  // 未設定の地点では確率でスパルタキャンプ招待券が出る
  const guaranteed = state.selectedPoint.ticket
    ? TICKETS[state.selectedPoint.ticket]
    : null;
  state.ticket = guaranteed ||
    (Math.random() < CONFIG.TICKET_RATE ? TICKETS.sparta : null);
  state.isTicket = state.ticket !== null;
  document.getElementById("inspect-button").disabled = true;
  document.getElementById("inspect-button").textContent = "調査中...";
  document.getElementById("chest-title").textContent = "宝箱を調査中";
  document.getElementById("chest-art").classList.add("is-inspecting");
  document.getElementById("chest-text").innerHTML =
    "箱の周囲を払った。<br>古い留め具が少し動いた。";

  const delay = typeof setTimeout === "function"
    ? setTimeout
    : (fn) => fn();
  delay(() => {
    showChestOpening();
    delay(showChestReward, 720);
  }, 680);
}

// ===== 画面6: 装備解除演出 =====
function startRemoval() {
  show("s-removal");
  const video = document.getElementById("return-video");
  const removalScreen = document.getElementById("s-removal");
  const soundButton = document.getElementById("video-sound-button");
  const removalHint = document.getElementById("removal-hint");

  soundButton.hidden = true;
  video.controls = false;
  video.muted = false;
  video.volume = 1;
  removalHint.textContent = "タップでスキップ";

  removalScreen.onclick = (event) => {
    if (event.target.closest("#video-sound-button")) return;
    showResult();
  };

  soundButton.onclick = (event) => {
    event.stopPropagation();
    video.muted = false;
    video.volume = 1;
    const retryPromise = video.play();
    if (retryPromise && typeof retryPromise.then === "function") {
      retryPromise.then(() => {
        soundButton.hidden = true;
        removalHint.textContent = "タップでスキップ";
      }).catch(() => {
        removalHint.textContent = "端末の音量設定を確認してください";
      });
    }
  };

  video.onended = showResult;
  if (video.readyState > 0) video.currentTime = 0;
  const playPromise = video.play();
  if (playPromise && typeof playPromise.catch === "function") {
    playPromise.catch(() => {
      soundButton.hidden = false;
      removalHint.textContent = "音声付きで再生をタップ";
    });
  }
}

// ===== 結果 =====
function showResult() {
  renderResult();
  ANALYTICS.gameCompleted(); // 動画終了とスキップの両方から呼ばれるが、送信は1回だけ
}

// ===== 初期化 =====
function wireButtons() {
  document.getElementById("intro-start").addEventListener("click", startPrep);
  document.getElementById("timeout-retry").addEventListener("click", startRetry);
  document.getElementById("rank-next").addEventListener("click", () => { show("s-select"); renderSelect(); });
  document.getElementById("chest-next").addEventListener("click", startRemoval);
  document.getElementById("result-replay").addEventListener("click", () => location.reload());
  document.querySelectorAll(".step-card[data-step]").forEach(card => {
    card.addEventListener("click", () => tapStep(Number(card.dataset.step)));
  });
  document.getElementById("mash-button").addEventListener("click", tapMash);
  document.getElementById("inspect-button").addEventListener("click", openChest);
}

async function initGame() {
  try {
    const data = await loadGameConfig();
    ANALYTICS.init({ config_version: data.config_version, features: data.features });
  } catch (e) {
    // 設定が読めない場合はゲームを開始できない（file://直開き・サーバー未起動など）
    const lead = document.querySelector("#s-intro .lead");
    if (lead) {
      lead.innerHTML =
        "設定ファイルを読み込めませんでした。<br>ローカルサーバー経由で開いてください。<br>" +
        "<span style='font-size:13px'>例: python3 -m http.server 8080</span>";
    }
    document.getElementById("intro-start").disabled = true;
    return;
  }
  applyStrings();
  wireButtons();
}

initGame();
