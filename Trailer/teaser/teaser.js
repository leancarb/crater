/*
 * CRATER — teaser (~57 s)
 * Version corta del trailer. Mismo motor que Trailer/trailer.js: canvas 2D con
 * poligonos ordenados por profundidad, luces spot y sonido sintetizado con WebAudio.
 *
 * Sigue el GDD v0.5 con la estructura del trailer de Silo: planos lentos que se
 * aceleran, dos placas sobre negro con un golpe grave precedido de silencio, el
 * titulo recien al final y un cierre corto.
 *
 * Todo sale de drawFrame(t), que depende solo de t: se puede saltar a cualquier momento.
 * Cada escena reutiliza su render original con un mapa de tiempo propio (ver SC).
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
const OCULO = [0.788, 0.443, 0.29];        // Mat_Oculo emission
const GLOW  = [1.0, 0.42, 0.06];            // ambar saturado para lo que brilla (anclas, rocas)
const ECL   = [0.78, 0.83, 0.93];          // plateado frio: corona, puerta, oculos (GDD 5)
const WARM  = [1, 0.96, 0.88];             // LinternaController colorBase
const ASH   = [0.30, 0.27, 0.24];
const ROCK  = [0.21, 0.195, 0.185];
const ANCLA_OFF = [0.12, 0.11, 0.10];

const FILTROS = {
  none:   { col: WARM,  ang: 28, range: 14, int: 5.0 },
  cuerpo: { col: AMBER, ang: 45, range: 14, int: 5.5 },
  hueco:  { col: BLUE,  ang: 55, range: 30, int: 8.0 },
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
function poly(Q, pts, o) {
  let nx = 0, ny = 0, nz = 0, cx = 0, cy = 0, cz = 0;
  for (let i = 0; i < pts.length; i++) {
    const a = pts[i], b = pts[(i + 1) % pts.length];
    nx += (a.y - b.y) * (a.z + b.z); ny += (a.z - b.z) * (a.x + b.x); nz += (a.x - b.x) * (a.y + b.y);
    cx += a.x; cy += a.y; cz += a.z;
  }
  const k = pts.length, c = V(cx / k, cy / k, cz / k), n = vnorm(V(nx, ny, nz));
  const j = o.jit ?? 0.08, f = 1 - j + 2 * j * hash(c.x, c.y, c.z);
  const q = { p: pts, c, n, alb: [o.alb[0] * f, o.alb[1] * f, o.alb[2] * f], em: o.em ? o.em.slice() : [0, 0, 0],
              a: o.a ?? 1, upd: o.upd, i: o.i, off: null, layer: o.layer ?? 1, noSpot: o.noSpot };
  Q.push(q); return q;
}
const quad = (Q, p0, p1, p2, p3, o) => poly(Q, [p0, p1, p2, p3], o);
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
      const p = V(r * ca + (o.cx || 0), y, r * sa + (o.cz || 0)), d = disp(p) * amp * (1 - 0.7 * Math.abs(ny));
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

// prisma de base irregular con la tapa cortada en plano inclinado (monolitos, columnas)
function prism(Q, cx, cz, r, sides, y0, h, slope, o) {
  const R = rng(o.seed || 7), a0 = R() * 6.283, sa = R() * 6.283, B = [], T = [];
  for (let i = 0; i < sides; i++) {
    const a = a0 + i / sides * 6.283 + (R() - 0.5) * 0.4, rr = r * (0.8 + 0.4 * R());
    const x = cx + Math.cos(a) * rr, z = cz + Math.sin(a) * rr;
    B.push(V(x, y0, z)); T.push(V(x, h + slope * ((x - cx) * Math.cos(sa) + (z - cz) * Math.sin(sa)), z));
  }
  for (let i = 0; i < sides; i++) { const i1 = (i + 1) % sides; poly(Q, [B[i], B[i1], T[i1], T[i]], o); }
  poly(Q, T, o);
}
// caja rotada en el plano xz
function obox(Q, cx, cz, ang, w, d, y0, y1, o) {
  const ux = Math.cos(ang), uz = Math.sin(ang), vx = -uz, vz = ux;
  const P = (a, b, y) => V(cx + ux * a * w / 2 + vx * b * d / 2, y, cz + uz * a * w / 2 + vz * b * d / 2);
  const B = [P(-1, -1, y0), P(1, -1, y0), P(1, 1, y0), P(-1, 1, y0)], T = [P(-1, -1, y1), P(1, -1, y1), P(1, 1, y1), P(-1, 1, y1)];
  for (let i = 0; i < 4; i++) { const i1 = (i + 1) % 4; poly(Q, [B[i], B[i1], T[i1], T[i]], o); }
  poly(Q, T, o);
}
// roca facetada: icosaedro con vertices desplazados. Devuelve las caras (con q.on, normal hacia afuera)
function icosa(Q, c, r, seed, o) {
  const t = (1 + Math.sqrt(5)) / 2, R = rng(seed);
  const vs = [[-1, t, 0], [1, t, 0], [-1, -t, 0], [1, -t, 0], [0, -1, t], [0, 1, t], [0, -1, -t], [0, 1, -t], [t, 0, -1], [t, 0, 1], [-t, 0, -1], [-t, 0, 1]]
    .map(v => { const k = r * (0.8 + 0.35 * R()) / Math.hypot(v[0], v[1], v[2]); return V(c.x + v[0] * k * (o.sx || 1), c.y + v[1] * k * (o.sy || 1), c.z + v[2] * k); });
  const F = [[0, 11, 5], [0, 5, 1], [0, 1, 7], [0, 7, 10], [0, 10, 11], [1, 5, 9], [5, 11, 4], [11, 10, 2], [10, 7, 6], [7, 1, 8],
             [3, 9, 4], [3, 4, 2], [3, 2, 6], [3, 6, 8], [3, 8, 9], [4, 9, 5], [2, 4, 11], [6, 2, 10], [8, 6, 7], [9, 8, 1]];
  return F.map(f => { const q = poly(Q, [vs[f[0]], vs[f[1]], vs[f[2]]], o); q.on = vnorm(vsub(q.c, c)); return q; });
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
function lightAt(px, py, pz, n, L, out, noSpot) {
  for (const l of L) {
    if (l.int <= 0) continue;
    if (l.par) { // luz direccional: solo ilumina las caras que la miran
      const dd = n ? -(n.x * l.d.x + n.y * l.d.y + n.z * l.d.z) : 0.5; if (dd <= 0) continue;
      out[0] += l.col[0] * l.int * dd; out[1] += l.col[1] * l.int * dd; out[2] += l.col[2] * l.int * dd; continue;
    }
    if (noSpot && l.dir) continue;
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
  const amb = env.amb, E = env.E ?? 1, sat = env.sat ?? 1, fog = env.fog ?? 0.04, fc = env.fogCol || [0, 0, 0];
  const list = [], acc = [0, 0, 0];
  for (const q of Q) {
    if (q.upd) q.upd(q);
    if (q.a < 0.004) continue;
    const off = q.off, cs = q.p.map(p => toCam(p, off));
    let front = 0; for (const c of cs) if (c[2] >= NEAR) front++;
    if (!front) continue;
    const pl = front < cs.length ? clipNear(cs) : cs;
    let l = 0, r = 0, u = 0, dn = 0;
    const sp = pl.map(c => { const s = toScr(c[0], c[1], c[2]); if (s[0] < -40) l++; if (s[0] > W + 40) r++; if (s[1] < -40) u++; if (s[1] > H + 40) dn++; return s; });
    if (l === sp.length || r === sp.length || u === sp.length || dn === sp.length) continue;
    const cx = q.c.x + (off ? off.x : 0), cy = q.c.y + (off ? off.y : 0), cz = q.c.z + (off ? off.z : 0);
    // normal girada hacia la camara: en solidos convexos las caras visibles son las que la miran
    let n = q.n; if (n.x * (cam.p.x - cx) + n.y * (cam.p.y - cy) + n.z * (cam.p.z - cz) < 0) n = V(-n.x, -n.y, -n.z);
    acc[0] = amb[0]; acc[1] = amb[1]; acc[2] = amb[2];
    lightAt(cx, cy, cz, n, L, acc, q.noSpot);
    const dist = Math.hypot(cx - cam.p.x, cy - cam.p.y, cz - cam.p.z), fk = Math.exp(-dist * fog);
    const col = tone((q.alb[0] * acc[0] + q.em[0]) * fk + fc[0] * (1 - fk), (q.alb[1] * acc[1] + q.em[1]) * fk + fc[1] * (1 - fk),
                     (q.alb[2] * acc[2] + q.em[2]) * fk + fc[2] * (1 - fk), E, sat);
    let d = 0; for (const c of cs) d += c[2];
    list.push({ d: d / cs.length, layer: q.layer, sp, col, a: q.a });
  }
  // capas: 0 piso, 0.5 marcas en el piso, 1 todo lo demas. Entre el piso y los objetos va env.onFloor (la mancha del haz)
  list.sort((a, b) => a.layer - b.layer || b.d - a.d);
  g.lineJoin = 'round';
  let floorDone = false;
  for (const it of list) {
    if (!floorDone && it.layer >= 1) { floorDone = true; if (env.onFloor) env.onFloor(); }
    const s = it.sp; g.beginPath(); g.moveTo(s[0][0], s[0][1]);
    for (let i = 1; i < s.length; i++) g.lineTo(s[i][0], s[i][1]);
    g.closePath();
    const c = rgb(it.col);
    g.fillStyle = c;
    if (it.a < 1) { g.globalAlpha = it.a; g.fill(); g.globalAlpha = 1; }
    else { g.strokeStyle = c; g.lineWidth = 0.7; g.fill(); g.stroke(); }
  }
  if (!floorDone && env.onFloor) env.onFloor();
}

// mancha nitida del haz sobre el piso: el borde del cono intersectado con el plano y = y0
function floorPool(Lt, y0, alpha) {
  if (!Lt || Lt.int <= 0) return;
  const d = Lt.dir, u = vnorm(vcross(d, Math.abs(d.y) > 0.9 ? V(1, 0, 0) : V(0, 1, 0))), w = vcross(u, d), half = Math.acos(Lt.cosO);
  const cs = [];
  for (let k = 0; k < 36; k++) {
    const a = k / 36 * 6.283, r = vnorm(vadd(vmul(d, Math.cos(half)), vmul(vadd(vmul(u, Math.cos(a)), vmul(w, Math.sin(a))), Math.sin(half))));
    let t = Lt.range; if (r.y < -1e-3) t = Math.min(t, (y0 - Lt.p.y) / r.y);
    const q = vadd(Lt.p, vmul(r, t)); q.y = y0; cs.push(toCam(q));
  }
  const pl = clipNear(cs); if (pl.length < 3) return;
  const sp = pl.map(c => toScr(c[0], c[1], c[2])), col = Lt.col.map(v => v * 255 | 0);
  const flat = vnorm(V(d.x, 0, d.z)), a0 = proj(V(Lt.p.x + flat.x * 2, y0, Lt.p.z + flat.z * 2)), a1 = proj(V(Lt.p.x + flat.x * Lt.range, y0, Lt.p.z + flat.z * Lt.range));
  let fill = rgba(col, alpha);
  if (a0 && a1) { const gr = g.createLinearGradient(a0[0], a0[1], a1[0], a1[1]); gr.addColorStop(0, rgba(col, alpha)); gr.addColorStop(1, rgba(col, alpha * 0.55)); fill = gr; }
  g.fillStyle = fill; g.beginPath(); sp.forEach((p, i) => (i ? g.lineTo(p[0], p[1]) : g.moveTo(p[0], p[1]))); g.closePath(); g.fill();
}
// volumen del haz: una cuna translucida plana, de cara a la camara
function beamVolume(p0, p1, r0, r1, col, alpha) {
  const d = vnorm(vsub(p1, p0)), toC = vnorm(vsub(cam.p, vmul(vadd(p0, p1), 0.5))), s2 = vnorm(vcross(d, toC));
  const pl = clipNear([vadd(p0, vmul(s2, r0)), vadd(p1, vmul(s2, r1)), vsub(p1, vmul(s2, r1)), vsub(p0, vmul(s2, r0))].map(p => toCam(p)));
  if (pl.length < 3) return;
  const sp = pl.map(c => toScr(c[0], c[1], c[2]));
  g.fillStyle = rgba(col.map(v => v * 255 | 0), alpha);
  g.beginPath(); sp.forEach((p, i) => (i ? g.lineTo(p[0], p[1]) : g.moveTo(p[0], p[1]))); g.closePath(); g.fill();
}
// anillos concentricos del ancla encendida, en el plano de la cara que recibe el haz
function glyphRings(c, nrm, r, k) {
  if (k <= 0) return;
  const s0 = proj(c); if (!s0) return;
  const u = vnorm(vcross(nrm, Math.abs(nrm.y) > 0.9 ? V(1, 0, 0) : V(0, 1, 0))), w = vcross(u, nrm), lw = Math.max(0.8, CT.f * 0.05 * r / s0[2]);
  g.globalCompositeOperation = 'lighter';
  for (const rr of [r, r * 0.62]) {
    const pts = []; for (let i = 0; i <= 40; i++) { const a = i / 40 * 6.283; pts.push(vadd(c, vadd(vmul(u, Math.cos(a) * rr), vmul(w, Math.sin(a) * rr)))); }
    polyline(pts, lw * 3.5, 'rgb(232,161,74)', 0.3 * k); polyline(pts, lw, 'rgb(255,214,150)', k);
  }
  g.globalCompositeOperation = 'source-over';
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
function resetS() { S.audio = { drone: 0.3, wind: 0.1, hum: 0, humF: 110, birds: 0 }; S.grain = 0.035; S.flash = 0; }
const walkBob = (z, amp = 1) => ({ y: 0.028 * amp * MOTION * Math.sin(z * 6.2), roll: 0.006 * amp * MOTION * Math.sin(z * 3.1) });

// ================================================================== ESCENAS
// ---- el sol y el eclipse (2D, en el cielo) --------------------------------
function corona(x, y, r, a = 1) {
  g.globalCompositeOperation = 'lighter';
  const gr = g.createRadialGradient(x, y, r * 0.95, x, y, r * 3.4);
  gr.addColorStop(0, 'rgba(225,235,255,' + 0.95 * a + ')'); gr.addColorStop(0.18, 'rgba(190,205,240,' + 0.45 * a + ')');
  gr.addColorStop(1, 'rgba(160,180,230,0)');
  g.fillStyle = gr; g.beginPath(); g.arc(x, y, r * 3.4, 0, 6.283); g.fill();
  g.lineCap = 'round';
  for (let i = 0; i < 46; i++) {
    const an = i / 46 * 6.283 + hash(i, 4, 4) * 0.12, len = r * (1.25 + 2.3 * Math.pow(hash(i, 5, 5), 2));
    g.strokeStyle = 'rgba(215,228,255,' + (0.1 + 0.18 * hash(i, 6, 6)) * a + ')'; g.lineWidth = Math.max(0.6, r * 0.05);
    g.beginPath(); g.moveTo(x + Math.cos(an) * r, y + Math.sin(an) * r); g.lineTo(x + Math.cos(an) * len, y + Math.sin(an) * len); g.stroke();
  }
  g.globalCompositeOperation = 'source-over';
  g.fillStyle = '#000'; g.beginPath(); g.arc(x, y, r, 0, 6.283); g.fill();
  g.strokeStyle = 'rgba(240,246,255,' + 0.9 * a + ')'; g.lineWidth = Math.max(1, r * 0.04);
  g.beginPath(); g.arc(x, y, r, 0, 6.283); g.stroke();
}
// prog: 0 sol entero, 1 totalidad. La luna no se ve de dia: se pinta del color del cielo.
function sunDisk(x, y, r, prog, skyCss) {
  if (prog >= 1) { corona(x, y, r); return; }
  const off = (1 - prog) * 2.1 * r, visible = clamp(off / (2.1 * r));
  g.fillStyle = '#fffaf0'; g.beginPath(); g.arc(x, y, r, 0, 6.283); g.fill();
  g.fillStyle = skyCss; g.beginPath(); g.arc(x + off, y - off * 0.3, r * 1.03, 0, 6.283); g.fill();
  g.globalCompositeOperation = 'lighter';
  const gl = g.createRadialGradient(x, y, r * 0.6, x, y, r * 9);
  gl.addColorStop(0, 'rgba(255,250,235,' + 0.6 * (0.15 + 0.85 * visible) + ')'); gl.addColorStop(1, 'rgba(255,250,235,0)');
  g.fillStyle = gl; g.fillRect(0, 0, W, H);
  g.globalCompositeOperation = 'source-over';
}
function bead(x, y, r, k) { // anillo de diamante
  if (k <= 0) return;
  const bx = x + Math.cos(-0.75) * r, by = y + Math.sin(-0.75) * r, R = r * (0.4 + 5 * k * k);
  g.globalCompositeOperation = 'lighter';
  const gr = g.createRadialGradient(bx, by, 0, bx, by, R);
  gr.addColorStop(0, 'rgba(255,255,255,' + Math.min(1, 0.4 + k) + ')'); gr.addColorStop(0.2, 'rgba(240,246,255,' + 0.6 * k + ')'); gr.addColorStop(1, 'rgba(220,230,255,0)');
  g.fillStyle = gr; g.fillRect(0, 0, W, H);
  g.fillStyle = 'rgba(255,255,255,' + 0.7 * k + ')';
  g.fillRect(bx - R * 1.6, by - 0.7, R * 3.2, 1.4); g.fillRect(bx - 0.7, by - R * 1.2, 1.4, R * 2.4);
  g.globalCompositeOperation = 'source-over';
}

// ---- 0. La capilla y el valle (prologo del GDD) ---------------------------
const ADOBE = [0.82, 0.8, 0.74], PIEDRA = [0.42, 0.39, 0.35], PAJA = [0.55, 0.45, 0.28], PASTO = [0.38, 0.34, 0.24];
const SUN_DIR = vnorm(V(0, Math.sin(0.34), Math.cos(0.34))), CRATER_C = V(0, 0, 78);
const STV = { rise: 0 };
const W_VALLE = (() => {
  const Q = [];
  grid(Q, V(-120, 0, -60), V(240, 0, 0), V(0, 0, 260), 24, 26, { alb: PASTO, amp: 1.4, jit: 0.06 });
  // capilla andina: zocalo, nave encalada, techo de paja a dos aguas, espadana con cruz
  box(Q, 0, 0.4, -15.5, 6.4, 0.8, 11.4, { alb: PIEDRA, jit: 0.15 });
  box(Q, 0, 2.65, -15.5, 6, 3.7, 11, { alb: ADOBE, jit: 0.05 });
  quad(Q, V(-3.3, 4.5, -21.3), V(-3.3, 4.5, -9.7), V(0, 6.8, -9.7), V(0, 6.8, -21.3), { alb: PAJA, jit: 0.15 });
  quad(Q, V(3.3, 4.5, -9.7), V(3.3, 4.5, -21.3), V(0, 6.8, -21.3), V(0, 6.8, -9.7), { alb: PAJA, jit: 0.15 });
  quad(Q, V(-3, 4.5, -9.98), V(3, 4.5, -9.98), V(0, 6.8, -9.98), V(0, 6.8, -9.98), { alb: ADOBE, jit: 0.03 });
  box(Q, 0, 7.6, -10.1, 2.2, 1.8, 0.6, { alb: ADOBE, jit: 0.04 });
  quad(Q, V(-0.3, 7.2, -9.78), V(0.3, 7.2, -9.78), V(0.3, 8.0, -9.78), V(-0.3, 8.0, -9.78), { alb: [0.05, 0.045, 0.04], jit: 0 });
  box(Q, 0, 8.95, -10.1, 0.12, 0.9, 0.12, { alb: [0.3, 0.25, 0.2] }); box(Q, 0, 9.1, -10.1, 0.55, 0.12, 0.12, { alb: [0.3, 0.25, 0.2] });
  quad(Q, V(-0.7, 0.8, -9.96), V(0.7, 0.8, -9.96), V(0.7, 3.1, -9.96), V(-0.7, 3.1, -9.96), { alb: [0.06, 0.05, 0.04], jit: 0 });
  quad(Q, V(0, 3.6, -9.96), V(0.35, 3.95, -9.96), V(0, 4.3, -9.96), V(-0.35, 3.95, -9.96), { alb: [0.06, 0.05, 0.04], jit: 0 });
  for (const x of [-3.35, 3.35]) box(Q, x, 1.5, -10.4, 0.9, 3, 1.3, { alb: ADOBE, jit: 0.05 });
  // atrio: pirca, arco de ingreso, cruz atrial
  for (const x of [-5, 5]) box(Q, x, 0.45, -3, 6.4, 0.9, 0.6, { alb: PIEDRA, jit: 0.2 });
  for (const x of [-1.6, 1.6]) box(Q, x, 1.3, -3, 0.6, 2.6, 0.6, { alb: ADOBE, jit: 0.05 });
  box(Q, 0, 2.8, -3, 3.8, 0.5, 0.6, { alb: ADOBE, jit: 0.05 });
  box(Q, 4.2, 1.6, -6.5, 0.16, 3.2, 0.16, { alb: [0.3, 0.25, 0.2] }); box(Q, 4.2, 2.6, -6.5, 1.1, 0.16, 0.16, { alb: [0.3, 0.25, 0.2] });
  // cardones
  const R = rng(31);
  for (let i = 0; i < 14; i++) {
    const x = (R() < 0.5 ? -1 : 1) * (9 + R() * 40), z = -30 + R() * 90, h = 2 + R() * 3.5, c = { alb: [0.2, 0.27, 0.15], jit: 0.05 };
    box(Q, x, h / 2, z, 0.45, h, 0.45, c);
    if (R() < 0.7) { const ah = h * (0.45 + R() * 0.3); box(Q, x + 0.5, ah, z, 0.6, 0.3, 0.3, c); box(Q, x + 0.7, ah + 0.6, z, 0.3, 1.2, 0.3, c); }
  }
  // el borde del crater, que sube cuando empieza la totalidad
  revolve(Q, [[15, -2], [18.5, 6.5], [22, 4.5], [27, -2]], 40, { alb: ROCK, amp: 1.6, tile: 2.2, jit: 0.05, cx: CRATER_C.x, cz: CRATER_C.z,
    upd: q => { q.a = STV.rise > 0.01 ? 1 : 0; q.off = V(0, (STV.rise - 1) * 8.5, 0); } });
  return Q;
})();
const lerp3 = (a, b, t) => [lerp(a[0], b[0], t), lerp(a[1], b[1], t), lerp(a[2], b[2], t)];
const css3 = c => 'rgb(' + (c[0] * 255 | 0) + ',' + (c[1] * 255 | 0) + ',' + (c[2] * 255 | 0) + ')';
function valle(prog) {
  const day = 1 - smooth(0.5, 1, prog), tot = smooth(0.92, 1, prog);
  // cielo: de dia claro, en la totalidad noche con el horizonte encendido en 360
  const top = lerp3([0.02, 0.03, 0.07], [0.55, 0.66, 0.78], day), hor = lerp3(lerp3([0.2, 0.2, 0.26], [0.36, 0.2, 0.12], tot), [0.86, 0.86, 0.83], day);
  const hp = proj(vadd(cam.p, V(Math.sin(cam.yaw) * 2000, 0, Math.cos(cam.yaw) * 2000)));
  const hy = hp ? hp[1] : H * 0.6, sky = g.createLinearGradient(0, hy - H * 1.2, 0, hy);
  sky.addColorStop(0, css3(top)); sky.addColorStop(1, css3(hor));
  g.fillStyle = sky; g.fillRect(0, 0, W, H);
  const sp = proj(vadd(cam.p, vmul(SUN_DIR, 1000)));
  if (sp) sunDisk(sp[0], sp[1], CT.f * 0.03, prog, css3(lerp3(top, hor, clamp((sp[1] - (hy - H * 1.2)) / (H * 1.2)))));
  // cerros facetados lejanos
  const mc = css3(lerp3([0.03, 0.03, 0.05], [0.55, 0.58, 0.6], day));
  g.fillStyle = mc; g.strokeStyle = mc; g.lineWidth = 0.8;
  for (let i = 0; i < 90; i++) {
    const a0 = i / 90 * 6.283, a1 = (i + 1) / 90 * 6.283, hgt = a => 30 + 38 * Math.abs(Math.sin(a * 3.1)) + 16 * Math.sin(a * 7.3 + 1);
    const P = [V(Math.sin(a0) * 700, -40, Math.cos(a0) * 700), V(Math.sin(a1) * 700, -40, Math.cos(a1) * 700),
               V(Math.sin(a1) * 700, hgt(a1), Math.cos(a1) * 700), V(Math.sin(a0) * 700, hgt(a0), Math.cos(a0) * 700)].map(proj);
    if (P.some(q => !q)) continue;
    g.beginPath(); P.forEach((q, k) => (k ? g.lineTo(q[0], q[1]) : g.moveTo(q[0], q[1]))); g.closePath(); g.fill(); g.stroke();
  }
  const L = [{ p: vmul(SUN_DIR, 3000), col: [1, 0.97, 0.9], int: 1.35 * day, k: 0, range: 9000 },
             { p: vmul(SUN_DIR, 3000), col: ECL, int: 0.05 * tot, k: 0, range: 9000 }];
  const env = { amb: lerp3([0.006, 0.007, 0.012], [0.32, 0.33, 0.36], day), fog: 0.006,
                fogCol: lerp3([0.012, 0.012, 0.02], [0.75, 0.77, 0.78], day) };
  renderWorld(W_VALLE, L, env);
  return { day, tot };
}
function rEclipse(lt) {
  const prog = clamp(lt / 5.2);
  STV.rise = ease(clamp((lt - 5.8) / 3));
  cam.p = V(0.05 * hh(lt * 0.2), 1.6, -1.5); cam.fov = 1.12; cam.roll = 0;
  cam.yaw = 0.015 * hh(lt * 0.3); cam.pitch = key(lt, [[0, 0.32], [5.4, 0.32], [7.4, 0.1], [10.5, 0.07]]);
  setCam();
  const v = valle(prog);
  // destella el contorno de la puerta en el centro del crater
  const k = smooth(8.8, 9.1, lt) * (0.35 + 0.65 * Math.exp(-Math.max(0, lt - 9.1) * 2.5));
  const dp = proj(vadd(CRATER_C, V(0, 1.2, 0)));
  if (k > 0 && dp) {
    g.globalCompositeOperation = 'lighter';
    const R = CT.f / dp[2] * 12, gr = g.createRadialGradient(dp[0], dp[1], 0, dp[0], dp[1], R);
    gr.addColorStop(0, 'rgba(235,242,255,' + k + ')'); gr.addColorStop(0.15, 'rgba(200,215,245,' + 0.5 * k + ')'); gr.addColorStop(1, 'rgba(180,200,240,0)');
    g.fillStyle = gr; g.fillRect(0, 0, W, H);
    g.fillStyle = 'rgba(240,246,255,' + 0.8 * k + ')'; g.fillRect(dp[0] - R * 0.9, dp[1] - 0.5, R * 1.8, 1);
    const arch = []; for (let i = 0; i <= 16; i++) { const a = Math.PI - i / 16 * Math.PI; arch.push(V(CRATER_C.x + Math.cos(a) * 1.2, 2.2 + Math.sin(a) * 1.2, CRATER_C.z)); }
    polyline([V(-1.2, 0, CRATER_C.z), ...arch, V(1.2, 0, CRATER_C.z)], 1.2, 'rgb(240,246,255)', k);
    g.globalCompositeOperation = 'source-over';
  }
  S.audio = { drone: 0.4 * v.tot, wind: 0.12 + 0.1 * v.tot, hum: 0, humF: 110, birds: prog < 1 ? 1 : 0 };
}

// ---- placas ------------------------------------------------------------------
function rBlack() { S.audio = { drone: 0.2, wind: 0.05, hum: 0, humF: 110 }; }

// ---- El Umbral: sala circular con el oculo y la linterna en el piso ---------
const STARS = (() => { const R = rng(55), out = []; while (out.length < 110) { const x = R() * 2 - 1, z = R() * 2 - 1; if (x * x + z * z < 0.92) out.push([x * 5, z * 5, R()]); } return out; })();
const W_UMBRAL = (() => {
  const Q = [], DARK = [0.14, 0.14, 0.15], STONE = [0.52, 0.5, 0.46], BENCH = [0.3, 0.29, 0.27];
  const ring = (prof, alb, extra = {}) => revolve(Q, prof, 24, { alb, amp: 0, tile: 100, jit: 0.06, ...extra });
  ring([[0.3, 0], [12, 0]], [0.08, 0.08, 0.09], { layer: 0, noSpot: true, jit: 0.12 });
  ring([[5.4, 0.01], [6.2, 0.01]], [0.34, 0.34, 0.35], { layer: 0.5, noSpot: true });
  ring([[0.3, 0.012], [2.4, 0.012]], [0.42, 0.42, 0.43], { layer: 0.5, noSpot: true });
  ring([[8.8, 0.01], [9.3, 0.01]], [0.24, 0.24, 0.25], { layer: 0.5, noSpot: true });
  ring([[9.6, 0], [9.6, 0.5]], BENCH); ring([[9.6, 0.5], [10.8, 0.5]], BENCH);
  ring([[10.8, 0.5], [10.8, 1.0]], BENCH); ring([[10.8, 1.0], [12, 1.0]], BENCH);
  ring([[12, 1.0], [12, 9.2]], DARK);
  ring([[11.6, 8.6], [11.6, 9.4]], [0.36, 0.35, 0.33]); ring([[11.6, 9.4], [12, 9.4]], [0.36, 0.35, 0.33]);
  ring([[12, 9.4], [8.5, 11.8], [5, 13.6]], DARK.map(v => v * 0.9));
  ring([[5, 13.6], [5, 15]], STONE);
  for (let k = 0; k < 8; k++) {
    const a = k / 8 * 6.283 + Math.PI / 8;
    obox(Q, Math.cos(a) * 11.4, Math.sin(a) * 11.4, a + Math.PI / 2, 0.9, 0.8, 1.0, 9.2, { alb: STONE, jit: 0.05 });
    const b = k / 8 * 6.283, dA = 0.05, R2 = 11.75;   // puerta oscura entre pilastras
    quad(Q, V(Math.cos(b - dA) * R2, 1.0, Math.sin(b - dA) * R2), V(Math.cos(b + dA) * R2, 1.0, Math.sin(b + dA) * R2),
            V(Math.cos(b + dA) * R2, 3.4, Math.sin(b + dA) * R2), V(Math.cos(b - dA) * R2, 3.4, Math.sin(b - dA) * R2), { alb: [0.015, 0.015, 0.015], jit: 0 });
  }
  box(Q, 0, 0.09, 0, 0.5, 0.16, 0.16, { alb: [0.24, 0.24, 0.25], jit: 0.05 });    // la linterna
  box(Q, 0.3, 0.12, 0, 0.14, 0.24, 0.24, { alb: [0.2, 0.2, 0.21], jit: 0.05 });
  return Q;
})();
function rUmbral(lt) {
  const tilt = ease(clamp((lt - 0.5) / 3.6));
  cam.p = V(0.05 * hh(lt * 0.2), 1.7, -9 + 1.2 * ease(clamp(lt / 6.2))); cam.roll = 0; cam.fov = 1.15;
  cam.yaw = 0.01 * hh(lt * 0.3); cam.pitch = lerp(0.98, -0.17, tilt);
  setCam();
  // cielo nocturno por el oculo
  const rim = []; for (let i = 0; i < 48; i++) { const a = i / 48 * 6.283; rim.push(proj(V(Math.cos(a) * 5, 15, Math.sin(a) * 5))); }
  const ctr = proj(V(0, 15.05, 0));
  if (ctr && rim.every(Boolean)) {
    const R = Math.max(...rim.map(p => Math.hypot(p[0] - ctr[0], p[1] - ctr[1])));
    const gr = g.createRadialGradient(ctr[0], ctr[1], 0, ctr[0], ctr[1], R);
    gr.addColorStop(0, '#1a3688'); gr.addColorStop(1, '#0c1d56');
    g.fillStyle = gr; g.beginPath(); rim.forEach((p, i) => (i ? g.lineTo(p[0], p[1]) : g.moveTo(p[0], p[1]))); g.closePath(); g.fill();
    for (const [x, z, b] of STARS) { const sp = proj(V(x, 15.04, z)); if (!sp) continue; const sz = b > 0.85 ? 1.6 : 1;
      g.fillStyle = 'rgba(235,240,255,' + (0.45 + 0.55 * b) + ')'; g.fillRect(sp[0], sp[1], sz, sz); }
  }
  const moon = { par: true, d: vnorm(V(0.25, -1, 0.35)), col: [0.78, 0.82, 0.95], int: 0.52 };
  const L = [moon];
  let F = null;
  if (lt >= 4.4) {
    const fl = lt < 4.6 ? (hash(Math.floor(lt * 40), 3, 3) > 0.4 ? 1 : 0.15) : 1;
    F = { p: V(0.42, 0.12, 0), dir: vnorm(V(0.55, -0.012, 1)), col: WARM, int: 5 * fl, k: 0.1, range: 16,
          cosO: Math.cos(14 * Math.PI / 180), cosI: Math.cos(8 * Math.PI / 180), cosS: Math.cos(30 * Math.PI / 180), ang: 28 };
    L.push(F);
  }
  renderWorld(W_UMBRAL, L, { amb: [0.012, 0.012, 0.016], fog: 0.008, onFloor: () => floorPool(F, 0.015, 0.5 * (F ? F.int / 5 : 0)) });
  S.audio = { drone: 0.34, wind: 0.22, hum: 0, humF: 110 };
}

// ---- La sala de los monolitos -----------------------------------------------
const W_SALA = (() => {
  const Q = [], R = rng(41), CREAM = [0.66, 0.64, 0.6];
  grid(Q, V(-16, 0, -4), V(32, 0, 0), V(0, 0, 56), 8, 14, { alb: [0.22, 0.21, 0.2], layer: 0, noSpot: true, jit: 0.06 });
  for (let i = 0; i < 14; i++) {
    const side = i % 2 ? 1 : -1, x = side * (3.2 + R() * 4), z = 2 + i * 3.1 + R() * 2, h = 0.25 + R() * 0.5;
    box(Q, x, h / 2, z, 3 + R() * 3, h, 2 + R() * 3, { alb: [0.27, 0.26, 0.25], jit: 0.05 });
  }
  for (let i = 0; i < 8; i++) for (const side of [-1, 1]) {
    prism(Q, side * (6.5 + R() * 3.5), 5 + i * 5.5 + R() * 2, 1.2 + R() * 1.1, R() < 0.5 ? 4 : 5, 0, 10 + R() * 16, (R() - 0.5) * 1.6,
      { alb: CREAM, jit: 0.04, seed: 100 + i * 2 + (side > 0 ? 1 : 0) });
  }
  for (let st = 0; st < 8; st++) box(Q, 0, (st + 1) * 0.1, 39 + st * 0.55, 6, (st + 1) * 0.2, 0.55, { alb: [0.3, 0.29, 0.27], jit: 0.03 });
  box(Q, 0, 0.8, 46.5, 10, 1.6, 6, { alb: [0.3, 0.29, 0.27], jit: 0.03 });
  box(Q, 0, 3.8, 47.6, 3.6, 4.4, 1.2, { alb: [0.2, 0.19, 0.18], jit: 0.03 });
  quad(Q, V(-0.75, 1.62, 46.98), V(0.75, 1.62, 46.98), V(0.75, 4.3, 46.98), V(-0.75, 4.3, 46.98), { alb: [0.01, 0.01, 0.01], jit: 0, em: AMBER.map(v => v * 0.04) });
  const frame = { alb: AMBER, em: AMBER.map(v => v * 0.55), jit: 0 };
  box(Q, -0.85, 2.96, 46.94, 0.12, 2.7, 0.08, frame); box(Q, 0.85, 2.96, 46.94, 0.12, 2.7, 0.08, frame); box(Q, 0, 4.36, 46.94, 1.82, 0.12, 0.08, frame);
  // rocas flotantes: las caras de abajo brillan en ambar
  [[-6.8, 6.6, 15, 1.2], [7.4, 7.8, 18, 1.0], [-2.6, 6.2, 21, 0.32], [2.4, 7.4, 25, 0.3], [-2.1, 4.1, 29, 0.3], [3.3, 3.8, 32, 0.42], [-6.5, 9, 30, 0.8]]
    .forEach(([x, y, z, r], k) => icosa(Q, V(x, y, z), r, 60 + k, { alb: [0.06, 0.055, 0.05], jit: 0.03 })
      .forEach(q => { if (q.on.y < -0.45) q.em = GLOW.map(v => v * 0.5); }));
  return Q;
})();
function rSala(lt) {
  cam.p = V(0.5, 1.6, 1.5 + lt * 1.3); cam.roll = 0.004 * hh(lt); cam.fov = 1.2;
  cam.yaw = -0.03 + 0.012 * hh(lt * 0.5); cam.pitch = 0.05 + 0.01 * hh(lt * 0.4 + 2);
  setCam();
  const key = { par: true, d: vnorm(V(0.55, -0.5, 0.65)), col: [1, 0.96, 0.9], int: 1.0 };
  const F = flashlight(FILTROS.cuerpo, 1, -0.015, -0.02); F.range = 34;
  renderWorld(W_SALA, [key, F], { amb: [0.004, 0.004, 0.005], fog: 0.01, onFloor: () => floorPool(F, 0.012, 0.7) });
  const dp = proj(V(0, 3, 46.9));
  if (dp) { g.globalCompositeOperation = 'lighter'; const R = CT.f * 3 / dp[2], gr = g.createRadialGradient(dp[0], dp[1], 0, dp[0], dp[1], R);
    gr.addColorStop(0, 'rgba(232,161,74,0.35)'); gr.addColorStop(1, 'rgba(232,161,74,0)'); g.fillStyle = gr; g.fillRect(0, 0, W, H); g.globalCompositeOperation = 'source-over'; }
  S.audio = { drone: 0.34, wind: 0.12, hum: 0.1, humF: 110 };
}

// ---- CUERPO de cerca: el haz enciende el ancla -------------------------------
const ANCLA_A = V(0, 2.3, 6.2), LAMP_A = V(-10, 2.6, 1.2), TO_LAMP_A = vnorm(vsub(LAMP_A, ANCLA_A));
const STA = { k: 0 };
const W_ANCLA = (() => {
  const Q = [], BR = [0.2, 0.15, 0.1];
  prism(Q, 0.5, 8.4, 1.9, 5, -8, 14, 0.8, { alb: BR, seed: 201 });
  prism(Q, -2.0, 7.7, 1.1, 4, -8, 11, -1, { alb: BR, seed: 202 });
  prism(Q, 2.3, 7.5, 1.2, 5, -8, 12.5, 1.2, { alb: BR, seed: 203 });
  prism(Q, -0.4, 6.9, 0.85, 4, -8, 1.0, 0.5, { alb: BR, seed: 204 });
  prism(Q, 0.8, 6.8, 0.75, 4, 3.7, 12, 0.5, { alb: BR, seed: 205 });
  icosa(Q, ANCLA_A, 1.05, 77, { alb: [0.07, 0.06, 0.05], jit: 0.03, sy: 1.35 }).forEach(q => {
    q.upd = q2 => { const d = q2.on.x * TO_LAMP_A.x + q2.on.y * TO_LAMP_A.y + q2.on.z * TO_LAMP_A.z;
      q2.em = d > 0.2 ? GLOW.map(v => v * (0.03 + 0.9 * STA.k) * d * d) : [0, 0, 0]; };
  });
  return Q;
})();
function rAnclaA(lt) {
  const push = ease(clamp(lt / 3.4));
  cam.p = V(-1.9 + 0.3 * push, 2.4, -0.4 + 0.9 * push); cam.roll = 0; cam.fov = 1.05;
  const tg = V(-0.7, 2.5, 6.2), d = vsub(tg, cam.p);
  cam.yaw = Math.atan2(d.x, d.z) + 0.008 * hh(lt); cam.pitch = Math.atan2(d.y, Math.hypot(d.x, d.z));
  setCam();
  STA.k = clamp((lt - 0.9) / 0.35);                 // carga real del receptor: 0,35 s
  const on = lt >= 0.3 ? (lt < 0.45 ? 0.5 : 1) : 0;
  const L = [{ p: LAMP_A, dir: vmul(TO_LAMP_A, -1), col: AMBER, int: 3.5 * on, k: 0.02, range: 25,
               cosO: Math.cos(6 * Math.PI / 180), cosI: Math.cos(3.5 * Math.PI / 180) },
             { p: ANCLA_A, col: AMBER, int: 0.45 * STA.k, k: 0.8, range: 5 }];
  renderWorld(W_ANCLA, L, { amb: [0.003, 0.0025, 0.002], fog: 0.01 });
  if (on) beamVolume(LAMP_A, vadd(ANCLA_A, vmul(TO_LAMP_A, 0.9)), 0.12, 0.55, AMBER, 0.32 * on);
  glyphRings(vadd(ANCLA_A, vmul(TO_LAMP_A, 0.98)), TO_LAMP_A, 0.42, smooth(1.2, 1.5, lt));
  S.audio = { drone: 0.3, wind: 0.08, hum: on ? 0.1 : 0, humF: 110 };
}

// ---- CUERPO: el puente entre dos anclas --------------------------------------
const ANCLA_PA = V(-5.2, 6.9, 25.3), ANCLA_PB = V(4.4, 5.1, 10.1), LAMP_B = V(9, 3, 2.5);
const PA = V(-4.5, 6.1, 24.6), PB = V(3.6, 4.5, 10.9);
const STB = { b: 0, br: 0 };
const W_PUENTE = (() => {
  const Q = [], BR = [0.17, 0.13, 0.09];
  prism(Q, -6.4, 26.4, 1.9, 5, -30, 9.5, 1, { alb: BR, seed: 301 });
  prism(Q, -4.3, 27.6, 1.3, 4, -30, 7.2, -0.8, { alb: BR, seed: 302 });
  prism(Q, -7.8, 24.2, 1.0, 4, -30, 11, 0.6, { alb: BR, seed: 303 });
  prism(Q, 5.6, 10.6, 1.8, 5, -30, 7.5, 1.2, { alb: BR, seed: 311 });
  prism(Q, 7.3, 12.8, 1.2, 4, -30, 9.5, -1, { alb: BR, seed: 312 });
  prism(Q, 4.3, 8.4, 1.0, 5, -30, 3.8, 0.5, { alb: BR, seed: 313 });
  [[-1, 34, 1.4, 4], [4, 30, 1.1, 6], [9, 22, 1.3, 3], [-10, 16, 1.5, 5], [1.2, 20, 0.9, -2]]
    .forEach(([x, z, r, h], k) => prism(Q, x, z, r, 4 + k % 2, -30, h, 0.8, { alb: BR.map(v => v * 0.8), seed: 320 + k }));
  const toB = vnorm(vsub(PB, ANCLA_PA)), toL = vnorm(vsub(LAMP_B, ANCLA_PB));
  icosa(Q, ANCLA_PA, 0.85, 91, { alb: [0.07, 0.06, 0.05], jit: 0.03 }).forEach(q => {
    const d = q.on.x * toB.x + q.on.y * toB.y + q.on.z * toB.z; q.em = d > 0.2 ? GLOW.map(v => v * 0.9 * d * d) : [0, 0, 0]; });
  icosa(Q, ANCLA_PB, 1.0, 92, { alb: [0.07, 0.06, 0.05], jit: 0.03 }).forEach(q => {
    q.upd = q2 => { const d = q2.on.x * toL.x + q2.on.y * toL.y + q2.on.z * toL.z; q2.em = d > 0.2 ? GLOW.map(v => v * (0.03 + 0.9 * STB.b) * d * d) : [0, 0, 0]; }; });
  return Q;
})();
const BRIDGE = (() => {
  const R = rng(17), N = 9, D = vsub(PB, PA), S2 = vmul(vnorm(vcross(D, V(0, 1, 0))), 1.1), Lr = [], Rr = [], tris = [];
  for (let i = 0; i <= N; i++) {
    const c = vadd(PA, vmul(D, i / N)), j = () => V((R() - 0.5) * 0.25, (R() - 0.5) * 0.18, (R() - 0.5) * 0.25);
    Lr.push(vadd(vsub(c, S2), j())); Rr.push(vadd(vadd(c, S2), j()));
  }
  for (let i = 0; i < N; i++) { tris.push([Lr[i], Rr[i], Lr[i + 1]]); tris.push([Rr[i], Rr[i + 1], Lr[i + 1]]); }
  return tris;
})();
function rAnclaB(lt) {
  cam.p = V(0.8, 11, 1.2 + lt * 0.3); cam.roll = 0; cam.fov = 1.1;
  const d = vsub(V(-1.6, 4.2, 20), cam.p);
  cam.yaw = Math.atan2(d.x, d.z) + 0.006 * hh(lt); cam.pitch = Math.atan2(d.y, Math.hypot(d.x, d.z));
  setCam();
  STB.b = clamp((lt - 0.6) / 0.35); STB.br = clamp((lt - 1.0) / 1.8);
  const toL = vnorm(vsub(LAMP_B, ANCLA_PB)), on = lt >= 0.2 ? 1 : 0;
  const mid = vmul(vadd(PA, PB), 0.5);
  const L = [{ p: LAMP_B, dir: vmul(toL, -1), col: AMBER, int: 6 * on, k: 0.02, range: 25,
               cosO: Math.cos(8 * Math.PI / 180), cosI: Math.cos(4 * Math.PI / 180), cosS: Math.cos(18 * Math.PI / 180) },
             { p: ANCLA_PA, col: AMBER, int: 1.4, k: 0.25, range: 9 },
             { p: ANCLA_PB, col: AMBER, int: 1.4 * STB.b, k: 0.25, range: 9 },
             { p: mid, col: AMBER, int: 1.2 * STB.br, k: 0.03, range: 14 }];
  renderWorld(W_PUENTE, L, { amb: [0.003, 0.0025, 0.002], fog: 0.012 });
  if (on) beamVolume(LAMP_B, vadd(ANCLA_PB, vmul(toL, 1.0)), 0.3, 0.7, AMBER, 0.3);
  // paneles triangulados que se arman de A hacia B
  const n = BRIDGE.length;
  for (let j = 0; j < n; j++) {
    const v = clamp(STB.br * (n + 4) - j, 0, 1); if (v <= 0) continue;
    const ps = BRIDGE[j].map(proj); if (ps.some(q => !q)) continue;
    g.beginPath(); ps.forEach((q, i) => (i ? g.lineTo(q[0], q[1]) : g.moveTo(q[0], q[1]))); g.closePath();
    g.fillStyle = 'rgba(232,161,74,' + (0.5 * v) + ')'; g.fill();
    g.strokeStyle = 'rgba(255,208,140,' + (0.95 * v) + ')'; g.lineWidth = 1.2; g.stroke();
  }
  // fragmentos que flotan en el frente del puente
  if (STB.br > 0 && STB.br < 1) {
    const front = vadd(PA, vmul(vsub(PB, PA), Math.min(1, STB.br * 1.1)));
    for (let i = 0; i < 30; i++) {
      const q = proj(vadd(front, V((hash(i, 1, 2) - 0.5) * 3, (hash(i, 2, 3) - 0.4) * 2 + lt * 0.3 * hash(i, 4, 4), (hash(i, 3, 1) - 0.5) * 3)));
      if (!q) continue; const sz = clamp(CT.f * 0.08 / q[2], 1, 4);
      g.fillStyle = 'rgba(245,175,85,' + (0.5 + 0.5 * hash(i, 5, 5)) + ')'; g.fillRect(q[0], q[1], sz, sz);
    }
  }
  glyphRings(vadd(ANCLA_PA, vmul(vnorm(vsub(PB, ANCLA_PA)), 0.84)), vnorm(vsub(PB, ANCLA_PA)), 0.34, 1);
  glyphRings(vadd(ANCLA_PB, vmul(toL, 0.98)), toL, 0.4, smooth(0.95, 1.2, lt));
  S.audio = { drone: 0.36, wind: 0.1, hum: on ? 0.1 : 0, humF: 110 };
}

// ---- 6. La Cresta: pared espejo, oscuridad y la puerta del eclipse --------
const W_ADAPT = (() => {
  const Q = [];
  grid(Q, V(-10, 0, -2), V(20, 0, 0), V(0, 0, 14), 20, 14, { alb: ASH, amp: 0.15 });
  grid(Q, V(-10, 0, 12), V(20, 0, 0), V(0, 14, 0), 28, 18, { alb: [0.13, 0.125, 0.12], amp: 0.02, jit: 0.08 }); // basalto pulido
  grid(Q, V(-10, 0, -2), V(0, 0, 14), V(0, 14, 0), 12, 11, { alb: ROCK, amp: 0.4 });
  grid(Q, V(10, 0, -2), V(0, 14, 0), V(0, 0, 14), 11, 12, { alb: ROCK, amp: 0.4 });
  grid(Q, V(-10, 14, -2), V(0, 0, 14), V(20, 0, 0), 8, 12, { alb: ROCK, amp: 0.3 });
  for (const x of [-7.5, 7.5]) for (const z of [2.5, 6.5, 10.5]) box(Q, x, 7, z, 1.1, 14, 1.1, { alb: [0.26, 0.24, 0.22], jit: 0.12 });
  return Q;
})();
const WALL_Z = 12, DOOR_R = 2.4, DOOR_TOP = 6.4;
const archPts = (r, top, z) => { const p = [V(-r, 0.3, z), V(-r, top, z)];
  for (let i = 1; i < 18; i++) { const a = Math.PI - i / 18 * Math.PI; p.push(V(Math.cos(a) * r, top + Math.sin(a) * r, z)); }
  p.push(V(r, top, z), V(r, 0.3, z)); return p; };
// tallados latentes (azul) y el contorno de la puerta (plateado): espiral, serpiente escalonada, chakana, rombo
const DOOR = (() => {
  const Z = WALL_Z - 0.05, lines = [];
  lines.push({ pts: archPts(DOOR_R, DOOR_TOP, Z), em: 0.0032, col: ECL }, { pts: archPts(2.95, DOOR_TOP, Z), em: 0.0024, col: ECL });
  const espiral = (cx, cy, s2) => { const p = []; for (let i = 0; i <= 70; i++) { const a = i / 70 * 4.5 * Math.PI, r = s2 * i / 70; p.push(V(cx + Math.cos(a) * r, cy + Math.sin(a) * r, Z)); } return p; };
  lines.push({ pts: espiral(-5.6, 4.4, 1.2), em: 0.003, col: BLUE }, { pts: espiral(5.6, 4.4, 1.2), em: 0.003, col: BLUE });
  const chak = [[1, 3], [1, 2], [2, 2], [2, 1], [3, 1], [3, -1], [2, -1], [2, -2], [1, -2], [1, -3], [-1, -3], [-1, -2], [-2, -2], [-2, -1], [-3, -1], [-3, 1], [-2, 1], [-2, 2], [-1, 2], [-1, 3], [1, 3]];
  lines.push({ pts: chak.map(([x, y]) => V(x * 0.42, 10.4 + y * 0.42, Z)), em: 0.0034, col: BLUE });
  const circ = []; for (let i = 0; i <= 24; i++) { const a = i / 24 * 6.283; circ.push(V(Math.cos(a) * 0.35, 10.4 + Math.sin(a) * 0.35, Z)); }
  lines.push({ pts: circ, em: 0.006, col: ECL });
  const escal = (x0, x1, y) => { const p = []; let up = false; for (let x = x0; x <= x1 + 1e-6; x += 0.55) { p.push(V(x, y + (up ? 0.5 : 0), Z)); up = !up; p.push(V(x, y + (up ? 0.5 : 0), Z)); } return p; };
  lines.push({ pts: escal(-9.4, -3.6, 1.1), em: 0.0028, col: BLUE }, { pts: escal(3.6, 9.4, 1.1), em: 0.0028, col: BLUE });
  for (const x of [-5.6, 5.6]) for (const s2 of [0.7, 0.35]) lines.push({ pts: [V(x, 7.9 + s2, Z), V(x + s2, 7.9, Z), V(x, 7.9 - s2, Z), V(x - s2, 7.9, Z), V(x, 7.9 + s2, Z)], em: 0.003, col: BLUE });
  lines.push({ pts: [V(-9.8, 0.4, Z), V(9.8, 0.4, Z)], em: 0.0015, col: ECL });
  return lines;
})();
// GDD 3.4: apagada, tras una demora la exposicion sube; al 90 % se abre la puerta
const adaptProgress = lt => clamp((lt - 4.0) / 6.0);
function rAdapt(lt) {
  const back = ease(clamp((lt - 3.5) / 6)), fwd = ease(clamp((lt - 10.2) / 2.8));
  cam.p = V(0, 1.65, 1 - 1.5 * back + 3.2 * fwd); cam.fov = 1.2; cam.roll = 0;
  cam.yaw = (lt < 3 ? 0.16 * Math.sin(lt * 1.2) : 0) + 0.03 * hh(lt * 0.4);
  cam.pitch = key(lt, [[0, 0.06], [3, 0.08], [10, 0.26], [13, 0.2]]) + 0.015 * hh(lt * 0.5);
  setCam();
  const p = adaptProgress(lt), curve = smooth(0, 1, p), open = smooth(0.9, 1, p) * smooth(9.4, 10.6, lt);
  const on = lt < 3.0;
  const L = on ? [flashlight(FILTROS.none, 1, 0.04 * hh(lt + 3), -0.02)] : [];
  L.push({ p: V(0, 3, 11.5), col: ECL, int: 0.004 + 0.4 * open, k: 0.02, range: 22 });
  const E = Math.pow(2, 5 * curve), sat = lerp(1, 0.22, curve);
  const env = { amb: [0.0035, 0.0036, 0.0042], fog: 0.02, E, sat };
  renderWorld(W_ADAPT, L, env);
  g.globalCompositeOperation = 'lighter';
  // pared espejo: con la linterna prendida devuelve el reflejo encandilante del propio foco
  if (on) {
    const img = proj(V(L[0].p.x, L[0].p.y, 2 * WALL_Z - L[0].p.z));
    if (img) {
      const R = CT.f * 1.3 / img[2], gr = g.createRadialGradient(img[0], img[1], 0, img[0], img[1], R * 4);
      gr.addColorStop(0, 'rgba(255,250,238,1)'); gr.addColorStop(0.08, 'rgba(255,244,222,0.85)'); gr.addColorStop(0.3, 'rgba(255,236,205,0.18)'); gr.addColorStop(1, 'rgba(255,236,205,0)');
      g.fillStyle = gr; g.fillRect(0, 0, W, H);
    }
  }
  for (const ln of DOOR) {
    const c = tone(ln.col[0] * ln.em, ln.col[1] * ln.em, ln.col[2] * ln.em, E, sat); if (c[0] + c[1] + c[2] < 3) continue;
    const s0 = proj(ln.pts[0]); if (!s0) continue; const lw = 0.07 * CT.f / s0[2];
    polyline(ln.pts, lw * 4, rgb(c), 0.18); polyline(ln.pts, lw, rgb(c), 0.95);
  }
  // adaptado, la hoja desaparece: detras, la luz plateada del eclipse
  if (open > 0) {
    const P = archPts(DOOR_R, DOOR_TOP, WALL_Z - 0.06).map(proj), c = proj(V(0, 3.4, WALL_Z));
    if (c && P.every(Boolean)) {
      const R = CT.f * 6 / c[2], gr = g.createRadialGradient(c[0], c[1], 0, c[0], c[1], R);
      gr.addColorStop(0, 'rgba(240,246,255,' + 0.95 * open + ')'); gr.addColorStop(1, 'rgba(170,190,235,' + 0.35 * open + ')');
      g.fillStyle = gr; g.beginPath(); P.forEach((q, i) => (i ? g.lineTo(q[0], q[1]) : g.moveTo(q[0], q[1]))); g.closePath(); g.fill();
      const halo = g.createRadialGradient(c[0], c[1], 0, c[0], c[1], R * 2.4);
      halo.addColorStop(0, 'rgba(200,215,250,' + 0.25 * open + ')'); halo.addColorStop(1, 'rgba(200,215,250,0)');
      g.fillStyle = halo; g.fillRect(0, 0, W, H);
    }
  }
  g.globalCompositeOperation = 'source-over';
  if (on) haze(L[0]);
  S.grain = 0.05 + 0.16 * curve;
  S.audio = { drone: 0.26 * (1 - 0.5 * curve), wind: 0.05 + curve * 0.6 + open * 0.2, hum: 0, humF: 110 };
}

// ---- 7. Anillo de diamante --------------------------------------------------
function rDiamante(lt) {
  const x = W / 2, y = H * 0.48, r = H * 0.1;
  const a = smooth(0, 1.2, lt);
  corona(x, y, r, a);
  bead(x, y, r, smooth(1.7, 3.3, lt));
  g.fillStyle = '#000'; g.beginPath(); g.arc(x, y, r * 0.99, 0, 6.283); g.fill();
  S.flash = smooth(2.7, 3.7, lt);
  S.audio = { drone: 0, wind: 0, hum: 0, humF: 110 };
}

// ---- 9. Cierre -------------------------------------------------------------
function rFinal(lt) {
  const gl = g.createRadialGradient(W / 2, H * 0.42, 0, W / 2, H * 0.42, H * 0.7);
  const a = smooth(0, 2, lt) * (1 - smooth(7.6, 9, lt));
  gl.addColorStop(0, 'rgba(170,190,235,' + (0.1 * a) + ')'); gl.addColorStop(1, 'rgba(170,190,235,0)');
  g.fillStyle = gl; g.fillRect(0, 0, W, H);
  g.globalCompositeOperation = 'lighter';
  for (let i = 0; i < 140; i++) {
    const x = (hash(i, 1, 1) * W + lt * (6 + hash(i, 2, 2) * 10)) % W, y = (hash(i, 3, 3) * H - lt * (3 + hash(i, 4, 4) * 6) + H * 4) % H;
    g.fillStyle = 'rgba(237,214,180,' + (0.12 + 0.3 * hash(i, 5, 5)) * a + ')'; g.fillRect(x, y, 1.2, 1.2);
  }
  g.globalCompositeOperation = 'source-over';
  S.audio = { drone: 0.4 * (1 - smooth(7, 9, lt)), wind: 0.12, hum: 0, humF: 110 };
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
  gr.addColorStop(0, 'rgba(170,190,235,0)'); gr.addColorStop(0.3, 'rgba(170,190,235,' + 0.16 * a + ')'); gr.addColorStop(1, 'rgba(170,190,235,0)');
  m.fillStyle = gr; m.beginPath(); m.arc(cx, cy, r * 1.3, 0, 6.283); m.fill();
  m.strokeStyle = 'rgba(225,235,255,' + 0.5 * a + ')'; m.lineWidth = Math.max(1, DPR);
  m.beginPath(); m.arc(cx, cy, r, 0, 6.283); m.stroke();
}
function oFinal(lt) {
  const u = VR.h / 100, cx = VR.x + VR.w / 2, cy = VR.y + VR.h * 0.46;
  const out = 1 - smooth(4.3, 5, lt), a = smooth(0.05, 0.6, lt) * out;
  ring(cx, cy, VR.h * (0.27 + 0.02 * smooth(0, 5, lt)), a);
  drawTitle('CRATER', cx, cy, u * 17, lerp(0.9, 0.42, ease(clamp(lt / 3))), a,
    { x: VR.x + VR.w * lerp(0.15, 0.55, ease(clamp((lt - 0.2) / 2))), y: cy, r: VR.h * (0.3 + 2.5 * smooth(1.8, 3.2, lt)) }, 'rgba(232,161,74,0.8)');
  const t1 = smooth(2.2, 3.0, lt) * out;
  if (t1 > 0) { m.fillStyle = 'rgba(237,234,226,' + t1 + ')'; m.font = 'italic 400 ' + (u * 5) + 'px ' + SERIF; m.textAlign = 'center'; m.textBaseline = 'middle';
    m.fillText('Lo que la luz tapa.', cx, cy + u * 15 + (1 - t1) * u); }
}
function oCoda(lt) {
  const u = VR.h / 100, a = smooth(0.55, 1.0, lt) * (1 - smooth(1.6, 2.0, lt));
  if (a <= 0) return;
  m.font = '400 ' + (u * 2.2) + 'px ' + MONO; m.fillStyle = 'rgba(237,234,226,' + 0.8 * a + ')'; m.textBaseline = 'middle';
  spaced(m, 'PRÓXIMAMENTE', VR.x + VR.w / 2, VR.y + VR.h / 2, u * 1.1);
}
function rCard() { S.audio = { drone: 0, wind: 0, hum: 0, humF: 110 }; } // silencio antes del golpe

// ------------------------------------------------------------------ linea de tiempo
const mapAdapt = lt => (lt < 1.8 ? lt + 1.2 : 3.0 + (lt - 1.8) * 1.8);   // apaga en 1,8 s; la puerta se abre a los ~5,4 s
const card = dur => ({ dur, render: rCard, card: true });
const SC = {
  eclipse: { dur: 5.2, render: lt => rEclipse(2.0 + lt * 1.4), fadeIn: 0.8, fadeOut: 0.5 },
  placa1:  card(2.6),
  umbral:  { dur: 6.2, render: rUmbral, fadeIn: 0.6, fadeOut: 0.3 },
  sala:    { dur: 5,   render: rSala, fadeIn: 0.3, fadeOut: 0.3 },
  anclaA:  { dur: 3.4, render: rAnclaA, fadeIn: 0.2 },
  anclaB:  { dur: 4.4, render: rAnclaB, fadeOut: 0.4 },
  placa2:  card(2.8),
  adapt:   { dur: 6.5, render: lt => rAdapt(mapAdapt(lt)), fadeIn: 0.3, fadeOut: 0.4 },
  diamante:{ dur: 2.2, render: lt => rDiamante(1.2 + lt * 1.2), fadeIn: 0.3 },
  final:   { dur: 4,   render: rFinal, over: lt => oFinal(lt * 1.25) },
  coda:    { dur: 1.6, render: rBlack, over: lt => oCoda(lt * 1.25) },
};
const ORDER = Object.keys(SC);
let TOTAL = 0; for (const id of ORDER) { SC[id].start = TOTAL; TOTAL += SC[id].dur; }
const sceneAt = t => { for (let i = ORDER.length - 1; i >= 0; i--) { const s = SC[ORDER[i]]; if (t >= s.start) return [s, t - s.start]; } return [SC[ORDER[0]], 0]; };

const CUES = [];
const cue = (id, t0, t1, text, style) => CUES.push({ t0: SC[id].start + t0, t1: SC[id].start + t1, text, style });
cue('placa1', 0.35, SC.placa1.dur - 0.1, 'Cuando el sol se apaga,\naparece un cráter.', 'card');
cue('placa2', 0.35, SC.placa2.dur - 0.1, 'Pero hay una puerta\nque la luz no deja ver.', 'card');
cue('adapt', 0.2, 1.75, 'Apagala.', 'big');

function drawCue(c, t) {
  const a = smooth(c.t0, c.t0 + 0.5, t) * (1 - smooth(c.t1 - 0.5, c.t1, t)); if (a <= 0) return;
  const u = VR.h / 100, cx = VR.x + VR.w / 2;
  m.textBaseline = 'middle'; m.textAlign = 'center'; m.fillStyle = 'rgba(237,234,226,' + a + ')';
  if (c.style === 'card') {
    const lines = c.text.split('\n'), lh = u * 7.2, y0 = VR.y + VR.h / 2 - (lines.length - 1) * lh / 2;
    m.font = '400 ' + (u * 5.4) + 'px ' + SERIF;
    lines.forEach((l, i) => m.fillText(l, cx, y0 + i * lh));
  } else {
    m.font = 'italic 400 ' + (u * 10) + 'px ' + SERIF;
    m.fillText(c.text, cx, VR.y + VR.h / 2);
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
  const gk = Math.floor(t * 24); m.translate(hash(gk, 1, 7) * 192, hash(gk, 2, 9) * 192);
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
    if (a.birds && Math.random() < a.birds * 0.045) this.chirp();
  },
  chirp() { // pajaros de la capilla
    const C = this.C, t0 = C.currentTime, f0 = 2300 + Math.random() * 2000, n = 2 + Math.floor(Math.random() * 4), vol = 0.02 + Math.random() * 0.03;
    for (let i = 0; i < n; i++) {
      const t = t0 + i * (0.08 + Math.random() * 0.05), o = C.createOscillator(), gg = C.createGain();
      o.frequency.setValueAtTime(f0, t); o.frequency.exponentialRampToValueAtTime(f0 * (1.25 + Math.random() * 0.4), t + 0.05);
      this.env(gg, t, 0.006, vol, 0.06); o.connect(gg); gg.connect(this.master); gg.connect(this.rev); o.start(t); o.stop(t + 0.1);
    }
  },
  bell() { // campana de la espadana
    const C = this.C; if (!C) return; const t = C.currentTime;
    [[1, 0.2, 6], [2.0, 0.08, 4], [2.76, 0.07, 3], [5.4, 0.03, 1.5]].forEach(([k, v, d]) => {
      const o = C.createOscillator(); o.frequency.value = 311 * k; const gg = C.createGain(); this.env(gg, t, 0.004, v, d);
      o.connect(gg); gg.connect(this.master); gg.connect(this.rev); o.start(t); o.stop(t + d + 0.1); });
  },
  rumble(dur) { // el crater sube
    if (!this.C) return; const t = this.C.currentTime, [gg] = this.src(dur + 0.3, 'lowpass', 110, 1.5);
    gg.gain.setValueAtTime(0.0001, t); gg.gain.exponentialRampToValueAtTime(0.9, t + dur * 0.6); gg.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    gg.connect(this.master); gg.connect(this.rev);
  },
  grave() { // entra la totalidad
    if (!this.C) return; const C = this.C, t = C.currentTime;
    [41.2, 61.7].forEach((f, i) => { const o = C.createOscillator(); o.frequency.value = f; const gg = C.createGain();
      gg.gain.setValueAtTime(0.0001, t); gg.gain.exponentialRampToValueAtTime(i ? 0.18 : 0.4, t + 1.5); gg.gain.exponentialRampToValueAtTime(0.0001, t + 7);
      o.connect(gg); gg.connect(this.master); o.start(t); o.stop(t + 7.1); });
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
at('eclipse', (5.2 - 2) / 1.4, () => AU.grave());               // totalidad: se callan los pajaros
at('eclipse', (5.8 - 2) / 1.4, () => AU.rumble(2.2));           // sube el borde del crater
at('eclipse', (8.8 - 2) / 1.4, () => { AU.tone(1318.5, 4, 0.09); AU.tone(1975.5, 3, 0.05); });
for (const id of ORDER) if (SC[id].card) at(id, 0.3, () => AU.boom(0.7));
at('umbral', 0.2, () => AU.swell(5));
at('umbral', 4.4, () => AU.click());
at('anclaA', 0.3, () => AU.click());
at('anclaA', 1.25, () => AU.tone(220));
at('anclaB', 0.95, () => AU.tone(277.18));
at('anclaB', 1.0, () => { AU.tone(329.63, 5, 0.16); AU.tone(440, 5, 0.1); AU.swell(4); });
at('adapt', 1.8, () => AU.click());
at('adapt', 1.8 + (9.4 - 3.0) / 1.8, () => { AU.boom(0.55); AU.swell(3); });
at('diamante', (1.7 - 1.2) / 1.2, () => { AU.tone(1760, 3, 0.12); AU.tone(2637, 2.5, 0.06); });
at('final', 0.05, () => AU.boom(1.1));
at('final', 1.8, () => { AU.tone(110, 5, 0.18); AU.tone(164.8, 5, 0.1); });
at('coda', 0.2, () => AU.click());
EVENTS.sort((a, b) => a.t - b.t);

// ------------------------------------------------------------------ reproduccion
let T = SC.final.start + 3.2, playing = false, last = 0, started = false;
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

{ const n = document.getElementById('dur'); if (n) n.textContent = Math.floor(TOTAL / 60) + ':' + String(Math.round(TOTAL % 60)).padStart(2, '0') + ' · con sonido'; }
const fontsReady = document.fonts ? Promise.all([`400 40px ${DISPLAY}`, `italic 400 40px ${SERIF}`, `400 20px ${MONO}`, `500 20px ${MONO}`]
  .map(f => document.fonts.load(f).catch(() => null))) : Promise.resolve();
resize();
fontsReady.then(() => { if (!started) drawFrame(T); });
})();
