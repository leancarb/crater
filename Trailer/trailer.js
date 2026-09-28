/*
 * CRATER — trailer
 * Motor 3D minimo en canvas 2D (poligonos + painter's algorithm + luces spot),
 * sonido sintetizado con WebAudio. Sin dependencias.
 *
 * Las mecanicas que se muestran salen de Assets/Scripts:
 *   LinternaController  -> haz con cono, alcance y color por filtro, demora de cambio 0.8 s
 *   Ancla + PuenteLuz   -> CUERPO: dos anclas encendidas sostienen un puente
 *   MateriaHueca        -> HUECO: la reja baja a 15 % de opacidad y se atraviesa
 *   RastroFosforescente -> RASTRO: brilla 8 s despues de apagar, curva pow(t, 0.45)
 *   AdaptacionOscuridad -> con la linterna apagada sube la exposicion, al encender se pierde
 *   Lente               -> alineacion ojo -> diafragma -> lente, sostenida 2.2 s
 */
(() => {
'use strict';

// ------------------------------------------------------------------ util
const V = (x, y, z) => ({ x, y, z });
const vadd = (a, b) => V(a.x + b.x, a.y + b.y, a.z + b.z);
const vsub = (a, b) => V(a.x - b.x, a.y - b.y, a.z - b.z);
const vmul = (a, s) => V(a.x * s, a.y * s, a.z * s);
const vcross = (a, b) => V(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
const vlen = a => Math.hypot(a.x, a.y, a.z);
const vnorm = a => { const l = vlen(a) || 1; return V(a.x / l, a.y / l, a.z / l); };
const clamp = (x, a = 0, b = 1) => Math.min(b, Math.max(a, x));
const lerp = (a, b, t) => a + (b - a) * t;
const smooth = (a, b, x) => { const t = clamp((x - a) / (b - a)); return t * t * (3 - 2 * t); };
const ease = t => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);
const hash = (a, b, c) => { const h = Math.sin(a * 127.1 + b * 311.7 + c * 74.7) * 43758.5453; return h - Math.floor(h); };
const disp = p => Math.sin(p.x * 1.3 + p.z * 0.7) * Math.sin(p.y * 1.9 + p.z * 1.1)
               + 0.5 * Math.sin(p.x * 2.9 - p.y * 2.3 + p.z * 1.7)
               + 0.25 * Math.sin(p.z * 4.1 + p.x * 3.7);
const REDUCED = window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches;
const MOTION = REDUCED ? 0.25 : 1;
const hh = t => MOTION * (Math.sin(t * 1.31) * 0.5 + Math.sin(t * 2.17 + 1.3) * 0.3 + Math.sin(t * 4.03 + 2.1) * 0.2);
function key(t, k) {
  if (t <= k[0][0]) return k[0][1];
  for (let i = 0; i < k.length - 1; i++) {
    const a = k[i], b = k[i + 1];
    if (t <= b[0]) { const u = (t - a[0]) / (b[0] - a[0]); return a[1] + (b[1] - a[1]) * u * u * (3 - 2 * u); }
  }
  return k[k.length - 1][1];
}
function rng(seed) { // mulberry32
  return () => { seed |= 0; seed = seed + 0x6D2B79F5 | 0; let t = Math.imul(seed ^ seed >>> 15, 1 | seed);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; };
}

// ------------------------------------------------------------------ paleta (de los assets del juego)
const AMBER = [0.91, 0.63, 0.29];          // Filtro_Cuerpo / Ancla colorEncendida
const BLUE  = [0.29, 0.357, 0.91];         // Filtro_Hueco
const BONE  = [0.93, 0.918, 0.886];        // Filtro_Rastro / colorDelRastro
const OCULO = [0.788, 0.443, 0.29];        // Mat_Oculo emission
const WARM  = [1, 0.96, 0.88];             // LinternaController colorBase
const ASH   = [0.30, 0.27, 0.24];
const ROCK  = [0.21, 0.195, 0.185];
const ANCLA_OFF = [0.12, 0.11, 0.10];

const FILTROS = {
  none:   { col: WARM,  ang: 28, range: 14, int: 5.0 },
  cuerpo: { col: AMBER, ang: 45, range: 14, int: 5.5 },
  hueco:  { col: BLUE,  ang: 55, range: 30, int: 8.0 },
  rastro: { col: BONE,  ang: 32, range: 14, int: 5.0 },
};
const DEMORA_CAMBIO = 0.8;

const DISPLAY = '"Italiana", "Didot", "Bodoni 72", serif';
const SERIF = '"Instrument Serif", "Iowan Old Style", Georgia, serif';
const MONO = '"IBM Plex Mono", ui-monospace, "SF Mono", Menlo, monospace';

// ------------------------------------------------------------------ canvas
const W = 768, H = 432, NEAR = 0.06;
const lo = document.createElement('canvas'); lo.width = W; lo.height = H;
const g = lo.getContext('2d');
const cv = document.getElementById('trailer');
const m = cv.getContext('2d');
let CW = 0, CH = 0, DPR = 1;
const VR = { x: 0, y: 0, w: 0, h: 0, bar: 0 };
const tA = document.createElement('canvas'), tB = document.createElement('canvas');

function resize() {
  DPR = Math.min(2, window.devicePixelRatio || 1);
  CW = cv.width = Math.round(cv.clientWidth * DPR);
  CH = cv.height = Math.round(cv.clientHeight * DPR);
  tA.width = tB.width = CW; tA.height = tB.height = CH;
  let w = CW, h = w * 9 / 16;
  if (h > CH) { h = CH; w = h * 16 / 9; }
  VR.w = w; VR.h = h; VR.x = (CW - w) / 2; VR.y = (CH - h) / 2;
  VR.bar = Math.max(0, (h - w / 2.2) / 2);
  if (!playing) drawFrame(T);
}

// grano de pelicula
const GRAIN = [];
for (let k = 0; k < 4; k++) {
  const c = document.createElement('canvas'); c.width = c.height = 192;
  const x = c.getContext('2d'), d = x.createImageData(192, 192);
  for (let i = 0; i < d.data.length; i += 4) { const v = Math.random() * 255; d.data[i] = d.data[i + 1] = d.data[i + 2] = v; d.data[i + 3] = 255; }
  x.putImageData(d, 0, 0); GRAIN.push(c);
}

// ------------------------------------------------------------------ geometria
function quad(Q, p0, p1, p2, p3, o) {
  const c = V((p0.x + p1.x + p2.x + p3.x) / 4, (p0.y + p1.y + p2.y + p3.y) / 4, (p0.z + p1.z + p2.z + p3.z) / 4);
  const n = vnorm(vcross(vsub(p2, p0), vsub(p3, p1)));
  const j = o.jit ?? 0.28, k = 1 - j + 2 * j * hash(c.x, c.y, c.z);
  const q = { p: [p0, p1, p2, p3], c, n, alb: [o.alb[0] * k, o.alb[1] * k, o.alb[2] * k],
              em: o.em ? o.em.slice() : [0, 0, 0], a: o.a ?? 1, upd: o.upd, i: o.i, off: null };
  Q.push(q); return q;
}
function subdivide(prof, tile) {
  const pts = [];
  for (let i = 0; i < prof.length - 1; i++) {
    const a = prof[i], b = prof[i + 1], n = Math.max(1, Math.round(Math.hypot(b[0] - a[0], b[1] - a[1]) / tile));
    for (let k = 0; k < n; k++) pts.push([lerp(a[0], b[0], k / n), lerp(a[1], b[1], k / n)]);
  }
  pts.push(prof[prof.length - 1]);
  return pts.map((p, i) => {
    const pr = pts[Math.max(0, i - 1)], nx = pts[Math.min(pts.length - 1, i + 1)];
    let a = -(nx[1] - pr[1]), b = nx[0] - pr[0]; const l = Math.hypot(a, b) || 1;
    return [p[0], p[1], a / l, b / l];
  });
}
// perfil (x,y) extruido a lo largo de z
function extrude(Q, prof, z0, z1, o) {
  const pts = subdivide(prof, o.tile), nz = Math.max(1, Math.round((z1 - z0) / o.tile)), amp = o.amp ?? 0.18;
  const G = [];
  for (let k = 0; k <= nz; k++) {
    const z = z0 + (z1 - z0) * k / nz, row = [];
    for (const [x, y, nx, ny] of pts) {
      const d = disp(V(x, y, z)) * amp * (1 - 0.75 * Math.abs(ny));
      row.push(V(x + nx * d, y + ny * d, z));
    }
    G.push(row);
  }
  for (let k = 0; k < nz; k++) for (let i = 0; i < pts.length - 1; i++)
    quad(Q, G[k][i], G[k][i + 1], G[k + 1][i + 1], G[k + 1][i], o);
}
// perfil (r,y) revolucionado sobre el eje y
function revolve(Q, prof, segs, o) {
  const pts = subdivide(prof, o.tile), amp = o.amp ?? 0.3, G = [];
  for (let k = 0; k < segs; k++) {
    const a = k / segs * Math.PI * 2, ca = Math.cos(a), sa = Math.sin(a), row = [];
    for (const [r, y, nr, ny] of pts) {
      const p = V(r * ca, y, r * sa), d = disp(p) * amp * (1 - 0.7 * Math.abs(ny));
      row.push(V(p.x + nr * ca * d, y + ny * d, p.z + nr * sa * d));
    }
    G.push(row);
  }
  for (let k = 0; k < segs; k++) { const k2 = (k + 1) % segs;
    for (let i = 0; i < pts.length - 1; i++) quad(Q, G[k][i], G[k][i + 1], G[k2][i + 1], G[k2][i], o); }
}
// grilla plana desplazada por la normal (bordes fijos, sin grietas con vecinos)
function grid(Q, o0, u, v, nu, nv, o) {
  const n = vnorm(vcross(u, v)), amp = o.amp ?? 0, G = [];
  for (let i = 0; i <= nu; i++) { const row = [];
    for (let j = 0; j <= nv; j++) {
      let p = vadd(o0, vadd(vmul(u, i / nu), vmul(v, j / nv)));
      if (amp && i > 0 && j > 0 && i < nu && j < nv) p = vadd(p, vmul(n, disp(p) * amp));
      row.push(p);
    }
    G.push(row);
  }
  const out = [];
  for (let i = 0; i < nu; i++) for (let j = 0; j < nv; j++)
    out.push(quad(Q, G[i][j], G[i + 1][j], G[i + 1][j + 1], G[i][j + 1], o));
  return out;
}
function box(Q, cx, cy, cz, sx, sy, sz, o) {
  const x0 = cx - sx / 2, x1 = cx + sx / 2, y0 = cy - sy / 2, y1 = cy + sy / 2, z0 = cz - sz / 2, z1 = cz + sz / 2;
  const out = [];
  out.push(quad(Q, V(x0, y0, z0), V(x1, y0, z0), V(x1, y1, z0), V(x0, y1, z0), o));
  out.push(quad(Q, V(x1, y0, z1), V(x0, y0, z1), V(x0, y1, z1), V(x1, y1, z1), o));
  out.push(quad(Q, V(x0, y0, z1), V(x0, y0, z0), V(x0, y1, z0), V(x0, y1, z1), o));
  out.push(quad(Q, V(x1, y0, z0), V(x1, y0, z1), V(x1, y1, z1), V(x1, y1, z0), o));
  out.push(quad(Q, V(x0, y1, z0), V(x1, y1, z0), V(x1, y1, z1), V(x0, y1, z1), o));
  return out;
}

// ------------------------------------------------------------------ camara y proyeccion
const cam = { p: V(0, 1.6, 0), yaw: 0, pitch: 0, roll: 0, fov: 1.05 };
let CT = null;
function setCam() {
  CT = { cy: Math.cos(cam.yaw), sy: Math.sin(cam.yaw), cp: Math.cos(cam.pitch), sp: Math.sin(cam.pitch),
         cr: Math.cos(cam.roll), sr: Math.sin(cam.roll), f: (H / 2) / Math.tan(cam.fov / 2) };
}
const fwdOf = (yaw, pitch) => V(Math.sin(yaw) * Math.cos(pitch), Math.sin(pitch), Math.cos(yaw) * Math.cos(pitch));
function toCam(p, off) {
  let dx = p.x - cam.p.x, dy = p.y - cam.p.y, dz = p.z - cam.p.z;
  if (off) { dx += off.x; dy += off.y; dz += off.z; }
  const x = dx * CT.cy - dz * CT.sy, z1 = dx * CT.sy + dz * CT.cy;
  return [x, dy * CT.cp - z1 * CT.sp, dy * CT.sp + z1 * CT.cp];
}
function toScr(x, y, z) {
  const sx = x / z * CT.f, sy = -y / z * CT.f;
  return [W / 2 + sx * CT.cr - sy * CT.sr, H / 2 + sx * CT.sr + sy * CT.cr];
}
function proj(p) { const c = toCam(p); if (c[2] < NEAR) return null; const s = toScr(c[0], c[1], c[2]); return [s[0], s[1], c[2]]; }
function clipNear(cs) {
  const out = [];
  for (let i = 0; i < cs.length; i++) {
    const a = cs[i], b = cs[(i + 1) % cs.length], ai = a[2] >= NEAR, bi = b[2] >= NEAR;
    if (ai) out.push(a);
    if (ai !== bi) { const t = (NEAR - a[2]) / (b[2] - a[2]); out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, NEAR]); }
  }
  return out;
}

