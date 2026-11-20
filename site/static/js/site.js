// Vanilla / Combat Extended switch, remembered across pages, and the mobile menu.
(function () {
  var root = document.documentElement;

  function setMode(mode) {
    if (mode !== "vanilla" && mode !== "ce") return;
    root.setAttribute("data-mode", mode);
    try { localStorage.setItem("bor-mode", mode); } catch (e) { /* private mode: the switch still works for this page */ }
    var buttons = document.querySelectorAll("[data-set-mode]");
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].setAttribute("aria-pressed", String(buttons[i].getAttribute("data-set-mode") === mode));
    }
  }

  var switches = document.querySelectorAll("[data-set-mode]");
  for (var i = 0; i < switches.length; i++) {
    switches[i].addEventListener("click", function () { setMode(this.getAttribute("data-set-mode")); });
  }

  // The inline script in <head> already chose the mode (from ?mode= or the last choice).
  setMode(root.getAttribute("data-mode") === "ce" ? "ce" : "vanilla");

  var toggle = document.querySelector(".menu-toggle");
  var nav = document.getElementById("topnav");
  if (toggle && nav) {
    toggle.addEventListener("click", function () {
      var open = toggle.getAttribute("aria-expanded") === "true";
      toggle.setAttribute("aria-expanded", String(!open));
      nav.classList.toggle("open", !open);
    });
  }

  // Following a link into a collapsed spoiler opens it.
  function openTarget() {
    if (!location.hash) return;
    var el = document.getElementById(decodeURIComponent(location.hash.slice(1)));
    while (el) {
      if (el.tagName === "DETAILS") el.open = true;
      el = el.parentElement;
    }
  }
  window.addEventListener("hashchange", openTarget);
  openTarget();
})();
