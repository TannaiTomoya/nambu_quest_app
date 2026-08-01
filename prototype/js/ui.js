"use strict";

// ===== 画面描画 =====
// DOMの書き換えだけを担当する。進行フローは app.js、状態は game-state.js。

function show(id) {
  document.querySelectorAll(".screen").forEach(s => s.classList.remove("active"));
  document.getElementById(id).classList.add("active");
}

function updateTimerLabel() {
  const label = document.getElementById("prep-timer");
  label.textContent = state.timeLeft.toFixed(1);
  label.classList.toggle("warn", state.timeLeft <= 3);
}

function renderSteps() {
  const current = currentStepIndex();
  for (let i = 0; i < 3; i++) {
    const card = document.getElementById("step-" + i);
    card.className = "step-card" +
      (state.stepDone[i] ? " done" : (i === current ? " current" : ""));
    if (state.stepDone[i]) {
      card.textContent = card.textContent.replace(/^(✓ )?/, "✓ ");
    }
  }
  renderPrepCharacter();
}

function renderPrepCharacter() {
  const stage = state.stepDone.filter(done => done).length;
  const character = document.getElementById("mogu-character");
  const note = document.getElementById("prep-note");
  if (character) character.dataset.stage = stage;
  if (note) note.innerHTML = STRINGS.prepNotes[stage];
  document.querySelectorAll(".gear-tag").forEach((tag) => {
    tag.classList.toggle("on", Number(tag.dataset.gear) < stage);
  });
}

function setPrepMashMode(isMash) {
  document.getElementById("s-prep").classList.toggle("prep-mash-mode", isMash);
  document.getElementById("prep-mash").style.display = isMash ? "block" : "none";
  document.getElementById("prep-steps").style.display = isMash ? "none" : "block";
}

// ===== 画面4: 地点選択 =====
function renderSelect() {
  const list = document.getElementById("select-list");
  list.innerHTML = "";
  POINTS_DATA.forEach(point => {
    const unlocked = state.rank >= point.rank;
    const btn = document.createElement("button");
    btn.className = "btn" + (unlocked ? "" : " locked");
    btn.style.marginBottom = "12px";
    btn.innerHTML = unlocked
      ? point.name + "<br><span style='font-size:13px'>消費空気 " + point.air + "</span>"
      : point.name + "<br><span style='font-size:13px'>🔒 準備ランク" + point.rank + "で潜れる</span>";
    if (unlocked) {
      btn.onclick = () => { state.selectedPoint = point; startDive(); };
    }
    list.appendChild(btn);
  });
}

function renderDivePhase(phase) {
  document.getElementById("dive-log-title").textContent = phase.title;
  document.getElementById("dive-text").textContent = phase.text;
  document.getElementById("dive-depth").textContent = phase.depth + "m";
  document.getElementById("dive-depth-fill").style.height = phase.fill + "%";
  document.getElementById("dive-marker").textContent = phase.marker;
}

// ===== 画面5: 宝箱 =====
function resetChestView() {
  document.getElementById("s-chest").classList.remove("is-opened");
  document.getElementById("chest-title").textContent = "海底イベント";
  document.getElementById("chest-art").classList.remove("is-opened", "is-inspecting");
  document.getElementById("chest-art").innerHTML =
    "<svg viewBox='0 0 160 120' aria-hidden='true'>" +
      "<path d='M26 54 Q80 18 134 54 L134 92 L26 92Z' fill='#b97433' stroke='#061123' stroke-width='7' stroke-linejoin='round'/>" +
      "<rect x='22' y='50' width='116' height='54' rx='6' fill='#9a5b2c' stroke='#061123' stroke-width='7'/>" +
      "<path d='M29 56 L80 78 L131 56' fill='none' stroke='#d9b078' stroke-width='8'/>" +
      "<rect x='68' y='62' width='25' height='23' rx='3' fill='#f0c85a' stroke='#061123' stroke-width='5'/>" +
      "<path d='M34 95 L128 95' stroke='#5b341c' stroke-width='6' stroke-linecap='round'/>" +
    "</svg>";
  document.getElementById("chest-command").style.display = "grid";
  document.getElementById("chest-text").textContent = "海底に古い箱が沈んでいる。";
  document.getElementById("inspect-button").disabled = false;
  document.getElementById("inspect-button").textContent = "▶ 調べる";
  document.getElementById("s-chest").scrollTop = 0;
}