// ------------------------------------------------------------------ tono
const BP = 0.003, IG = 1 / 2.2;
function tone(r, gg, b, E, sat) {
  r = 1 - Math.exp(-r * E); gg = 1 - Math.exp(-gg * E); b = 1 - Math.exp(-b * E);
  if (sat < 1) { const l = 0.3 * r + 0.59 * gg + 0.11 * b, u = 1 - sat; r = r * sat + l * u * 0.8; gg = gg * sat + l * u * 0.96; b = b * sat + l * u * 1.25; }
  r = r > BP ? Math.pow(r - BP, IG) : 0; gg = gg > BP ? Math.pow(gg - BP, IG) : 0; b = b > BP ? Math.pow(b - BP, IG) : 0;
  return [Math.min(255, r * 255) | 0, Math.min(255, gg * 255) | 0, Math.min(255, b * 255) | 0];
}
const rgb = c => 'rgb(' + c[0] + ',' + c[1] + ',' + c[2] + ')';
const rgba = (c, a) => 'rgba(' + c[0] + ',' + c[1] + ',' + c[2] + ',' + a.toFixed(3) + ')';

// luz sobre un punto (para quads y particulas)
function lightAt(px, py, pz, n, L, out) {
  for (const l of L) {
    if (l.int <= 0) continue;
    const dx = px - l.p.x, dy = py - l.p.y, dz = pz - l.p.z, d = Math.hypot(dx, dy, dz) || 1e-3;
    if (d > l.range) continue;
    const ix = dx / d, iy = dy / d, iz = dz / d;
    let f = l.int / (1 + l.k * d * d) * (1 - d / l.range);
    if (l.dir) { const ca = ix * l.dir.x + iy * l.dir.y + iz * l.dir.z, lim = l.cosS ?? l.cosO; if (ca < lim) continue;
      f *= smooth(l.cosO, l.cosI, ca) + (l.cosS ? 0.16 * smooth(l.cosS, l.cosO, ca) : 0); }
    if (n) f *= 0.25 + 0.75 * Math.abs(ix * n.x + iy * n.y + iz * n.z);
    out[0] += l.col[0] * f; out[1] += l.col[1] * f; out[2] += l.col[2] * f;
  }
  return out;
}

function renderWorld(Q, L, env) {
  const amb = env.amb, E = env.E ?? 1, sat = env.sat ?? 1, fog = env.fog ?? 0.04;
  const list = [], acc = [0, 0, 0];
  for (const q of Q) {
    if (q.upd) q.upd(q);
    if (q.a < 0.004) continue;
    const off = q.off, cs = [toCam(q.p[0], off), toCam(q.p[1], off), toCam(q.p[2], off), toCam(q.p[3], off)];
    let front = 0; for (const c of cs) if (c[2] >= NEAR) front++;
    if (!front) continue;
    let poly = front < 4 ? clipNear(cs) : cs;
    // descarte lateral grosero
    let l = 0, r = 0, u = 0, dn = 0;
    const sp = poly.map(c => { const s = toScr(c[0], c[1], c[2]); if (s[0] < -40) l++; if (s[0] > W + 40) r++; if (s[1] < -40) u++; if (s[1] > H + 40) dn++; return s; });
    if (l === sp.length || r === sp.length || u === sp.length || dn === sp.length) continue;
    const cx = q.c.x + (off ? off.x : 0), cy = q.c.y + (off ? off.y : 0), cz = q.c.z + (off ? off.z : 0);
    acc[0] = amb[0]; acc[1] = amb[1]; acc[2] = amb[2];
    lightAt(cx, cy, cz, q.n, L, acc);
    const dist = Math.hypot(cx - cam.p.x, cy - cam.p.y, cz - cam.p.z), fk = Math.exp(-dist * fog);
    const col = tone((q.alb[0] * acc[0] + q.em[0]) * fk, (q.alb[1] * acc[1] + q.em[1]) * fk, (q.alb[2] * acc[2] + q.em[2]) * fk, E, sat);
    list.push({ d: (cs[0][2] + cs[1][2] + cs[2][2] + cs[3][2]) / 4, sp, col, a: q.a });
  }
  list.sort((a, b) => b.d - a.d);
  g.lineJoin = 'round';
  for (const it of list) {
    const s = it.sp; g.beginPath(); g.moveTo(s[0][0], s[0][1]);
    for (let i = 1; i < s.length; i++) g.lineTo(s[i][0], s[i][1]);
    g.closePath();
    const c = rgb(it.col);
    g.fillStyle = c;
    if (it.a < 1) { g.globalAlpha = it.a; g.fill(); g.globalAlpha = 1; }
    else { g.strokeStyle = c; g.lineWidth = 0.7; g.fill(); g.stroke(); }
  }
}

// linterna: spot desde un poco abajo y a la derecha del ojo
function flashlight(F, intMul = 1, yawLag = 0, pitchLag = 0) {
  const dir = fwdOf(cam.yaw + yawLag, cam.pitch + pitchLag);
  const right = V(Math.cos(cam.yaw), 0, -Math.sin(cam.yaw));
  const half = F.ang * Math.PI / 360;
  return { p: vadd(vadd(cam.p, vmul(right, 0.22)), V(0, -0.24, 0)), dir, col: F.col, int: F.int * intMul,
           k: 0.12, range: F.range, cosO: Math.cos(half), cosI: Math.cos(half * 0.55), cosS: Math.cos(Math.min(1.4, half * 2.6)), ang: F.ang };
}
// estado de la linterna durante un cambio de filtro (demora 0.8 s)
function beamState(lt, swapT, from, to) {
  const A = FILTROS[from], B = FILTROS[to];
  if (lt < swapT) return { F: A, mul: 1, swapping: false };
  const k = lt - swapT;
  const F = { col: k < 0.12 ? A.col : B.col, ang: lerp(A.ang, B.ang, smooth(0.08, 0.4, k)),
              range: lerp(A.range, B.range, smooth(0.08, 0.5, k)), int: B.int };
  let mul = 1;
  if (k < 0.32) mul = 0.08 + 0.5 * (hash(Math.floor(lt * 50), 3, 1) > 0.45 ? 1 : 0);
  return { F, mul, swapping: k < DEMORA_CAMBIO };
}
function haze(L, strength = 0.07) {
  if (!L || L.int <= 0) return;
  const s = proj(vadd(L.p, vmul(L.dir, 3))); if (!s) return;
  const r = CT.f * Math.tan(L.ang * Math.PI / 360) * 1.25;
  const c = L.col.map(v => v * 255 | 0), gr = g.createRadialGradient(s[0], s[1], 0, s[0], s[1], r);
  const a = strength * Math.min(1.4, L.int / 5);
  gr.addColorStop(0, rgba(c, a)); gr.addColorStop(0.5, rgba(c, a * 0.35)); gr.addColorStop(1, rgba(c, 0));
  g.globalCompositeOperation = 'lighter'; g.fillStyle = gr; g.fillRect(0, 0, W, H); g.globalCompositeOperation = 'source-over';
}
function motes(n, seed, posFn, L, env, gain = 0.5) {
  g.globalCompositeOperation = 'lighter';
  const acc = [0, 0, 0];
  for (let i = 0; i < n; i++) {
    const p = posFn(i, hash(i, seed, 1), hash(i, seed, 2), hash(i, seed, 3));
    acc[0] = acc[1] = acc[2] = 0; lightAt(p.x, p.y, p.z, null, L, acc);
    const s = proj(p); if (!s || s[2] > 25) continue;
    const c = tone(acc[0] * gain, acc[1] * gain, acc[2] * gain, env.E ?? 1, env.sat ?? 1);
    if (c[0] + c[1] + c[2] < 20) continue;
    const sz = clamp(2.2 / s[2], 0.6, 2.4);
    g.fillStyle = rgb(c); g.fillRect(s[0], s[1], sz, sz);
  }
  g.globalCompositeOperation = 'source-over';
}
function polyline(pts, lw, color, alpha) {
  const ps = pts.map(proj); if (ps.some(p => !p)) return;
  g.globalAlpha = alpha; g.strokeStyle = color; g.lineWidth = lw; g.lineCap = 'round'; g.lineJoin = 'round';
  g.beginPath(); g.moveTo(ps[0][0], ps[0][1]); for (let i = 1; i < ps.length; i++) g.lineTo(ps[i][0], ps[i][1]); g.stroke();
  g.globalAlpha = 1;
}

