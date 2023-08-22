export function receiveHotReload() {
  return BINDING.js_to_mono_obj(new Promise((resolve) => receiveHotReloadAsync().then(resolve(0))));
}

export async function receiveHotReloadAsync() {
  const cacheKey = 'blazor-webassembly-cache';
  const cacheJson = window.sessionStorage.getItem(cacheKey);
  const cache = cacheJson ? JSON.parse(cacheJson) : {};

  let headers;
  let deltas;

  if (cache.etag) {
    headers = { 'if-none-match' : cache.etag };
  }

  const response = await fetch('/_framework/blazor-hotreload', { headers });
  if (response.status === 200) {
    deltas = await response.json();
  } else if (response.status === 304) {
    deltas = cache.deltas;
  }

  if (deltas) {
    try {
      deltas.forEach(d => window.Blazor._internal.applyHotReload(d.moduleId, d.metadataDelta, d.ilDelta));
    } catch (error) {
      console.warn(error);
    }
  }
}
