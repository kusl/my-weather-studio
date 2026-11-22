(function (win, doc) {
  "use strict";

  var root = doc.documentElement;
  var scripts = doc.getElementsByTagName("script");
  var self = doc.currentScript || scripts[scripts.length - 1];
  var blazorSource = self ? self.getAttribute("data-blazor-src") : null;

  var messages = {
    unsupported: {
      title: "This browser can't run Weather",
      lead: "Weather is built for current browsers and yours is missing a few things it relies on."
    },
    failed: {
      title: "Weather couldn't start",
      lead: "The page arrived but the app didn't start. Check the connection and reload. If it keeps happening, copy the details below and share them."
    }
  };

  function syntax(name, source) {
    return {
      name: name,
      test: function () {
        return typeof new Function(source) === "function";
      }
    };
  }

  function feature(name, test) {
    return { name: name, test: test };
  }

  function cssSupports(name, condition) {
    return feature(name, function () {
      return !!(win.CSS && isFunction(win.CSS.supports) && win.CSS.supports(condition));
    });
  }

  function cssNesting() {
    var style = doc.createElement("style");
    style.appendChild(doc.createTextNode(".gate-probe{.gate-probe-child{color:red}}"));
    doc.getElementsByTagName("head")[0].appendChild(style);
    try {
      var rule = style.sheet && style.sheet.cssRules && style.sheet.cssRules[0];
      return !!(rule && rule.cssRules && rule.cssRules.length === 1);
    } finally {
      style.parentNode.removeChild(style);
    }
  }

  function isFunction(value) {
    return typeof value === "function";
  }

  var checks = [
    syntax("JavaScript optional chaining and nullish coalescing (ES2020)", "var o = {}; return o?.a ?? 1;"),
    syntax("JavaScript class fields and private members (ES2022)", "class A { #x = 1; static y = 2; get x() { return this.#x; } } return new A().x;"),
    syntax("JavaScript async iteration (ES2018)", "return async function* () { for await (const x of []) { yield x; } };"),
    feature("JavaScript modules", function () { return "noModule" in doc.createElement("script"); }),
    feature("Promise.withResolvers", function () { return isFunction(win.Promise) && isFunction(win.Promise.withResolvers); }),
    feature("Object.groupBy", function () { return isFunction(Object.groupBy); }),
    feature("Array.prototype.findLast", function () { return isFunction(Array.prototype.findLast); }),
    feature("structuredClone", function () { return isFunction(win.structuredClone); }),
    feature("fetch and AbortController", function () { return isFunction(win.fetch) && isFunction(win.AbortController); }),
    feature("WebSocket", function () { return isFunction(win.WebSocket) || typeof win.WebSocket === "object"; }),
    feature("TextEncoder and TextDecoder", function () { return isFunction(win.TextEncoder) && isFunction(win.TextDecoder); }),
    feature("HTML dialog element", function () { return isFunction(win.HTMLDialogElement); }),
    feature("CSS nesting", cssNesting),
    feature("CSS cascade layers", function () { return isFunction(win.CSSLayerBlockRule); }),
    cssSupports("CSS :has() selector", "selector(:has(a))"),
    cssSupports("CSS container queries", "(container-type: inline-size)"),
    cssSupports("CSS color-mix()", "(color: color-mix(in srgb, red 50%, blue))"),
    cssSupports("CSS dynamic viewport units", "(height: 100dvh)")
  ];

  function failingChecks() {
    var failed = [];
    for (var i = 0; i < checks.length; i++) {
      var passed = false;
      try {
        passed = checks[i].test() === true;
      } catch (e) {
        passed = false;
      }
      if (!passed) {
        failed.push(checks[i].name);
      }
    }
    return failed;
  }

  function setText(element, text) {
    if (!element) {
      return;
    }
    if ("textContent" in element) {
      element.textContent = text;
    } else {
      element.innerText = text;
    }
  }

  function byId(id) {
    return doc.getElementById(id);
  }

  function now() {
    var date = new Date();
    return isFunction(date.toISOString) ? date.toISOString() : String(date);
  }

  function diagnostics(kind, details) {
    var nav = win.navigator || {};
    var screenInfo = win.screen ? win.screen.width + "x" + win.screen.height : "unknown";
    var lines = [
      "Weather browser diagnostics",
      "Result: " + kind,
      "Problems: " + (details.length ? details.join("; ") : "none"),
      "Page: " + win.location.href,
      "Time: " + now(),
      "User agent: " + (nav.userAgent || "unknown"),
      "Platform: " + (nav.platform || "unknown"),
      "Language: " + (nav.language || nav.userLanguage || "unknown"),
      "Viewport: " + (win.innerWidth || root.clientWidth) + "x" + (win.innerHeight || root.clientHeight),
      "Screen: " + screenInfo + " @" + (win.devicePixelRatio || 1) + "x",
      "Secure context: " + (win.isSecureContext === undefined ? "unknown" : win.isSecureContext),
      "Cookies enabled: " + (nav.cookieEnabled === undefined ? "unknown" : nav.cookieEnabled),
      "Online: " + (nav.onLine === undefined ? "unknown" : nav.onLine)
    ];
    return lines.join("\n");
  }

  function copyDiagnostics(textarea, status) {
    function selected() {
      textarea.focus();
      textarea.select();
      var copied = false;
      try {
        copied = doc.execCommand("copy");
      } catch (e) {
        copied = false;
      }
      setText(status, copied ? "Copied." : "Selected. Copy it with your device's copy command.");
    }

    var clipboard = win.navigator && win.navigator.clipboard;
    if (clipboard && isFunction(clipboard.writeText) && win.isSecureContext) {
      clipboard.writeText(textarea.value).then(function () {
        setText(status, "Copied.");
      }, selected);
    } else {
      selected();
    }
  }

  function show(kind, details) {
    if (/(^|\s)legacy(\s|$)/.test(root.className)) {
      return;
    }
    root.className += " legacy";

    var message = messages[kind];
    setText(byId("legacy-gate-title"), message.title);
    setText(byId("legacy-gate-lead"), message.lead);

    var list = byId("legacy-gate-missing");
    if (list) {
      for (var i = 0; i < details.length; i++) {
        var item = doc.createElement("li");
        setText(item, details[i]);
        list.appendChild(item);
      }
    }

    var textarea = byId("legacy-gate-diagnostics");
    var status = byId("legacy-gate-status");
    if (textarea) {
      textarea.value = diagnostics(kind, details);
    }

    var copy = byId("legacy-gate-copy");
    if (copy && textarea) {
      copy.onclick = function () {
        copyDiagnostics(textarea, status);
      };
    }

    var reload = byId("legacy-gate-reload");
    if (reload) {
      reload.onclick = function () {
        win.location.reload();
      };
    }
  }

  function describe(error) {
    if (!error) {
      return "Unknown startup error";
    }
    return String(error.message || error);
  }

  function startBlazor() {
    if (!blazorSource) {
      show("failed", ["The page is missing its application script reference"]);
      return;
    }

    var script = doc.createElement("script");
    script.src = blazorSource;
    script.setAttribute("autostart", "false");
    script.onload = function () {
      try {
        win.Blazor.start()["catch"](function (error) {
          show("failed", ["Startup error: " + describe(error)]);
        });
      } catch (error) {
        show("failed", ["Startup error: " + describe(error)]);
      }
    };
    script.onerror = function () {
      show("failed", ["Could not download " + blazorSource]);
    };
    doc.body.appendChild(script);
  }

  var missing = failingChecks();
  if (missing.length) {
    show("unsupported", missing);
  } else {
    startBlazor();
  }
})(window, document);