// ------------------------------------------------------------------ estado por frame
const S = { audio: null, grain: 0.05, flash: 0 };
function resetS() { S.audio = { drone: 0.3, wind: 0.1, hum: 0, humF: 110 }; S.grain = 0.05; S.flash = 0; }
const walkBob = (z, amp = 1) => ({ y: 0.028 * amp * MOTION * Math.sin(z * 6.2), roll: 0.006 * amp * MOTION * Math.sin(z * 3.1) });

// ================================================================== ESCENAS
// ---- 1. El oculo -----------------------------------------------------------
const W_OCULO = (() => {
  const Q = [];
  revolve(Q, [[0.3, 0], [20, 0]], 40, { alb: ASH, amp: 0.4, tile: 1.4 });
  revolve(Q, [[20, 0], [20.6, 4], [20, 8], [18.5, 13], [15, 17.5], [10.5, 20.5], [6, 22]], 40, { alb: ROCK, amp: 0.9, tile: 1.7 });
  const R = rng(11);
  for (let i = 0; i < 16; i++) { const a = R() * 6.28, r = 7 + R() * 11, s = 0.5 + R() * 1.6;
    box(Q, Math.cos(a) * r, s * 0.4, Math.sin(a) * r, s, s * 0.8 + R(), s * (0.7 + R() * 0.6), { alb: ROCK, jit: 0.3 }); }
  return Q;
})();
function rOculo(lt) {
  cam.p = V(0.3 * hh(lt * 0.2), 1.7, -13 + lt * 0.2);
  cam.pitch = lerp(1.02, -0.05, ease(clamp((lt - 1.0) / 6.4)));
  cam.yaw = 0.05 * hh(lt * 0.3); cam.roll = 0.01 * hh(lt * 0.25 + 4); cam.fov = 1.0;
  setCam();
  const sun = { p: V(0, 24, 0), dir: V(0, -1, 0), col: [1.0, 0.56, 0.32], int: 2.6, k: 0.0015, range: 70,
                cosO: Math.cos(0.4), cosI: Math.cos(0.17) };
  const L = [sun, { p: V(0, 21, 0), col: OCULO, int: 0.05, k: 0.0005, range: 60 }], env = { amb: [0.009, 0.008, 0.009], fog: 0.01 };
  // cielo tras el oculo
  const ring = []; for (let i = 0; i < 40; i++) { const a = i / 40 * 6.283; ring.push(proj(V(Math.cos(a) * 6.4, 22.2, Math.sin(a) * 6.4))); }
  const ctr = proj(V(0, 22.6, 0));
  if (ctr && ring.every(Boolean)) {
    const R = Math.hypot(ring[0][0] - ctr[0], ring[0][1] - ctr[1]) * 1.2;
    const gl = g.createRadialGradient(ctr[0], ctr[1], 0, ctr[0], ctr[1], R * 3);
    gl.addColorStop(0, 'rgba(201,113,74,0.35)'); gl.addColorStop(1, 'rgba(201,113,74,0)');
    g.fillStyle = gl; g.fillRect(0, 0, W, H);
    const gr = g.createRadialGradient(ctr[0], ctr[1], 0, ctr[0], ctr[1], R);
    gr.addColorStop(0, '#fff3e2'); gr.addColorStop(0.45, '#ffc98f'); gr.addColorStop(1, '#c9714a');
    g.fillStyle = gr; g.beginPath(); ring.forEach((p, i) => i ? g.lineTo(p[0], p[1]) : g.moveTo(p[0], p[1])); g.closePath(); g.fill();
  }
  renderWorld(W_OCULO, L, env);
  // columna de luz
  g.globalCompositeOperation = 'lighter';
  for (let i = 0; i < 28; i++) {
    const a0 = i / 28 * 6.283, a1 = (i + 1) / 28 * 6.283;
    const P = [V(Math.cos(a0) * 6, 22, Math.sin(a0) * 6), V(Math.cos(a1) * 6, 22, Math.sin(a1) * 6),
               V(Math.cos(a1) * 9.6, 0, Math.sin(a1) * 9.6), V(Math.cos(a0) * 9.6, 0, Math.sin(a0) * 9.6)].map(proj);
    if (P.some(p => !p)) continue;
    g.fillStyle = 'rgba(255,150,95,0.022)'; g.beginPath(); P.forEach((p, k) => k ? g.lineTo(p[0], p[1]) : g.moveTo(p[0], p[1])); g.fill();
  }
  g.globalCompositeOperation = 'source-over';
  motes(320, 7, (i, a, b, c) => {
    const y = (b * 22 + lt * 0.25) % 22, r = Math.sqrt(a) * (6 + (22 - y) * 0.16);
    const an = c * 6.283 + lt * 0.02; return V(Math.cos(an) * r, y, Math.sin(an) * r + Math.sin(lt * 0.5 + i) * 0.2);
  }, L, env, 0.6);
  S.audio = { drone: 0.5, wind: 0.28, hum: 0, humF: 110 };
}

// ---- 2. Tunel de ceniza: se enciende la linterna ---------------------------
const PT = [[-1.7, 0], [1.7, 0], [1.85, 1.8], [1.5, 2.6], [0.8, 3.1], [0, 3.25], [-0.8, 3.1], [-1.5, 2.6], [-1.85, 1.8], [-1.7, 0]];
const W_TUNEL = (() => {
  const Q = []; extrude(Q, PT, -2, 42, { alb: ASH, amp: 0.2, tile: 0.42 });
  const R = rng(4);
  for (let i = 0; i < 18; i++) { const s = 0.15 + R() * 0.45; box(Q, (R() - 0.5) * 2.6, s * 0.35, 2 + R() * 30, s, s * 0.7, s * 1.2, { alb: ROCK }); }
  return Q;
})();
function rTunel(lt) {
  const z = 0.5 + Math.max(0, lt - 1.4) * 0.85, b = walkBob(z, lt > 1.4 ? 1 : 0);
  cam.p = V(0.05 * hh(lt * 0.4), 1.62 + b.y, z);
  cam.yaw = 0.08 * hh(lt * 0.6); cam.pitch = -0.04 + 0.03 * hh(lt * 0.5 + 3); cam.roll = b.roll; cam.fov = 1.05;
  setCam();
  const on = lt >= 0.8;
  const flick = lt < 1.15 ? (hash(Math.floor(lt * 40), 1, 2) > 0.4 ? 1 : 0.1) : 1;
  const L = on ? [flashlight(FILTROS.none, flick, 0.05 * hh(lt * 0.9 + 1), -0.02)] : [];
  const env = { amb: [0.0012, 0.0011, 0.0013], fog: 0.055 };
  renderWorld(W_TUNEL, L, env);
  if (on) { haze(L[0]); motes(160, 3, (i, a, b2, c) => V((a - 0.5) * 3, 0.3 + b2 * 2.7, cam.p.z + 0.4 + ((c * 8 + lt * 0.06) % 8)), L, env, 0.9); }
  S.audio = { drone: 0.35, wind: 0.14, hum: 0, humF: 110 };
}

// ---- 3 / 10. Titulos --------------------------------------------------------
function rBlack() { S.audio = { drone: 0.2, wind: 0.05, hum: 0, humF: 110 }; }

// ---- 4. CUERPO: anclas y puente de luz -------------------------------------
const PC = [[-6, -12], [-6, 0], [-6.3, 2.5], [-5.8, 5], [-4, 6.8], [-1.5, 7.6], [1.5, 7.6], [4, 6.8], [5.8, 5], [6.3, 2.5], [6, 0], [6, -12]];
const ST4 = { a: 0, b: 0, bridge: 0 };
const W_CUERPO = (() => {
  const Q = [];
  extrude(Q, PC, -3, 30, { alb: ROCK, amp: 0.45, tile: 0.9 });
  grid(Q, V(-6, 0, -3), V(12, 0, 0), V(0, 0, 9), 18, 13, { alb: ASH, amp: 0.1 });
  grid(Q, V(-6, 0, 15), V(12, 0, 0), V(0, 0, 15), 18, 20, { alb: ASH, amp: 0.1 });
  grid(Q, V(-6, 0, 6), V(12, 0, 0), V(0, -12, 0), 16, 14, { alb: ROCK, amp: 0.35 });
  grid(Q, V(-6, -12, 15), V(12, 0, 0), V(0, 12, 0), 16, 14, { alb: ROCK, amp: 0.35 });
  const ancla = (x, z, k) => {
    box(Q, x, 0.6, z, 0.42, 1.2, 0.42, { alb: ROCK, jit: 0.1 });
    box(Q, x, 1.45, z, 0.5, 0.5, 0.5, { alb: ANCLA_OFF, jit: 0.05, upd: q => {
      const c = ST4[k], col = [0, 1, 2].map(i => lerp(ANCLA_OFF[i], AMBER[i], c));
      q.alb = col; q.em = col.map(v => v * (0.02 + 1.3 * c)); } });
  };
  ancla(-5, 16.5, 'a'); ancla(5, 17.5, 'b');
  for (let i = 0; i < 12; i++) {
    const z0 = 6 + i * 0.75;
    for (let s = 0; s < 2; s++) {
      quad(Q, V(-0.95 + s * 0.95, 0.02, z0), V(s * 0.95, 0.02, z0), V(s * 0.95, 0.02, z0 + 0.75), V(-0.95 + s * 0.95, 0.02, z0 + 0.75),
        { alb: [0.5, 0.35, 0.16], jit: 0.12, i, upd: q => {
          const v = clamp(ST4.bridge * 1.5 - q.i / 12 * 0.5);
          q.a = v * 0.82; q.em = AMBER.map(c => c * (0.35 + 0.5 * v) * v); } });
    }
  }
  return Q;
})();
function rCuerpo(lt) {
  const walk = ease(clamp((lt - 6) / 5)), z = 2 + walk * 7.5, b = walkBob(z, walk > 0 && walk < 1 ? 1 : 0);
  cam.p = V(0.04 * hh(lt * 0.3), 1.62 + b.y, z); cam.roll = b.roll; cam.fov = 1.1;
  cam.yaw = key(lt, [[0, 0.1], [1.8, 0], [3.0, -0.34], [3.6, -0.34], [5.0, 0], [11, 0]]) + 0.03 * hh(lt * 0.7);
  cam.pitch = key(lt, [[0, -0.12], [5, -0.03], [6.2, -0.05], [8.2, -0.5], [9.4, -0.48], [11, -0.14]]) + 0.02 * hh(lt * 0.6 + 2);
  setCam();
  ST4.a = clamp((lt - 3.0) / 0.35); ST4.b = clamp((lt - 4.6) / 0.35); ST4.bridge = clamp((lt - 4.95) / 1.1);
  const bs = beamState(lt, 1.2, 'none', 'cuerpo');
  const L = [flashlight(bs.F, bs.mul, 0.04 * hh(lt + 5), -0.02)];
  L.push({ p: V(-5, 1.45, 16.5), col: AMBER, int: 1.1 * ST4.a, k: 0.25, range: 8 });
  L.push({ p: V(5, 1.45, 17.5), col: AMBER, int: 1.1 * ST4.b, k: 0.25, range: 8 });
  L.push({ p: V(0, 0.4, 10.5), col: AMBER, int: 0.9 * ST4.bridge, k: 0.1, range: 12 });
  const env = { amb: [0.0012, 0.0011, 0.0012], fog: 0.03 };
  renderWorld(W_CUERPO, L, env);
  if (ST4.bridge > 0) { // bordes del puente
    const len = 9 * clamp(ST4.bridge * 1.3), c = 'rgb(255,196,120)';
    g.globalCompositeOperation = 'lighter';
    for (const x of [-0.95, 0.95]) {
      polyline([V(x, 0.03, 6), V(x, 0.03, 6 + len)], 3, 'rgba(232,161,74,0.35)', ST4.bridge);
      polyline([V(x, 0.03, 6), V(x, 0.03, 6 + len)], 1, c, ST4.bridge);
    }
    g.globalCompositeOperation = 'source-over';
  }
  haze(L[0]);
  motes(200, 5, (i, a, b2, c) => V((a - 0.5) * 11, -5 + b2 * 10 + Math.sin(lt * 0.3 + i) * 0.3, cam.p.z + 0.5 + ((c * 15 + lt * 0.1) % 15)), L, env, 0.7);
  S.audio = { drone: 0.4, wind: 0.2, hum: lt > 1.2 ? 0.1 : 0, humF: 110 };
}

