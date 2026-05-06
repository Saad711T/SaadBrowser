const SEARCH_ENGINES = {
  google: "https://www.google.com/search?q=",
  duckduckgo: "https://duckduckgo.com/?q=",
  bing: "https://www.bing.com/search?q=",
  brave: "https://search.brave.com/search?q="
};

const STORAGE_KEYS = {
  engine: "saadbrowser:engine",
  theme: "saadbrowser:theme",
  social: "saadbrowser:social-embeds"
};

function looksLikeUrl(text) {
  if (/^https?:\/\//i.test(text)) return true;
  if (text.includes(" ")) return false;
  // domain-like: contains a dot, no spaces, has a TLD-like trailing segment
  return /^[^\s]+\.[^\s]{2,}/.test(text);
}

function buildTargetUrl(text, engineKey) {
  const trimmed = text.trim();
  if (!trimmed) return null;
  if (looksLikeUrl(trimmed)) {
    return /^https?:\/\//i.test(trimmed) ? trimmed : "https://" + trimmed;
  }
  const base = SEARCH_ENGINES[engineKey] || SEARCH_ENGINES.google;
  return base + encodeURIComponent(trimmed);
}

const searchForm = document.getElementById("search-form");
const searchInput = document.getElementById("search-input");
const engineSelect = document.getElementById("search-engine");

const savedEngine = localStorage.getItem(STORAGE_KEYS.engine);
if (savedEngine && SEARCH_ENGINES[savedEngine]) {
  engineSelect.value = savedEngine;
}

engineSelect.addEventListener("change", () => {
  localStorage.setItem(STORAGE_KEYS.engine, engineSelect.value);
});

searchForm.addEventListener("submit", (event) => {
  event.preventDefault();
  const target = buildTargetUrl(searchInput.value, engineSelect.value);
  if (target) {
    window.location.href = target;
  }
});

document.querySelectorAll(".tab-button").forEach((button) => {
  if (button.id === "load-tweet") return;
  button.addEventListener("click", () => {
    const url = button.getAttribute("data-url");
    if (url) window.location.href = url;
  });
});

const themeToggle = document.getElementById("theme-toggle");
const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
const initialTheme = localStorage.getItem(STORAGE_KEYS.theme) || (prefersDark ? "dark" : "light");

function applyTheme(theme) {
  document.documentElement.dataset.theme = theme;
  themeToggle.textContent = theme === "dark" ? "☀" : "🌙";
}

applyTheme(initialTheme);

themeToggle.addEventListener("click", () => {
  const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
  applyTheme(next);
  localStorage.setItem(STORAGE_KEYS.theme, next);
});

// Click-to-load social embed: don't reach out to platform.twitter.com (and the
// tracking pixels it pulls in) until the user explicitly asks.
const loadTweetBtn = document.getElementById("load-tweet");
const tweetContainer = document.getElementById("tweet-container");

const TWEET_HTML = '<blockquote class="twitter-tweet"><p lang="ar" dir="rtl">سلام عليكم ورحمة الله وبركاته<br><br>احصائية بسيطة عن أنشط حسابات قيتهب في السعودية لكل مدينة<br><br>حسبت : الريبوز والمشاركات العامة وأستثنيت الخاصة وعمليات الأتمتة<br><br>للمعلومية قد تكون صحيحة أو خاطئة ولكنها مبدئية جداً<br>وفيه مدن تحتاج لتدقيق مثل : جازان-نجران-الطائف-تبوك<br>بل وحائل تحتاج لبحث . <a href="https://t.co/Ry30D4zHBC">pic.twitter.com/Ry30D4zHBC</a></p>&mdash; 0xSaad 🇸🇦 (@0xdonzDev) <a href="https://twitter.com/0xdonzDev/status/2044347146929881160?ref_src=twsrc%5Etfw">April 15, 2026</a></blockquote>';

function loadTweetEmbed() {
  if (!loadTweetBtn || !tweetContainer) return;
  tweetContainer.innerHTML = TWEET_HTML;
  tweetContainer.hidden = false;
  const widgets = document.createElement("script");
  widgets.async = true;
  widgets.src = "https://platform.twitter.com/widgets.js";
  widgets.charset = "utf-8";
  document.body.appendChild(widgets);
  loadTweetBtn.remove();
}

if (loadTweetBtn) {
  loadTweetBtn.addEventListener("click", () => {
    localStorage.setItem(STORAGE_KEYS.social, "1");
    loadTweetEmbed();
  });

  // Auto-load only if the user opted in previously.
  if (localStorage.getItem(STORAGE_KEYS.social) === "1") {
    loadTweetEmbed();
  }
}
