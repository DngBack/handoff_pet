/* Codex Pet: a dependency-free exercise reminder for web pages. */
(() => {
  "use strict";

  const DEFAULT_INTERVAL = 25 * 60;
  const DEFAULT_DURATION = 2 * 60;
  const FRAME_COUNT = 5;

  function positiveSeconds(value, fallback) {
    if (value == null || value === "") return fallback;
    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
  }

  class CodexPet extends HTMLElement {
    constructor() {
      super();
      this.attachShadow({ mode: "open" });
      this._state = "waiting";
      this._frame = 0;
      this._clicks = 0;
      this._autoMove = true;
      this._drag = null;
      this._timers = new Set();
      this._dueAt = 0;
      this._hideAt = 0;
      this._remainingVisible = 0;
      this._onVisibility = () => this._handleVisibility();
      this._onResize = () => this._clampPosition();
    }

    connectedCallback() {
      if (this._mounted) return;
      this._mounted = true;
      this.intervalSeconds = positiveSeconds(this.getAttribute("interval-seconds"), DEFAULT_INTERVAL);
      this.durationSeconds = positiveSeconds(this.getAttribute("duration-seconds"), DEFAULT_DURATION);
      this.message = this.getAttribute("message") || "Đứng dậy tập thể dục thôi!";
      this._render();
      document.addEventListener("visibilitychange", this._onVisibility);
      window.addEventListener("resize", this._onResize);
      if (this.hasAttribute("start-immediately")) this.showNow();
      else this._scheduleNext();
    }

    disconnectedCallback() {
      this._clearTimers();
      document.removeEventListener("visibilitychange", this._onVisibility);
      window.removeEventListener("resize", this._onResize);
      this._state = "waiting";
      this._mounted = false;
    }

    showNow() {
      if (!this._mounted) return;
      this._clearTimers();
      this._state = "visible";
      this._frame = 0;
      this._clicks = 0;
      this._autoMove = true;
      this._drag = null;
      this._remainingVisible = this.durationSeconds * 1000;
      this._hideAt = 0;
      this._message.textContent = this.message;
      this._panel.hidden = false;
      this._panel.style.top = "auto";
      this._panel.style.bottom = "12px";
      this._panel.style.left = "12px";
      this._pet.dataset.frame = "0";
      this._setPetLabel();
      this._startVisibleTimers();
    }

    dismiss() {
      if (this._state !== "visible") return;
      this._clearTimers();
      this._drag = null;
      this._panel.hidden = true;
      this._state = "waiting";
      this._scheduleNext();
    }

    _scheduleNext() {
      this._dueAt = Date.now() + this.intervalSeconds * 1000;
      this._armReminder();
    }

    _armReminder() {
      if (this._state !== "waiting") return;
      this._clearTimers();
      const remaining = this._dueAt - Date.now();
      if (remaining <= 0) {
        if (!document.hidden) this.showNow();
        return;
      }
      this._timeout(() => this._armReminder(), Math.min(remaining, 2147483647));
    }

    _startVisibleTimers() {
      if (document.hidden) return;
      this._hideAt = Date.now() + this._remainingVisible;
      this._timeout(() => this.dismiss(), this._remainingVisible);
      this._interval(() => {
        this._frame = (this._frame + 1) % FRAME_COUNT;
        this._pet.dataset.frame = String(this._frame);
      }, 140);
      this._interval(() => {
        if (!this._autoMove || this._drag) return;
        const width = this._panel.getBoundingClientRect().width;
        const next = this._panel.offsetLeft + 3;
        this._panel.style.left = `${next + width > window.innerWidth ? 12 : next}px`;
      }, 50);
    }

    _handleVisibility() {
      if (this._state === "waiting") {
        if (!document.hidden) this._armReminder();
      } else if (document.hidden) {
        if (this._hideAt) this._remainingVisible = Math.max(0, this._hideAt - Date.now());
        this._clearTimers();
        this._hideAt = 0;
      } else if (this._remainingVisible <= 0) {
        this.dismiss();
      } else {
        this._startVisibleTimers();
      }
    }

    _timeout(callback, delay) {
      const id = window.setTimeout(() => {
        this._timers.delete(id);
        callback();
      }, delay);
      this._timers.add(id);
    }

    _interval(callback, delay) {
      const id = window.setInterval(callback, delay);
      this._timers.add(id);
    }

    _clearTimers() {
      for (const id of this._timers) {
        window.clearTimeout(id);
        window.clearInterval(id);
      }
      this._timers.clear();
    }

    _clampPosition() {
      if (this._state !== "visible") return;
      const rect = this._panel.getBoundingClientRect();
      const left = Math.min(Math.max(rect.left, 0), Math.max(0, window.innerWidth - rect.width));
      this._panel.style.left = `${left}px`;
      if (this._panel.style.top !== "auto") {
        const top = Math.min(Math.max(rect.top, 0), Math.max(0, window.innerHeight - rect.height));
        this._panel.style.top = `${top}px`;
      }
    }

    _setPetLabel() {
      const remaining = 3 - this._clicks;
      this._pet.setAttribute("aria-label", `Thú cưng nhắc vận động. Nhấn ${remaining} lần để tắt lời nhắc.`);
    }

    _countClick() {
      if (this._state !== "visible") return;
      this._clicks += 1;
      if (this._clicks >= 3) {
        this.dismiss();
      } else {
        this._message.textContent = `Nhấn thêm ${3 - this._clicks} lần để tắt`;
        this._setPetLabel();
      }
    }

    _render() {
      this.shadowRoot.innerHTML = `
        <style>
          :host { all: initial; }
          *, *::before, *::after { box-sizing: border-box; }
          .panel { position: fixed; left: 12px; bottom: 12px; z-index: 2147483647; width: min(320px, calc(100vw - 24px)); display: flex; flex-direction: column; align-items: center; pointer-events: none; font-family: system-ui, sans-serif; user-select: none; }
          .panel[hidden] { display: none; }
          .bubble { position: relative; max-width: 100%; padding: 14px 18px; border: 3px solid #142c38; border-radius: 22px; background: #fffdf3; box-shadow: 0 7px 0 #142c38, 0 14px 30px #142c3840; color: #142c38; text-align: center; font-size: clamp(19px, 3vw, 27px); font-weight: 800; line-height: 1.22; }
          .bubble::after { content: ""; display: block; position: absolute; width: 15px; height: 15px; background: #fffdf3; border-right: 3px solid #142c38; border-bottom: 3px solid #142c38; transform: translateX(-50%) rotate(45deg); left: 50%; bottom: -10px; }
          .pet { width: min(288px, 65vw, 42vh); height: auto; margin-top: 16px; padding: 0; border: 0; background: transparent; cursor: grab; pointer-events: auto; touch-action: none; -webkit-tap-highlight-color: transparent; }
          .pet:active { cursor: grabbing; }
          .pet:focus-visible { outline: 4px solid #ffb83f; outline-offset: 3px; border-radius: 30px; }
          svg { width: 100%; height: auto; overflow: visible; filter: drop-shadow(0 10px 2px #142c3833); }
          .leg-left, .leg-right, .arm-left, .arm-right, .body { transform-box: fill-box; transform-origin: center top; transition: transform 100ms steps(2); }
          [data-frame="0"] .leg-left, [data-frame="4"] .leg-left { transform: rotate(12deg); }
          [data-frame="0"] .leg-right, [data-frame="4"] .leg-right { transform: rotate(-12deg); }
          [data-frame="1"] .leg-left, [data-frame="3"] .leg-left { transform: rotate(-13deg); }
          [data-frame="1"] .leg-right, [data-frame="3"] .leg-right { transform: rotate(13deg); }
          [data-frame="1"] .arm-left, [data-frame="3"] .arm-left { transform: rotate(20deg); }
          [data-frame="1"] .arm-right, [data-frame="3"] .arm-right { transform: rotate(-20deg); }
          [data-frame="2"] .body { transform: translateY(-6px); }
          [data-frame="2"] .arm-left { transform: rotate(-20deg); }
          [data-frame="2"] .arm-right { transform: rotate(20deg); }
          @media (prefers-reduced-motion: reduce) { .leg-left, .leg-right, .arm-left, .arm-right, .body { transition: none; } }
        </style>
        <div class="panel" hidden>
          <div class="bubble" role="status" aria-live="polite"></div>
          <button class="pet" type="button" data-frame="0" aria-label="Thú cưng nhắc vận động">
            <svg viewBox="0 0 192 208" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
              <ellipse cx="96" cy="197" rx="54" ry="7" fill="#142c38" opacity=".18"/>
              <g class="leg-left"><path d="M72 161 Q55 170 53 189 Q52 198 66 198 L83 198 Q88 196 83 189 L84 169Z" fill="#158d8a" stroke="#142c38" stroke-width="5" stroke-linejoin="round"/></g>
              <g class="leg-right"><path d="M108 169 L109 189 Q105 198 112 198 L129 198 Q142 198 139 189 Q136 171 120 161Z" fill="#158d8a" stroke="#142c38" stroke-width="5" stroke-linejoin="round"/></g>
              <g class="arm-left"><path d="M55 106 Q32 113 26 139 Q23 150 35 153 Q46 154 52 143 L67 124Z" fill="#20b8a8" stroke="#142c38" stroke-width="5" stroke-linejoin="round"/></g>
              <g class="arm-right"><path d="M137 106 Q160 113 166 139 Q169 150 157 153 Q146 154 140 143 L125 124Z" fill="#20b8a8" stroke="#142c38" stroke-width="5" stroke-linejoin="round"/></g>
              <g class="body">
                <path d="M96 21 C130 21 153 47 153 87 L150 139 Q148 176 96 180 Q44 176 42 139 L39 87 C39 47 62 21 96 21Z" fill="#20b8a8" stroke="#142c38" stroke-width="5"/>
                <path d="M70 27 Q74 6 96 7 Q118 6 122 27" fill="none" stroke="#142c38" stroke-width="5" stroke-linecap="round"/>
                <circle cx="96" cy="10" r="8" fill="#ffb83f" stroke="#142c38" stroke-width="4"/>
                <path d="M55 70 Q57 48 96 46 Q135 48 137 70 L137 105 Q135 133 96 136 Q57 133 55 105Z" fill="#fffdf3" stroke="#142c38" stroke-width="5"/>
                <ellipse cx="75" cy="87" rx="7" ry="10" fill="#142c38"/>
                <ellipse cx="117" cy="87" rx="7" ry="10" fill="#142c38"/>
                <circle cx="78" cy="83" r="2.5" fill="white"/>
                <circle cx="120" cy="83" r="2.5" fill="white"/>
                <ellipse cx="62" cy="105" rx="8" ry="4" fill="#ff9f98" opacity=".8"/>
                <ellipse cx="130" cy="105" rx="8" ry="4" fill="#ff9f98" opacity=".8"/>
                <path d="M87 108 Q96 119 105 108" fill="none" stroke="#142c38" stroke-width="4" stroke-linecap="round"/>
                <path d="M83 153 Q96 161 109 153" fill="none" stroke="#fffdf3" stroke-width="5" stroke-linecap="round"/>
              </g>
            </svg>
          </button>
        </div>`;
      this._panel = this.shadowRoot.querySelector(".panel");
      this._message = this.shadowRoot.querySelector(".bubble");
      this._pet = this.shadowRoot.querySelector(".pet");
      this._pet.addEventListener("pointerdown", (event) => {
        if (event.button !== 0) return;
        this._drag = {
          x: event.clientX, y: event.clientY,
          left: this._panel.getBoundingClientRect().left,
          top: this._panel.getBoundingClientRect().top,
          moved: false
        };
        this._pet.setPointerCapture(event.pointerId);
      });
      this._pet.addEventListener("pointermove", (event) => {
        if (!this._drag) return;
        const dx = event.clientX - this._drag.x;
        const dy = event.clientY - this._drag.y;
        if (!this._drag.moved && Math.abs(dx) + Math.abs(dy) <= 4) return;
        this._drag.moved = true;
        this._autoMove = false;
        const rect = this._panel.getBoundingClientRect();
        this._panel.style.left = `${Math.min(Math.max(this._drag.left + dx, 0), Math.max(0, window.innerWidth - rect.width))}px`;
        this._panel.style.top = `${Math.min(Math.max(this._drag.top + dy, 0), Math.max(0, window.innerHeight - rect.height))}px`;
        this._panel.style.bottom = "auto";
      });
      this._pet.addEventListener("pointerup", (event) => {
        if (!this._drag) return;
        const moved = this._drag.moved;
        this._drag = null;
        if (this._pet.hasPointerCapture(event.pointerId)) this._pet.releasePointerCapture(event.pointerId);
        if (!moved) this._countClick();
      });
      this._pet.addEventListener("pointercancel", () => { this._drag = null; });
      this._pet.addEventListener("click", (event) => {
        if (event.detail === 0) this._countClick();
      });
    }
  }

  if (!customElements.get("codex-pet")) customElements.define("codex-pet", CodexPet);

  const script = document.currentScript;
  if (script && !script.hasAttribute("data-manual")) {
    const mount = () => {
      if (document.querySelector("codex-pet")) return;
      const pet = document.createElement("codex-pet");
      for (const attribute of ["interval-seconds", "duration-seconds", "message", "start-immediately"]) {
        if (script.hasAttribute(`data-${attribute}`)) pet.setAttribute(attribute, script.getAttribute(`data-${attribute}`) || "");
      }
      document.body.appendChild(pet);
    };
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", mount, { once: true });
    else mount();
  }
})();