// ---- 5. HUECO: la reja cede ------------------------------------------------
const PT2 = PT.map(([x, y]) => [x * 1.12, y * 1.12]);
const ceilAt = x => { let best = 0; for (let i = 0; i < PT2.length - 1; i++) { const a = PT2[i], b = PT2[i + 1];
  if (a[1] < 1 || b[1] < 1) continue; const lo2 = Math.min(a[0], b[0]), hi = Math.max(a[0], b[0]);
  if (x >= lo2 && x <= hi && hi > lo2) best = Math.max(best, lerp(a[1], b[1], (x - a[0]) / (b[0] - a[0]))); } return best || 2; };
const ST5 = { dis: 0 };
const W_HUECO = (() => {
  const Q = []; extrude(Q, PT2, -2, 40, { alb: ASH, amp: 0.2, tile: 0.48 });
  const reja = { alb: [0.2, 0.165, 0.13], jit: 0.15, upd: q => {
    const d = ST5.dis; q.a = lerp(1, 0.15, d); q.em = BLUE.map(c => c * (0.25 * d + 1.6 * d * (1 - d))); } };
  for (let x = -1.82; x <= 1.83; x += 0.26) { const h = ceilAt(x) - 0.05; box(Q, x, h / 2, 9, 0.07, h, 0.07, reja); }
  for (const [y, w] of [[0.7, 3.9], [1.6, 3.9], [2.5, 3.3]]) box(Q, 0, y, 9, w, 0.07, 0.08, reja);
  const R = rng(9);
  for (let i = 0; i < 16; i++) { const s = 0.15 + R() * 0.5; box(Q, (R() - 0.5) * 2.8, s * 0.35, 1 + R() * 34, s, s * 0.7, s * 1.2, { alb: ROCK }); }
  return Q;
})();
function rHueco(lt) {
  const walk = ease(clamp((lt - 3.1) / 6.2)), z = 3.2 + walk * 10.5, b = walkBob(z, walk > 0 && walk < 1 ? 1 : 0);
  cam.p = V(0.05 * hh(lt * 0.4), 1.62 + b.y, z); cam.roll = b.roll; cam.fov = 1.05;
  cam.yaw = 0.07 * hh(lt * 0.55); cam.pitch = -0.03 + 0.025 * hh(lt * 0.6 + 1);
  setCam();
  ST5.dis = clamp((lt - 2.55) * 4);
  const bs = beamState(lt, 1.4, 'none', 'hueco');
  const L = [flashlight(bs.F, bs.mul, 0.04 * hh(lt + 2), -0.02)];
  const env = { amb: [0.0011, 0.0011, 0.0014], fog: 0.035 };
  renderWorld(W_HUECO, L, env);
  haze(L[0], 0.09);
  motes(180, 8, (i, a, b2, c) => V((a - 0.5) * 3.4, 0.3 + b2 * 3, cam.p.z + 0.4 + ((c * 10 + lt * 0.06) % 10)), L, env, 0.9);
  S.audio = { drone: 0.36, wind: 0.14, hum: lt > 1.4 ? 0.11 : 0, humF: 82.4 };
}

// ---- 6. RASTRO: la escritura que queda -------------------------------------
const GLY = (() => {
  const R = rng(23), out = [];
  for (const y of [1.15, 1.85, 2.55, 3.25]) for (let x = -5.6; x <= 5.61; x += 0.66) {
    if (R() < 0.12) continue;
    const strokes = [], n = 2 + Math.floor(R() * 3), P = (i, j) => V(x + (i - 1) * 0.2, y + (j - 1) * 0.2, 5.97);
    for (let s = 0; s < n; s++) {
      let i = Math.floor(R() * 3), j = Math.floor(R() * 3); const line = [P(i, j)];
      for (let k = 0; k < 1 + Math.floor(R() * 2); k++) { i = clamp(i + Math.floor(R() * 3) - 1, 0, 2); j = clamp(j + Math.floor(R() * 3) - 1, 0, 2); line.push(P(i, j)); }
      if (line.length > 1) strokes.push(line);
    }
    out.push({ c: V(x, y, 5.97), strokes, last: -99 });
  }
  return out;
})();
const W_RASTRO = (() => {
  const Q = [];
  grid(Q, V(-7, 0, -3), V(14, 0, 0), V(0, 0, 9), 16, 10, { alb: ASH, amp: 0.1 });
  grid(Q, V(-7, 0, 6), V(14, 0, 0), V(0, 4.6, 0), 40, 13, { alb: [0.34, 0.31, 0.28], amp: 0.1 });
  grid(Q, V(-7, 0, -3), V(0, 0, 9), V(0, 4.6, 0), 10, 6, { alb: ROCK, amp: 0.2 });
  grid(Q, V(7, 0, -3), V(0, 4.6, 0), V(0, 0, 9), 6, 10, { alb: ROCK, amp: 0.2 });
  grid(Q, V(-7, 4.6, -3), V(0, 0, 9), V(14, 0, 0), 8, 14, { alb: ROCK, amp: 0.15 });
  return Q;
})();
function camRastro(t) {
  const back = ease(clamp((t - 6.4) / 4.6));
  const yaw = key(t, [[0, -0.12], [1.9, -0.82], [6.1, 0.82], [7.5, 0.3], [11.5, 0.0]]);
  const pitch = t > 1.9 && t < 6.1 ? 0.09 + 0.15 * Math.sin((t - 1.9) * 3.4) : key(t, [[0, 0.05], [1.9, 0.09], [6.1, 0.09], [8, 0.1]]);
  return { p: V(0, 1.65, 0.2 - 1.4 * back), yaw: yaw + 0.02 * hh(t), pitch };
}
function rRastro(lt) {
  // simulacion: cada glifo guarda la ultima vez que el haz RASTRO lo toco
  for (const gl of GLY) gl.last = -99;
  for (let t = 1.8; t <= Math.min(lt, 6.3); t += 1 / 30) {
    const c = camRastro(t), dir = fwdOf(c.yaw, c.pitch), cosHalf = Math.cos(16 * Math.PI / 180);
    for (const gl of GLY) { const d = vnorm(vsub(gl.c, c.p)); if (d.x * dir.x + d.y * dir.y + d.z * dir.z > cosHalf) gl.last = t; }
  }
  const c = camRastro(lt);
  cam.p = c.p; cam.yaw = c.yaw; cam.pitch = c.pitch; cam.roll = 0; cam.fov = 1.1; setCam();
  let sum = 0;
  for (const gl of GLY) { const rest = 8 - (lt - gl.last); gl.b = rest > 0 ? Math.pow(clamp(rest / 8), 0.45) * 2.2 : 0; sum += gl.b; }
  const on = lt < 6.3, bs = beamState(lt, 1.0, 'none', 'rastro');
  const L = on ? [flashlight(bs.F, bs.mul, 0.03 * hh(lt + 7), -0.02)] : [];
  L.push({ p: V(0, 2.2, 5.3), col: BONE, int: 0.035 * sum, k: 0.1, range: 9 });
  const env = { amb: [0.001, 0.001, 0.0011], fog: 0.03 };
  renderWorld(W_RASTRO, L, env);
  g.globalCompositeOperation = 'lighter';
  for (const gl of GLY) {
    if (gl.b < 0.01) continue;
    const s = proj(gl.c); if (!s) continue;
    const col = tone(BONE[0] * gl.b, BONE[1] * gl.b, BONE[2] * gl.b, 1, 1), lw = 0.045 * CT.f / s[2];
    for (const st of gl.strokes) { polyline(st, lw * 3, rgb(col), 0.16 * Math.min(1, gl.b)); polyline(st, lw, rgb(col), Math.min(1, gl.b * 0.8)); }
  }
  g.globalCompositeOperation = 'source-over';
  if (on) haze(L[0]);
  S.audio = { drone: 0.3, wind: 0.12, hum: on && lt > 1.0 ? 0.1 : 0, humF: 146.8 };
}

