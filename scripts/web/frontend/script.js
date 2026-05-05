const SEARCH_ENGINES = {
  google: "https://www.google.com/search?q=",
  duckduckgo: "https://duckduckgo.com/?q=",
  bing: "https://www.bing.com/search?q=",
  brave: "https://search.brave.com/search?q="
};

const STORAGE_KEYS = {
  engine: "saadbrowser:engine",
  theme: "saadbrowser:theme"
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
