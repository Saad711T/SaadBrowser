/* SaadBrowser anti-fingerprint shim. Loaded into every page before site
   scripts run. Goal: reduce uniqueness reported by amiunique.org and similar
   trackers without breaking common sites. */
(function () {
  'use strict';

  function safeDefine(obj, prop, value) {
    try {
      Object.defineProperty(obj, prop, {
        get: function () { return value; },
        configurable: true
      });
    } catch (e) { /* property may be non-configurable on some platforms */ }
  }

  // Blend in with the most common configuration we can plausibly claim.
  safeDefine(navigator, 'hardwareConcurrency', 8);
  safeDefine(navigator, 'deviceMemory', 8);
  safeDefine(navigator, 'doNotTrack', '1');
  safeDefine(navigator, 'webdriver', false);
  safeDefine(navigator, 'languages', Object.freeze(['en-US', 'en']));

  // Hide plugin/mime fingerprint surface (parity with privacy-focused browsers).
  try {
    safeDefine(navigator, 'plugins', Object.freeze([]));
    safeDefine(navigator, 'mimeTypes', Object.freeze([]));
  } catch (e) {}

  // Canvas: introduce per-pixel noise so toDataURL / getImageData are unstable.
  try {
    var origToDataURL = HTMLCanvasElement.prototype.toDataURL;
    HTMLCanvasElement.prototype.toDataURL = function () {
      try {
        var ctx = this.getContext('2d');
        if (ctx && this.width > 0 && this.height > 0) {
          var img = ctx.getImageData(0, 0, this.width, this.height);
          for (var i = 0; i < img.data.length; i += 4) {
            if (Math.random() < 0.003) img.data[i] ^= 1;
          }
          ctx.putImageData(img, 0, 0);
        }
      } catch (e) {}
      return origToDataURL.apply(this, arguments);
    };
  } catch (e) {}

  // WebGL: report a generic vendor / renderer string to defeat GPU fingerprints.
  try {
    var spoof = function (orig) {
      return function (p) {
        if (p === 37445 /* UNMASKED_VENDOR_WEBGL */) return 'Intel Inc.';
        if (p === 37446 /* UNMASKED_RENDERER_WEBGL */) return 'Intel Iris OpenGL Engine';
        return orig.call(this, p);
      };
    };
    if (window.WebGLRenderingContext) {
      WebGLRenderingContext.prototype.getParameter =
        spoof(WebGLRenderingContext.prototype.getParameter);
    }
    if (window.WebGL2RenderingContext) {
      WebGL2RenderingContext.prototype.getParameter =
        spoof(WebGL2RenderingContext.prototype.getParameter);
    }
  } catch (e) {}

  // AudioContext: tiny per-sample jitter to break audio fingerprinting.
  try {
    if (window.AudioBuffer) {
      var origGetChannelData = AudioBuffer.prototype.getChannelData;
      AudioBuffer.prototype.getChannelData = function () {
        var data = origGetChannelData.apply(this, arguments);
        for (var i = 0; i < data.length; i += 100) {
          data[i] = data[i] + (Math.random() - 0.5) * 1e-7;
        }
        return data;
      };
    }
  } catch (e) {}
})();