// ---- 7. Adaptacion a la oscuridad ------------------------------------------
const W_ADAPT = (() => {
  const Q = [];
  grid(Q, V(-10, 0, -2), V(20, 0, 0), V(0, 0, 14), 20, 14, { alb: ASH, amp: 0.15 });
  grid(Q, V(-10, 0, 12), V(20, 0, 0), V(0, 14, 0), 28, 18, { alb: [0.31, 0.29, 0.27], amp: 0.12 });
  grid(Q, V(-10, 0, -2), V(0, 0, 14), V(0, 14, 0), 12, 11, { alb: ROCK, amp: 0.4 });
  grid(Q, V(10, 0, -2), V(0, 14, 0), V(0, 0, 14), 11, 12, { alb: ROCK, amp: 0.4 });
  grid(Q, V(-10, 14, -2), V(0, 0, 14), V(20, 0, 0), 8, 12, { alb: ROCK, amp: 0.3 });
  for (const x of [-7.5, 7.5]) for (const z of [2.5, 6.5, 10.5]) box(Q, x, 7, z, 1.1, 14, 1.1, { alb: [0.26, 0.24, 0.22], jit: 0.12 });
  return Q;
})();
const DOOR = (() => {
  const Z = 11.95, arch = (r, top) => { const p = [V(-r, 0.3, Z), V(-r, top, Z)];
    for (let i = 1; i < 18; i++) { const a = Math.PI - i / 18 * Math.PI; p.push(V(Math.cos(a) * r, top + Math.sin(a) * r, Z)); }
    p.push(V(r, top, Z), V(r, 0.3, Z)); return p; };
  const lines = [{ pts: arch(2.4, 6.4), em: 0.0034, col: BONE }, { pts: arch(2.95, 6.4), em: 0.0026, col: BONE },
                 { pts: arch(3.5, 6.4), em: 0.0018, col: BONE }, { pts: [V(0, 0.3, Z), V(0, 7.6, Z)], em: 0.009, col: OCULO }];
  const eye = [], iris = [];
  for (let i = 0; i <= 32; i++) { const a = i / 32 * 6.283; eye.push(V(Math.cos(a) * 2.1, 10.7 + Math.sin(a) * 0.85 * Math.abs(Math.sin(a)) * Math.sign(Math.sin(a)), Z)); iris.push(V(Math.cos(a) * 0.5, 10.7 + Math.sin(a) * 0.5, Z)); }
  lines.push({ pts: eye, em: 0.003, col: BONE }, { pts: iris, em: 0.008, col: OCULO });
  for (let i = 0; i < 9; i++) { const a = Math.PI * (0.12 + i / 8 * 0.76); lines.push({ pts: [V(Math.cos(a) * 2.5, 10.7 + Math.sin(a) * 1.1, Z), V(Math.cos(a) * 3.4, 10.7 + Math.sin(a) * 1.9, Z)], em: 0.002, col: BONE }); }
  lines.push({ pts: [V(-9.8, 0.4, Z), V(9.8, 0.4, Z)], em: 0.0015, col: BONE });
  return lines;
})();
function adaptProgress(lt) {
  if (lt < 11.6) return clamp((lt - 4.0) / 6.5);         // demora y adaptacion lenta
  return clamp(1 - (lt - 11.6) / 1.2);                    // al encender se pierde rapido
}
function rAdapt(lt) {
  const push = ease(clamp((lt - 3.5) / 8));
  cam.p = V(0, 1.65, 1 - 2 * push); cam.fov = 1.2; cam.roll = 0;
  cam.yaw = (lt < 3 ? 0.22 * Math.sin(lt * 1.1) : 0) + 0.03 * hh(lt * 0.4);
  cam.pitch = key(lt, [[0, 0.06], [3, 0.1], [10.5, 0.3], [14, 0.3]]) + 0.015 * hh(lt * 0.5);
  setCam();
  const p = adaptProgress(lt), curve = smooth(0, 1, p);
  const on = lt < 3.0 || lt >= 11.6;
  const L = on ? [flashlight(FILTROS.none, 1, 0.04 * hh(lt + 3), -0.02)] : [];
  L.push({ p: V(0, 3, 11.5), col: OCULO, int: 0.004, k: 0.01, range: 22 });
  const E = Math.pow(2, 5 * curve), sat = lerp(1, 0.22, curve);
  const env = { amb: [0.0035, 0.0036, 0.0042], fog: 0.02, E, sat };
  renderWorld(W_ADAPT, L, env);
  g.globalCompositeOperation = 'lighter';
  for (const ln of DOOR) {
    const c = tone(ln.col[0] * ln.em, ln.col[1] * ln.em, ln.col[2] * ln.em, E, sat); if (c[0] + c[1] + c[2] < 3) continue;
    const s = proj(ln.pts[0]); if (!s) continue; const lw = 0.07 * CT.f / s[2];
    polyline(ln.pts, lw * 4, rgb(c), 0.18); polyline(ln.pts, lw, rgb(c), 0.95);
  }
  g.globalCompositeOperation = 'source-over';
  if (on) haze(L[0]);
  S.grain = 0.05 + 0.16 * curve;
  S.audio = { drone: 0.26 * (1 - 0.5 * curve), wind: 0.05 + curve * 0.6, hum: 0, humF: 110 };
}

// ---- 8. La lente ------------------------------------------------------------
const ST8 = { open: 0 };
const W_LENTE = (() => {
  const Q = [];
  grid(Q, V(-4, 0, -1), V(8, 0, 0), V(0, 0, 14), 12, 20, { alb: ASH, amp: 0.08 });
  grid(Q, V(-4, 0, -1), V(0, 0, 14), V(0, 4, 0), 18, 6, { alb: ROCK, amp: 0.2 });
  grid(Q, V(4, 0, -1), V(0, 4, 0), V(0, 0, 14), 6, 18, { alb: ROCK, amp: 0.2 });
  grid(Q, V(-4, 4, -1), V(0, 0, 14), V(8, 0, 0), 12, 8, { alb: ROCK, amp: 0.1 });
  grid(Q, V(-4, 0, 13), V(8, 0, 0), V(0, 4, 0), 10, 5, { alb: ROCK, amp: 0.2 });
  const plate = (dir) => ({ alb: [0.16, 0.15, 0.14], jit: 0.12, upd: q => { const k = ease(ST8.open); q.off = V(dir[0] * k * 2.2, dir[1] * k * 2.2, 0); } });
  grid(Q, V(-4, 0, 6), V(3.8, 0, 0), V(0, 4, 0), 8, 8, plate([-1, 0]));
  grid(Q, V(0.2, 0, 6), V(3.8, 0, 0), V(0, 4, 0), 8, 8, plate([1, 0]));
  grid(Q, V(-0.2, 1.82, 6), V(0.4, 0, 0), V(0, 2.18, 0), 1, 4, plate([0, 1]));
  grid(Q, V(-0.2, 0, 6), V(0.4, 0, 0), V(0, 1.42, 0), 1, 3, plate([0, -1]));
  box(Q, 0, 0.8, 10.2, 0.25, 1.6, 0.25, { alb: ROCK }); // soporte de la lente
  return Q;
})();
const LENS = V(0, 1.62, 10), HOLE = V(0, 1.62, 6), HOLE_R = 0.2;
const lensX = lt => key(lt, [[0, -1.8], [2.6, 0.5], [3.7, -0.14], [4.6, 0.03], [5.3, 0.0], [9, 0.0]]);
// misma cuenta que Lente.CalcularAlineacionCruda
function alignRaw(eye) {
  const d = vnorm(vsub(LENS, eye)), t = (HOLE.z - eye.z) / d.z, pt = vadd(eye, vmul(d, t));
  return clamp(1 - Math.hypot(pt.x - HOLE.x, pt.y - HOLE.y) / HOLE_R);
}
const LENS_SIM = (() => { // AlineacionSuave (MoveTowards 2/s) + sostenido 2.2 s
  const out = [], dt = 1 / 60; let s = 0, held = 0, tR = null;
  for (let t = 0; t <= 9; t += dt) {
    s += clamp(alignRaw(V(lensX(t), 1.62, 2)) - s, -2 * dt, 2 * dt);
    if (tR === null) { held = s > 0.55 ? held + dt : 0; if (held > 2.2) tR = t; }
    out.push(s);
  }
  return { smooth: out, tR: tR ?? 6.4 };
})();
function rLente(lt) {
  const x = lensX(lt);
  cam.p = V(x, 1.62, 2); cam.yaw = Math.atan2(-x, 4) + 0.01 * hh(lt); cam.pitch = 0.005 * hh(lt + 1); cam.roll = 0; cam.fov = 1.0;
  setCam();
  const tR = LENS_SIM.tR, al = LENS_SIM.smooth[Math.min(LENS_SIM.smooth.length - 1, Math.floor(lt * 60))];
  const bloom = clamp((lt - tR) / 1.4);
  ST8.open = clamp((lt - tR - 0.3) / 1.6);
  const L = [flashlight(FILTROS.none, 0.7, 0.02 * hh(lt + 9), -0.04)];
  L.push({ p: vadd(LENS, V(0, 0, -0.2)), col: BONE, int: 0.4 * al + 3 * bloom, k: 0.2, range: 12 });
  const env = { amb: [0.001, 0.001, 0.0011], fog: 0.03 };
  renderWorld(W_LENTE, L, env);
  // la lente solo se ve a traves del agujero (hasta que el diafragma se abre)
  const hole = [V(-0.2, 1.42, 5.99), V(0.2, 1.42, 5.99), V(0.2, 1.82, 5.99), V(-0.2, 1.82, 5.99)].map(proj);
  const ls = proj(LENS);
  if (ls && hole.every(Boolean)) {
    const r = 0.3 * CT.f / ls[2];
    g.save();
    if (ST8.open < 0.05) { g.beginPath(); hole.forEach((p, i) => i ? g.lineTo(p[0], p[1]) : g.moveTo(p[0], p[1])); g.closePath(); g.clip(); }
    g.globalCompositeOperation = 'lighter';
    const gr = g.createRadialGradient(ls[0], ls[1], 0, ls[0], ls[1], r * (1 + 2 * al));
    gr.addColorStop(0, 'rgba(255,246,228,1)'); gr.addColorStop(0.35, 'rgba(240,200,150,0.7)'); gr.addColorStop(1, 'rgba(201,113,74,0)');
    g.fillStyle = gr; g.fillRect(0, 0, W, H);
    g.restore();
    const ring = []; for (let i = 0; i <= 32; i++) { const a = i / 32 * 6.283; ring.push(V(Math.cos(a) * HOLE_R, 1.62 + Math.sin(a) * HOLE_R, 5.985)); }
    polyline(ring, 1, 'rgba(237,234,226,0.35)', 1 - ST8.open);
    if (al > 0.3) { // destello horizontal
      g.globalCompositeOperation = 'lighter';
      const k = (al - 0.3) / 0.7, fl = g.createLinearGradient(ls[0] - W * 0.4, 0, ls[0] + W * 0.4, 0);
      fl.addColorStop(0, 'rgba(232,161,74,0)'); fl.addColorStop(0.5, 'rgba(255,230,190,' + (0.5 * k) + ')'); fl.addColorStop(1, 'rgba(232,161,74,0)');
      g.fillStyle = fl; g.fillRect(0, ls[1] - 1.2, W, 2.4);
      g.globalCompositeOperation = 'source-over';
    }
    if (bloom > 0) { // onda de resolucion
      g.globalCompositeOperation = 'lighter';
      g.strokeStyle = 'rgba(255,220,170,' + (0.8 * (1 - bloom)) + ')'; g.lineWidth = 2 + 6 * (1 - bloom);
      g.beginPath(); g.arc(ls[0], ls[1], bloom * W * 0.8, 0, 6.283); g.stroke();
      g.globalCompositeOperation = 'source-over';
    }
  }
  S.flash = 0.35 * Math.exp(-(lt - tR) * 3) * (lt > tR ? 1 : 0) + smooth(8.1, 8.9, lt);
  S.audio = { drone: 0.3 + 0.2 * bloom, wind: 0.08, hum: 0, humF: 110 };
}