function showChestOpening() {
  document.getElementById("chest-art").classList.remove("is-inspecting");
  document.getElementById("chest-art").classList.add("is-opened");
  document.getElementById("chest-title").textContent = "宝箱が開いた！";
  document.getElementById("chest-command").style.display = "none";
  document.getElementById("chest-art").innerHTML =
    "<svg viewBox='0 0 160 120' aria-hidden='true'>" +
      "<path d='M22 52 L78 18 L135 52' fill='none' stroke='#f0c85a' stroke-width='8' stroke-linecap='round'/>" +
      "<rect x='22' y='58' width='116' height='46' rx='6' fill='#9a5b2c' stroke='#061123' stroke-width='7'/>" +
      "<path d='M80 10 L90 36 L118 37 L96 53 L104 82 L80 65 L56 82 L64 53 L42 37 L70 36Z' fill='#f0c85a' stroke='#061123' stroke-width='5'/>" +
      "<rect x='68' y='66' width='25' height='23' rx='3' fill='#f0c85a' stroke='#061123' stroke-width='5'/>" +
    "</svg>";
  document.getElementById("chest-text").innerHTML =
    "宝箱が開いた！<br>中から光があふれている...";
}

function showChestReward() {
  if (state.isTicket) {
    state.item = state.ticket.name;
    document.getElementById("s-chest").classList.add("is-opened");
    document.getElementById("chest-title").textContent = "特別なものが眠っていた！";
    document.getElementById("chest-text").innerHTML =
      "光の中から招待券が現れた！<br>" +
      "<div class='ticket-card'>" +
        "<div style='font-size:36px'>" + state.ticket.iconHtml + "</div>" +
        "<div class='ticket-title'>" +
          (state.ticket.titleHtml || state.ticket.name.replace("招待券", "<br>招待券")) +
        "</div>" +
        "<div class='ticket-sub'>" + state.ticket.sub + "</div>" +
        "<a class='ticket-link' href='" + ANALYTICS.ticketHref(state.ticket.destinationId) + "' target='_blank' rel='noopener'>詳細を見る</a>" +
      "</div>" +
      "もぐりポイント +" + state.points;
  } else {
    state.item = ITEMS[Math.floor(Math.random() * ITEMS.length)];
    document.getElementById("s-chest").classList.add("is-opened");
    document.getElementById("chest-art").innerHTML =
      "<svg viewBox='0 0 160 120' aria-hidden='true'>" +
        "<path d='M22 52 L78 18 L135 52' fill='none' stroke='#f0c85a' stroke-width='8' stroke-linecap='round'/>" +
        "<rect x='22' y='58' width='116' height='46' rx='6' fill='#9a5b2c' stroke='#061123' stroke-width='7'/>" +
        "<circle cx='80' cy='44' r='23' fill='#7fd7ee' stroke='#061123' stroke-width='6'/>" +
        "<rect x='68' y='66' width='25' height='23' rx='3' fill='#f0c85a' stroke='#061123' stroke-width='5'/>" +
      "</svg>";
    document.getElementById("chest-text").innerHTML =
      "光の中からアイテムが現れた！<br><b>" + state.item + "</b> を見つけた！<br>もぐりポイント +" + state.points;
  }

  document.getElementById("chest-next").style.display = "block";
  document.getElementById("chest-text").scrollIntoView({ behavior: "smooth", block: "start" });
}

// ===== 結果 =====
function renderResult() {
  const elapsed = ((Date.now() - state.startedAt) / 1000).toFixed(0);
  document.getElementById("r-rank").textContent = "★".repeat(state.rank);
  document.getElementById("r-point").textContent = state.selectedPoint.name;
  document.getElementById("r-item").innerHTML = state.isTicket
    ? "🎟️ <a href='" + ANALYTICS.ticketHref(state.ticket.destinationId) + "' target='_blank' rel='noopener' style='color:#ffd45c'>" +
      state.item + "</a>"
    : state.item;
  document.getElementById("r-points").textContent = "+" + state.points;
  document.getElementById("r-time").textContent = elapsed + " 秒";
  show("s-result");
}

// ===== 初期文言の反映（JSONの表示文言を静的HTMLへ流し込む） =====
function applyStrings() {
  document.querySelector("#s-intro h1").textContent = STRINGS.introTitle;
  document.querySelector("#s-intro .lead").innerHTML = STRINGS.introLead;
  STRINGS.stepLabels.forEach((label, i) => {
    document.getElementById("step-" + i).textContent = label;
  });
  document.querySelectorAll(".gear-tag").forEach((tag) => {
    tag.textContent = STRINGS.gearTags[Number(tag.dataset.gear)];
  });
  document.getElementById("prep-note").innerHTML = STRINGS.prepNotes[0];
  document.querySelector("#prep-mash .gauge-label").textContent = STRINGS.mashGuide;
  document.getElementById("timeout-text").innerHTML = STRINGS.timeoutText;
  document.querySelector(".return-video-caption").innerHTML = STRINGS.returnCaption;
}