// ---- 9. Montaje ------------------------------------------------------------
const CUTS = [['cuerpo', 5.3, 0.85], ['hueco', 2.45, 0.75], ['rastro', 6.9, 0.7], ['oculo', 6.4, 0.6], ['adapt', 10.3, 0.6],
              ['cuerpo', 8.6, 0.5], ['hueco', 5.2, 0.45], ['rastro', 4.2, 0.42], ['lente', 5.9, 0.4], ['tunel', 3.2, 0.36],
              ['cuerpo', 5.7, 0.32], ['adapt', 9.6, 0.3], ['rastro', 7.6, 0.3], ['hueco', 2.7, 0.28]];
const CUT_T = (() => { let t = 0; return CUTS.map(c => { const s = t; t += c[2]; return s; }); })();
const MONTAGE_LEN = CUT_T[CUT_T.length - 1] + CUTS[CUTS.length - 1][2];
function rMontaje(lt) {
  let i = CUT_T.length - 1; while (i > 0 && CUT_T[i] > lt) i--;
  if (lt < MONTAGE_LEN) {
    const [id, t0] = CUTS[i], k = lt - CUT_T[i];
    SC[id].render(t0 + k);
    S.flash = Math.max(S.flash, 0.22 * Math.exp(-k * 14));
  }
  S.audio = { drone: 0.5, wind: 0.15, hum: 0, humF: 110 };
}

// ---- 10. Cierre ------------------------------------------------------------
function rFinal(lt) {
  const gl = g.createRadialGradient(W / 2, H * 0.42, 0, W / 2, H * 0.42, H * 0.7);
  const a = smooth(0, 2, lt) * (1 - smooth(11, 12.5, lt));
  gl.addColorStop(0, 'rgba(201,113,74,' + (0.12 * a) + ')'); gl.addColorStop(1, 'rgba(201,113,74,0)');
  g.fillStyle = gl; g.fillRect(0, 0, W, H);
  g.globalCompositeOperation = 'lighter';
  for (let i = 0; i < 140; i++) {
    const x = (hash(i, 1, 1) * W + lt * (6 + hash(i, 2, 2) * 10)) % W, y = (hash(i, 3, 3) * H - lt * (3 + hash(i, 4, 4) * 6) + H * 4) % H;
    g.fillStyle = 'rgba(237,214,180,' + (0.12 + 0.3 * hash(i, 5, 5)) * a + ')'; g.fillRect(x, y, 1.2, 1.2);
  }
  g.globalCompositeOperation = 'source-over';
  S.audio = { drone: 0.4 * (1 - smooth(10, 12.5, lt)), wind: 0.12, hum: 0, humF: 110 };
}

// ------------------------------------------------------------------ titulos (alta resolucion)
function spaced(ctx, s, x, y, sp, align = 'center') {
  const ch = [...s], ws = ch.map(c => ctx.measureText(c).width), tot = ws.reduce((a, b) => a + b, 0) + sp * (ch.length - 1);
  let cx = align === 'center' ? x - tot / 2 : align === 'right' ? x - tot : x;
  const prev = ctx.textAlign; ctx.textAlign = 'left';
  ch.forEach((c, i) => { ctx.fillText(c, cx, y); cx += ws[i] + sp; });
  ctx.textAlign = prev; return tot;
}
function drawTitle(text, cx, cy, size, track, alpha, hot, glow) {
  if (alpha <= 0) return;
  const a = tA.getContext('2d'); a.clearRect(0, 0, CW, CH);
  a.font = '400 ' + size + 'px ' + DISPLAY; a.fillStyle = '#fff'; a.textBaseline = 'middle';
  spaced(a, text, cx, cy, size * track);
  m.save(); m.globalAlpha = alpha * 0.14; m.drawImage(tA, 0, 0); m.restore();
  const b = tB.getContext('2d'); b.globalCompositeOperation = 'source-over'; b.clearRect(0, 0, CW, CH);
  const gr = b.createRadialGradient(hot.x, hot.y, 0, hot.x, hot.y, hot.r);
  gr.addColorStop(0, 'rgb(255,246,230)'); gr.addColorStop(0.55, 'rgba(237,226,205,0.85)'); gr.addColorStop(1, 'rgba(237,226,205,0)');
  b.fillStyle = gr; b.fillRect(0, 0, CW, CH);
  b.globalCompositeOperation = 'destination-in'; b.drawImage(tA, 0, 0); b.globalCompositeOperation = 'source-over';
  m.save(); m.globalAlpha = alpha; m.shadowColor = glow; m.shadowBlur = size * 0.3; m.drawImage(tB, 0, 0); m.restore();
}
function ring(cx, cy, r, a) {
  const gr = m.createRadialGradient(cx, cy, r * 0.85, cx, cy, r * 1.25);
  gr.addColorStop(0, 'rgba(201,113,74,0)'); gr.addColorStop(0.3, 'rgba(201,113,74,' + 0.18 * a + ')'); gr.addColorStop(1, 'rgba(201,113,74,0)');
  m.fillStyle = gr; m.beginPath(); m.arc(cx, cy, r * 1.3, 0, 6.283); m.fill();
  m.strokeStyle = 'rgba(232,161,74,' + 0.45 * a + ')'; m.lineWidth = Math.max(1, DPR);
  m.beginPath(); m.arc(cx, cy, r, 0, 6.283); m.stroke();
}
function oTitulo1(lt) {
  const a = smooth(0, 0.12, lt) * (1 - smooth(2.1, 3, lt)), cx = VR.x + VR.w / 2, cy = VR.y + VR.h / 2;
  ring(cx, cy, VR.h * (0.26 + lt * 0.015), a);
  drawTitle('CRATER', cx, cy, VR.h * 0.19, 0.36 + lt * 0.03, a,
    { x: VR.x + VR.w * lerp(0.22, 0.8, ease(clamp(lt / 2.4))), y: cy, r: VR.h * 0.42 }, 'rgba(232,161,74,0.9)');
}
function oFinal(lt) {
  const u = VR.h / 100, cx = VR.x + VR.w / 2, cy = VR.y + VR.h * 0.44;
  const out = 1 - smooth(11, 12.4, lt), a = smooth(0.1, 1.3, lt) * out;
  ring(cx, cy, VR.h * (0.27 + 0.02 * smooth(0, 12, lt)), a);
  drawTitle('CRATER', cx, cy, u * 17, lerp(0.95, 0.42, ease(clamp(lt / 5))), a,
    { x: VR.x + VR.w * lerp(0.2, 0.5, ease(clamp((lt - 0.4) / 3))), y: cy, r: VR.h * (0.3 + 2.5 * smooth(2.8, 5, lt)) }, 'rgba(232,161,74,0.8)');
  const t1 = smooth(3.4, 4.6, lt) * out;
  if (t1 > 0) { m.fillStyle = 'rgba(237,234,226,' + t1 + ')'; m.font = 'italic 400 ' + (u * 5) + 'px ' + SERIF; m.textAlign = 'center'; m.textBaseline = 'middle';
    m.fillText('Lo que la luz tapa.', cx, cy + u * 15 + (1 - t1) * u); }
  const t2 = smooth(5.2, 6.2, lt) * out;
  if (t2 > 0) {
    const segW = VR.w * 0.07, gap = VR.w * 0.035, y = cy + u * 24;
    [['CUERPO', AMBER], ['HUECO', BLUE], ['RASTRO', BONE]].forEach(([name, c], i) => {
      const x = cx + (i - 1) * (segW + gap), col = c.map(v => v * 255 | 0);
      m.fillStyle = rgba(col, t2); m.fillRect(x - segW / 2, y, segW * smooth(5.2 + i * 0.15, 6 + i * 0.15, lt), Math.max(1.5, u * 0.3));
      m.font = '400 ' + (u * 1.7) + 'px ' + MONO; m.fillStyle = rgba(col, 0.8 * t2); spaced(m, name, x, y + u * 3.2, u * 0.5);
    });
  }
  const t3 = smooth(6.8, 7.8, lt) * out;
  if (t3 > 0) { m.font = '400 ' + (u * 2) + 'px ' + MONO; m.fillStyle = 'rgba(237,234,226,' + 0.75 * t3 + ')'; m.textBaseline = 'middle';
    spaced(m, 'PRÓXIMAMENTE', cx, VR.y + VR.h - VR.bar - u * 6, u * 0.9); }
}

// ------------------------------------------------------------------ linea de tiempo
const SC = {
  oculo:   { dur: 8,    render: rOculo,   fadeIn: 2.0, fadeOut: 0.5 },
  tunel:   { dur: 7,    render: rTunel,   fadeIn: 0,   fadeOut: 0 },
  titulo1: { dur: 3,    render: rBlack,   over: oTitulo1 },
  cuerpo:  { dur: 11,   render: rCuerpo,  fadeIn: 0.5, fadeOut: 0.5 },
  hueco:   { dur: 9.5,  render: rHueco,   fadeIn: 0.4, fadeOut: 0.5 },
  rastro:  { dur: 11.5, render: rRastro,  fadeIn: 0.4, fadeOut: 0.8 },
  adapt:   { dur: 14,   render: rAdapt,   fadeIn: 0.3, fadeOut: 0.9 },
  lente:   { dur: 9,    render: rLente,   fadeIn: 0.5, fadeOut: 0 },
  montaje: { dur: MONTAGE_LEN + 0.9, render: rMontaje },
  final:   { dur: 12.5, render: rFinal,   over: oFinal },
};
const ORDER = Object.keys(SC);
let TOTAL = 0; for (const id of ORDER) { SC[id].start = TOTAL; TOTAL += SC[id].dur; }
const sceneAt = t => { for (let i = ORDER.length - 1; i >= 0; i--) { const s = SC[ORDER[i]]; if (t >= s.start) return [s, t - s.start]; } return [SC.oculo, 0]; };

const CUES = [];
const cue = (id, t0, t1, text, style = 'line', extra = {}) => CUES.push({ t0: SC[id].start + t0, t1: SC[id].start + t1, text, style, ...extra });
cue('oculo', 1.8, 4.7, 'Al fondo del cráter');
cue('oculo', 4.6, 7.7, 'la luz apenas llega.');
cue('tunel', 3.2, 6.4, 'Llevás una sola.');
cue('cuerpo', 1.2, 10.6, 'CUERPO', 'hud', { n: 'I', col: AMBER });
cue('cuerpo', 6.8, 10.3, 'La luz sostiene.');
cue('hueco', 1.4, 9.1, 'HUECO', 'hud', { n: 'II', col: BLUE });
cue('hueco', 4.0, 7.8, 'La luz atraviesa.');
cue('rastro', 1.0, 11.0, 'RASTRO', 'hud', { n: 'III', col: BONE });
cue('rastro', 7.0, 10.6, 'La luz recuerda.');
cue('adapt', 0.9, 3.0, 'Apagala.', 'big');
cue('adapt', 8.0, 11.3, 'Y mirá lo que tapaba.');
cue('lente', 0.8, 4.5, 'Hay un solo lugar\ndesde donde se ve.');

function drawCue(c, t) {
  const a = smooth(c.t0, c.t0 + 0.6, t) * (1 - smooth(c.t1 - 0.6, c.t1, t)); if (a <= 0) return;
  const u = VR.h / 100, cx = VR.x + VR.w / 2;
  m.textBaseline = 'middle'; m.textAlign = 'center';
  if (c.style === 'line') {
    const lines = c.text.split('\n'), y0 = VR.y + VR.h - VR.bar - u * 13 - (lines.length - 1) * u * 6.5;
    m.font = 'italic 400 ' + (u * 5.6) + 'px ' + SERIF; m.fillStyle = 'rgba(237,234,226,' + a + ')';
    m.shadowColor = 'rgba(0,0,0,0.8)'; m.shadowBlur = u * 2;
    lines.forEach((l, i) => m.fillText(l, cx, y0 + i * u * 6.5 + (1 - a) * u * 1.2));
    m.shadowBlur = 0;
  } else if (c.style === 'big') {
    m.font = 'italic 400 ' + (u * 10) + 'px ' + SERIF; m.fillStyle = 'rgba(237,234,226,' + a + ')';
    m.fillText(c.text, cx, VR.y + VR.h / 2);
  } else if (c.style === 'hud') {
    const x = VR.x + u * 7, y = VR.y + VR.bar + u * 7, col = c.col.map(v => v * 255 | 0);
    m.fillStyle = rgba(col, a); m.beginPath(); m.arc(x + u * 0.6, y, u * 0.6, 0, 6.283); m.fill();
    m.font = '400 ' + (u * 1.6) + 'px ' + MONO; m.fillStyle = 'rgba(237,234,226,' + 0.5 * a + ')';
    spaced(m, 'FILTRO ' + c.n, x + u * 2.4, y - u * 1.6, u * 0.5, 'left');
    m.font = '500 ' + (u * 2.4) + 'px ' + MONO; m.fillStyle = rgba(col, a);
    spaced(m, c.text, x + u * 2.4, y + u * 1.4, u * 0.7, 'left');
  }
}

function drawFrame(t) {
  if (!CW) return;
  const [sc, lt] = sceneAt(t);
  resetS();
  g.globalCompositeOperation = 'source-over'; g.globalAlpha = 1; g.fillStyle = '#000'; g.fillRect(0, 0, W, H);
  sc.render(lt);
  m.globalAlpha = 1; m.globalCompositeOperation = 'source-over'; m.fillStyle = '#000'; m.fillRect(0, 0, CW, CH);
  m.imageSmoothingEnabled = true; m.imageSmoothingQuality = 'high';
  m.drawImage(lo, VR.x, VR.y, VR.w, VR.h);
  // grano
  m.save(); m.beginPath(); m.rect(VR.x, VR.y, VR.w, VR.h); m.clip();
  const gc = GRAIN[Math.max(0, Math.floor(t * 24)) % 4], pat = m.createPattern(gc, 'repeat');
  m.translate(Math.random() * 192, Math.random() * 192);
  m.globalCompositeOperation = 'screen'; m.globalAlpha = S.grain; m.fillStyle = pat; m.fillRect(-192, -192, CW + 384, CH + 384);
  m.restore();
  // vineta
  const vg = m.createRadialGradient(CW / 2, CH / 2, VR.h * 0.35, CW / 2, CH / 2, VR.w * 0.62);
  vg.addColorStop(0, 'rgba(0,0,0,0)'); vg.addColorStop(1, 'rgba(0,0,0,0.6)');
  m.fillStyle = vg; m.fillRect(VR.x, VR.y, VR.w, VR.h);
  if (S.flash > 0) { m.fillStyle = 'rgba(255,244,228,' + clamp(S.flash) + ')'; m.fillRect(VR.x, VR.y, VR.w, VR.h); }
  if (sc.over) sc.over(lt);
  const fade = Math.max(sc.fadeIn ? 1 - lt / sc.fadeIn : 0, sc.fadeOut ? 1 - (sc.dur - lt) / sc.fadeOut : 0);
  if (fade > 0) { m.fillStyle = 'rgba(0,0,0,' + clamp(fade) + ')'; m.fillRect(0, 0, CW, CH); }
  // franjas cinemascope
  m.fillStyle = '#000'; m.fillRect(VR.x, VR.y, VR.w, VR.bar); m.fillRect(VR.x, VR.y + VR.h - VR.bar, VR.w, VR.bar);
  for (const c of CUES) if (t >= c.t0 && t <= c.t1) drawCue(c, t);
  if (AU.C && playing) AU.levels(S.audio);
}

// ------------------------------------------------------------------ sonido
const AU = {
  C: null, muted: false,
  init() {
    if (this.C) return;
    const AC = window.AudioContext || window.webkitAudioContext; if (!AC) return;
    const C = this.C = new AC();
    const comp = C.createDynamicsCompressor(); comp.threshold.value = -16; comp.ratio.value = 4; comp.connect(C.destination);
    this.master = C.createGain(); this.master.gain.value = this.muted ? 0 : 0.85; this.master.connect(comp);
    const len = C.sampleRate * 3.8, ir = C.createBuffer(2, len, C.sampleRate);
    for (let ch = 0; ch < 2; ch++) { const d = ir.getChannelData(ch); for (let i = 0; i < len; i++) d[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / len, 2.8); }
    const rev = C.createConvolver(); rev.buffer = ir; rev.connect(this.master);
    this.rev = C.createGain(); this.rev.gain.value = 0.5; this.rev.connect(rev);
    this.noise = C.createBuffer(1, C.sampleRate * 2, C.sampleRate);
    { const d = this.noise.getChannelData(0); for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1; }
    // drone
    this.drone = C.createGain(); this.drone.gain.value = 0;
    const lp = C.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = 170; lp.Q.value = 3;
    [[55, 'sawtooth', 0.3], [55.4, 'sawtooth', 0.3], [82.6, 'triangle', 0.22]].forEach(([f, ty, v]) => {
      const o = C.createOscillator(); o.type = ty; o.frequency.value = f; const gg = C.createGain(); gg.gain.value = v; o.connect(gg); gg.connect(lp); o.start(); });
    const sub = C.createOscillator(); sub.frequency.value = 27.5; const sg = C.createGain(); sg.gain.value = 0.5; sub.connect(sg); sg.connect(this.drone); sub.start();
    const lfo = C.createOscillator(); lfo.frequency.value = 0.06; const lg = C.createGain(); lg.gain.value = 80; lfo.connect(lg); lg.connect(lp.frequency); lfo.start();
    lp.connect(this.drone); this.drone.connect(this.master); this.drone.connect(this.rev);
    // viento
    this.wind = C.createGain(); this.wind.gain.value = 0;
    const ws = C.createBufferSource(); ws.buffer = this.noise; ws.loop = true;
    const bp = C.createBiquadFilter(); bp.type = 'bandpass'; bp.frequency.value = 480; bp.Q.value = 0.9;
    const l2 = C.createOscillator(); l2.frequency.value = 0.13; const l2g = C.createGain(); l2g.gain.value = 260; l2.connect(l2g); l2g.connect(bp.frequency); l2.start();
    ws.connect(bp); bp.connect(this.wind); this.wind.connect(this.master); this.wind.connect(this.rev); ws.start();
    // zumbido del filtro
    this.hum = C.createGain(); this.hum.gain.value = 0;
    this.humO = C.createOscillator(); this.humO.frequency.value = 110;
    this.humO2 = C.createOscillator(); this.humO2.type = 'sawtooth'; this.humO2.frequency.value = 220;
    const hl = C.createBiquadFilter(); hl.type = 'lowpass'; hl.frequency.value = 500; const h2 = C.createGain(); h2.gain.value = 0.18;
    this.humO.connect(this.hum); this.humO2.connect(h2); h2.connect(hl); hl.connect(this.hum); this.humO.start(); this.humO2.start();
    this.hum.connect(this.master);
  },
  levels(a) {
    const now = this.C.currentTime;
    this.drone.gain.setTargetAtTime(a.drone, now, 0.25);
    this.wind.gain.setTargetAtTime(a.wind, now, 0.4);
    this.hum.gain.setTargetAtTime(a.hum * 0.5, now, 0.08);
    this.humO.frequency.setTargetAtTime(a.humF, now, 0.05); this.humO2.frequency.setTargetAtTime(a.humF * 2.01, now, 0.05);
  },
  env(node, t, a, peak, d) { const gg = node.gain; gg.cancelScheduledValues(t); gg.setValueAtTime(0.0001, t); gg.exponentialRampToValueAtTime(peak, t + a); gg.exponentialRampToValueAtTime(0.0001, t + a + d); },
  src(dur, type, freq, q) {
    const C = this.C, s = C.createBufferSource(); s.buffer = this.noise; const f = C.createBiquadFilter(); f.type = type; f.frequency.value = freq; f.Q.value = q || 0.7;
    const gg = C.createGain(); s.connect(f); f.connect(gg); s.start(C.currentTime, Math.random()); s.stop(C.currentTime + dur + 0.05); return [gg, f];
  },
  click() {
    if (!this.C) return; const t = this.C.currentTime;
    const [gg] = this.src(0.06, 'highpass', 1800); this.env(gg, t, 0.002, 0.5, 0.04); gg.connect(this.master); gg.connect(this.rev);
    const o = this.C.createOscillator(); o.type = 'square'; o.frequency.value = 2300; const og = this.C.createGain(); this.env(og, t, 0.001, 0.08, 0.012);
    o.connect(og); og.connect(this.master); o.start(t); o.stop(t + 0.05);
  },
  equip() {
    if (!this.C) return; this.click(); const t = this.C.currentTime + 0.09;
    const [gg, f] = this.src(0.3, 'bandpass', 900, 3); f.frequency.setValueAtTime(600, t); f.frequency.exponentialRampToValueAtTime(3200, t + 0.2);
    this.env(gg, t, 0.02, 0.3, 0.2); gg.connect(this.master); gg.connect(this.rev);
    setTimeout(() => this.click(), 170);
  },
  tone(f, dur = 5, vol = 0.22) {
    if (!this.C) return; const C = this.C, t = C.currentTime;
    [[1, 1], [2.005, 0.35], [3.01, 0.12]].forEach(([k, v]) => {
      const o = C.createOscillator(); o.frequency.value = f * k; const gg = C.createGain(); this.env(gg, t, 0.008, vol * v, dur);
      o.connect(gg); gg.connect(this.master); gg.connect(this.rev); o.start(t); o.stop(t + dur + 0.1); });
  },
  swell(dur = 4) {
    if (!this.C) return; const C = this.C, t = C.currentTime;
    [110, 164.8, 220].forEach(f => { const o = C.createOscillator(); o.type = 'triangle'; o.frequency.value = f; const gg = C.createGain();
      gg.gain.setValueAtTime(0.0001, t); gg.gain.exponentialRampToValueAtTime(0.1, t + dur * 0.4); gg.gain.exponentialRampToValueAtTime(0.0001, t + dur);
      o.connect(gg); gg.connect(this.master); gg.connect(this.rev); o.start(t); o.stop(t + dur + 0.1); });
  },
  hiss(dur = 1) {
    if (!this.C) return; const t = this.C.currentTime, [gg] = this.src(dur + 0.2, 'highpass', 4200);
    gg.gain.setValueAtTime(0.0001, t); gg.gain.exponentialRampToValueAtTime(0.22, t + 0.05); gg.gain.setValueAtTime(0.22, t + dur * 0.6); gg.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    gg.connect(this.master); gg.connect(this.rev);
  },
  boom(vol = 1) {
    if (!this.C) return; const C = this.C, t = C.currentTime;
    const o = C.createOscillator(); o.frequency.setValueAtTime(85, t); o.frequency.exponentialRampToValueAtTime(26, t + 1.8);
    const gg = C.createGain(); this.env(gg, t, 0.01, 0.95 * vol, 2.4); o.connect(gg); gg.connect(this.master); gg.connect(this.rev); o.start(t); o.stop(t + 2.6);
    const [ng] = this.src(0.6, 'lowpass', 380); this.env(ng, t, 0.005, 0.6 * vol, 0.5); ng.connect(this.master); ng.connect(this.rev);
  },
  hit() {
    if (!this.C) return; const C = this.C, t = C.currentTime;
    const o = C.createOscillator(); o.frequency.setValueAtTime(120, t); o.frequency.exponentialRampToValueAtTime(40, t + 0.25);
    const gg = C.createGain(); this.env(gg, t, 0.004, 0.7, 0.4); o.connect(gg); gg.connect(this.master); o.start(t); o.stop(t + 0.5);
    const [ng] = this.src(0.2, 'bandpass', 2200, 1.2); this.env(ng, t, 0.002, 0.25, 0.12); ng.connect(this.master); ng.connect(this.rev);
  },
  riser(dur) {
    if (!this.C) return; const C = this.C, t = C.currentTime, [gg, f] = this.src(dur, 'bandpass', 200, 5);
    f.frequency.setValueAtTime(200, t); f.frequency.exponentialRampToValueAtTime(5200, t + dur);
    gg.gain.setValueAtTime(0.0001, t); gg.gain.exponentialRampToValueAtTime(0.4, t + dur * 0.95); gg.gain.linearRampToValueAtTime(0, t + dur);
    gg.connect(this.master); gg.connect(this.rev);
    const o = C.createOscillator(); o.type = 'sawtooth'; o.frequency.setValueAtTime(55, t); o.frequency.exponentialRampToValueAtTime(220, t + dur);
    const og = C.createGain(); og.gain.setValueAtTime(0.0001, t); og.gain.exponentialRampToValueAtTime(0.06, t + dur * 0.95); og.gain.linearRampToValueAtTime(0, t + dur);
    const lp = C.createBiquadFilter(); lp.frequency.value = 900; o.connect(lp); lp.connect(og); og.connect(this.master); o.start(t); o.stop(t + dur + 0.05);
  },
  setMuted(v) { this.muted = v; if (this.C) this.master.gain.setTargetAtTime(v ? 0 : 0.85, this.C.currentTime, 0.05); },
};

const EVENTS = [];
const at = (id, t, fn) => EVENTS.push({ t: SC[id].start + t, fn });
at('oculo', 0.2, () => AU.swell(7));
at('tunel', 0.8, () => AU.click());
at('tunel', 5.4, () => AU.riser(1.6));
at('titulo1', 0, () => AU.boom(1));
at('cuerpo', 1.2, () => AU.equip());
at('cuerpo', 3.35, () => AU.tone(220));
at('cuerpo', 4.95, () => AU.tone(277.18));
at('cuerpo', 5.0, () => { AU.tone(329.63, 6, 0.16); AU.tone(440, 6, 0.1); AU.swell(5); });
at('hueco', 1.4, () => AU.equip());
at('hueco', 2.55, () => AU.hiss(1.1));
at('rastro', 1.0, () => AU.equip());
at('rastro', 6.3, () => AU.click());
at('rastro', 6.5, () => AU.tone(587.3, 7, 0.07));
at('adapt', 3.0, () => AU.click());
at('adapt', 11.6, () => { AU.click(); AU.hit(); });
at('lente', LENS_SIM.tR, () => { AU.tone(440, 6, 0.14); AU.tone(554.37, 6, 0.1); AU.tone(659.25, 6, 0.1); AU.tone(880, 5, 0.06); AU.boom(0.5); });
at('lente', 7.7, () => AU.riser(1.25));
CUTS.forEach((c, i) => at('montaje', CUT_T[i], () => (i === 0 ? AU.boom(0.9) : AU.hit())));
at('final', 0.1, () => AU.boom(1.1));
at('final', 3.4, () => { AU.tone(110, 9, 0.18); AU.tone(164.8, 9, 0.1); });
EVENTS.sort((a, b) => a.t - b.t);

// ------------------------------------------------------------------ reproduccion
let T = SC.final.start + 6.5, playing = false, last = 0, started = false;
const $ = id => document.getElementById(id);
const ui = { start: $('start'), end: $('end'), bar: $('bar'), fill: $('fill'), pause: $('btn-pause'), mute: $('btn-mute'), restart: $('btn-restart'), replay: $('btn-replay') };

function loop(now) {
  if (!playing) return;
  // el timestamp de rAF puede ser anterior a performance.now() del clic: dt nunca negativo
  const dt = clamp((now - last) / 1000, 0, 0.1); last = now;
  const prev = T; T += dt;
  for (const e of EVENTS) if (e.t > prev && e.t <= T) e.fn();
  if (T >= TOTAL) { T = TOTAL - 0.001; drawFrame(T); finish(); return; }
  drawFrame(T);
  ui.fill.style.transform = 'scaleX(' + (T / TOTAL).toFixed(4) + ')';
  requestAnimationFrame(loop);
}
function play() {
  AU.init(); if (AU.C && AU.C.state === 'suspended') AU.C.resume();
  playing = true; last = performance.now(); ui.pause.textContent = 'Pausa'; document.body.classList.add('is-playing');
  requestAnimationFrame(loop);
}
function pause() { playing = false; ui.pause.textContent = 'Seguir'; if (AU.C) AU.levels({ drone: 0, wind: 0, hum: 0, humF: 110 }); }
function begin() { T = 0; started = true; ui.start.hidden = true; ui.end.hidden = true; ui.bar.hidden = false; play(); }
function finish() { playing = false; if (AU.C) AU.levels({ drone: 0, wind: 0, hum: 0, humF: 110 }); ui.end.hidden = false; document.body.classList.remove('is-playing'); }

ui.start.addEventListener('click', begin);
ui.replay.addEventListener('click', begin);
ui.restart.addEventListener('click', begin);
ui.pause.addEventListener('click', () => (playing ? pause() : play()));
ui.mute.addEventListener('click', () => { AU.setMuted(!AU.muted); ui.mute.textContent = AU.muted ? 'Activar sonido' : 'Silenciar'; });
cv.addEventListener('click', () => { if (started && ui.end.hidden) (playing ? pause() : play()); });
window.addEventListener('keydown', e => {
  if (!started) { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); begin(); } return; }
  if (e.key === ' ') { e.preventDefault(); playing ? pause() : (ui.end.hidden ? play() : begin()); }
  else if (e.key === 'r' || e.key === 'R') begin();
  else if (e.key === 'm' || e.key === 'M') ui.mute.click();
});
let idle; const wake = () => { document.body.classList.add('ui-awake'); clearTimeout(idle); idle = setTimeout(() => document.body.classList.remove('ui-awake'), 2200); };
window.addEventListener('pointermove', wake); window.addEventListener('pointerdown', wake);
window.addEventListener('resize', resize);

const fontsReady = document.fonts ? Promise.all([`400 40px ${DISPLAY}`, `italic 400 40px ${SERIF}`, `400 20px ${MONO}`, `500 20px ${MONO}`]
  .map(f => document.fonts.load(f).catch(() => null))) : Promise.resolve();
resize();
fontsReady.then(() => { if (!started) drawFrame(T); });
})();
