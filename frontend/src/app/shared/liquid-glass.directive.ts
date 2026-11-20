/*!
 * Liquid Glass — refraction directive (Angular).
 *
 * The refraction field is a port of the fragment shader in liquid-glass-js
 * (https://github.com/dashersw/liquid-glass-js) — Copyright (c) 2025 Armagan
 * Amcalar, MIT.
 *
 * Pair with the `.liquid-glass` class from styles.css:
 *   <div class="liquid-glass" appLiquidGlass>…</div>
 */
import { Directive, ElementRef, Input, NgZone, OnDestroy, OnInit, inject } from '@angular/core';

export interface LiquidGlassConfig {
  edgeIntensity: number;
  rimIntensity: number;
  baseIntensity: number;
  edgeDistance: number;
  rimDistance: number;
  baseDistance: number;
  cornerBoost: number;
  rippleEffect: number;
  blurRadius: number;
  warp: boolean;
}

const GLASS_PRESET: LiquidGlassConfig = {
  edgeIntensity: 0.015,
  rimIntensity: 0.028,
  baseIntensity: 0.05,
  edgeDistance: 0.5,
  rimDistance: 1.7,
  baseDistance: 0.2,
  cornerBoost: 0.06,
  rippleEffect: 0.26,
  blurRadius: 2,
  warp: false,
};

const SUPERSAMPLE = 2;
const MAX_MAP_EDGE = 1400;
const BLUR_STD_PER_RADIUS = 0.35;

interface FilterEntry {
  id: string;
  node: SVGFilterElement;
  refs: number;
}

@Directive({
  selector: '[appLiquidGlass]',
  standalone: true,
})
export class LiquidGlassDirective implements OnInit, OnDestroy {
  @Input('appLiquidGlass') config: Partial<LiquidGlassConfig> | '' = '';

  private static readonly filters = new Map<string, FilterEntry>();
  private static defs: SVGSVGElement | null = null;
  private static nextId = 0;

  private readonly host = inject(ElementRef<HTMLElement>).nativeElement;
  private readonly zone = inject(NgZone);

  private resizeObserver: ResizeObserver | null = null;
  private readonly onWindowResize = () => {
    clearTimeout(this.resizeDebounce);
    this.resizeDebounce = setTimeout(() => this.scheduleRebuild(), 180);
  };
  private readonly onPerfChange = () => {
    if (LiquidGlassDirective.isLiteMode()) {
      this.releaseCurrentFilter();
      this.host.style.backdropFilter = '';
    } else {
      this.scheduleRebuild();
    }
  };
  private resizeDebounce: ReturnType<typeof setTimeout> | undefined;
  private rebuildHandle = 0;
  private currentKey: string | null = null;

  ngOnInit(): void {
    if (!LiquidGlassDirective.isSupported()) {
      return;
    }
    this.zone.runOutsideAngular(() => {
      this.resizeObserver = new ResizeObserver(() => this.scheduleRebuild());
      this.resizeObserver.observe(this.host);
      window.addEventListener('resize', this.onWindowResize, { passive: true });
      window.addEventListener('desanti-perf-change', this.onPerfChange);
      if (!LiquidGlassDirective.isLiteMode()) {
        this.scheduleRebuild();
      }
    });
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    window.removeEventListener('resize', this.onWindowResize);
    window.removeEventListener('desanti-perf-change', this.onPerfChange);
    clearTimeout(this.resizeDebounce);
    cancelAnimationFrame(this.rebuildHandle);
    this.releaseCurrentFilter();
  }

  private static isLiteMode(): boolean {
    return document.documentElement.dataset['perf'] === 'lite';
  }

  private static isSupported(): boolean {
    const brands = (navigator as any).userAgentData?.brands as { brand: string }[] | undefined;
    if (brands) {
      return brands.some((b) => /Chromium|Google Chrome|Microsoft Edge/i.test(b.brand));
    }
    return /Chrome\//.test(navigator.userAgent);
  }

  private scheduleRebuild(): void {
    if (LiquidGlassDirective.isLiteMode()) {
      return;
    }
    cancelAnimationFrame(this.rebuildHandle);
    this.rebuildHandle = requestAnimationFrame(() => this.rebuild());
  }

  private rebuild(): void {
    if (LiquidGlassDirective.isLiteMode()) {
      this.releaseCurrentFilter();
      this.host.style.backdropFilter = '';
      return;
    }
    const w = Math.round(this.host.offsetWidth);
    const h = Math.round(this.host.offsetHeight);
    if (w < 2 || h < 2) {
      return;
    }
    const radius = Math.min(
      resolveRadius(getComputedStyle(this.host).borderTopLeftRadius, w, h),
      Math.min(w, h) / 2,
    );
    const pageW = window.innerWidth;
    const pageH = window.innerHeight;
    const cfg: LiquidGlassConfig = { ...GLASS_PRESET, ...(this.config || {}) };

    const key = [w, h, radius, pageW, pageH, JSON.stringify(cfg)].join('|');
    if (key === this.currentKey) {
      return;
    }
    this.releaseCurrentFilter();
    const entry = LiquidGlassDirective.acquireFilter(key, w, h, radius, pageW, pageH, cfg);
    this.currentKey = key;
    this.host.style.backdropFilter = `url(#${entry.id})`;
  }

  private releaseCurrentFilter(): void {
    if (!this.currentKey) {
      return;
    }
    const key = this.currentKey;
    this.currentKey = null;
    const entry = LiquidGlassDirective.filters.get(key);
    if (entry && --entry.refs <= 0) {
      LiquidGlassDirective.filters.delete(key);
      entry.node.remove();
    }
  }

  private static acquireFilter(
    key: string,
    w: number,
    h: number,
    radius: number,
    pageW: number,
    pageH: number,
    cfg: LiquidGlassConfig,
  ): FilterEntry {
    let entry = this.filters.get(key);
    if (entry && entry.node.isConnected) {
      entry.refs++;
      return entry;
    }
    if (entry) {
      this.filters.delete(key);
    }
    const { mapUrl, scale } = buildDisplacementMap(w, h, radius, pageW, pageH, cfg);
    const id = `liquid-glass-${this.nextId++}`;

    const svgNs = 'http://www.w3.org/2000/svg';
    const filter = document.createElementNS(svgNs, 'filter');
    filter.setAttribute('id', id);
    const marginPx = scale / 2 + 3 * cfg.blurRadius * BLUR_STD_PER_RADIUS;
    const mx = (marginPx / w) * 100;
    const my = (marginPx / h) * 100;
    filter.setAttribute('x', `${-mx}%`);
    filter.setAttribute('y', `${-my}%`);
    filter.setAttribute('width', `${100 + 2 * mx}%`);
    filter.setAttribute('height', `${100 + 2 * my}%`);
    filter.setAttribute('color-interpolation-filters', 'sRGB');
    filter.dataset['key'] = key;

    const feImage = document.createElementNS(svgNs, 'feImage');
    feImage.setAttribute('href', mapUrl);
    feImage.setAttribute('x', '0');
    feImage.setAttribute('y', '0');
    feImage.setAttribute('width', String(w));
    feImage.setAttribute('height', String(h));
    feImage.setAttribute('preserveAspectRatio', 'none');
    feImage.setAttribute('result', 'map');

    const feDisplacement = document.createElementNS(svgNs, 'feDisplacementMap');
    feDisplacement.setAttribute('in', 'SourceGraphic');
    feDisplacement.setAttribute('in2', 'map');
    feDisplacement.setAttribute('scale', String(scale));
    feDisplacement.setAttribute('xChannelSelector', 'R');
    feDisplacement.setAttribute('yChannelSelector', 'G');
    feDisplacement.setAttribute('result', 'displaced');

    const feBlur = document.createElementNS(svgNs, 'feGaussianBlur');
    feBlur.setAttribute('in', 'displaced');
    feBlur.setAttribute('stdDeviation', String(cfg.blurRadius * BLUR_STD_PER_RADIUS));

    filter.append(feImage, feDisplacement, feBlur);
    this.ensureDefs().querySelector('defs')!.appendChild(filter);

    entry = { id, node: filter, refs: 1 };
    this.filters.set(key, entry);
    return entry;
  }

  private static ensureDefs(): SVGSVGElement {
    if (!this.defs || !this.defs.isConnected) {
      const svgNs = 'http://www.w3.org/2000/svg';
      const svg = document.createElementNS(svgNs, 'svg');
      svg.setAttribute('width', '0');
      svg.setAttribute('height', '0');
      svg.style.position = 'fixed';
      svg.setAttribute('aria-hidden', 'true');
      svg.appendChild(document.createElementNS(svgNs, 'defs'));
      document.body.appendChild(svg);
      this.defs = svg;
    }
    return this.defs;
  }
}

function buildDisplacementMap(
  w: number,
  h: number,
  radius: number,
  pageW: number,
  pageH: number,
  cfg: LiquidGlassConfig,
): { mapUrl: string; scale: number } {
  const ss = Math.max(0.25, Math.min(SUPERSAMPLE, MAX_MAP_EDGE / Math.max(w, h)));
  const bw = Math.max(1, Math.round(w * ss));
  const bh = Math.max(1, Math.round(h * ss));
  const r = radius * ss;
  const minDim = Math.min(w, h);
  const dx = new Float32Array(bw * bh);
  const dy = new Float32Array(bw * bh);
  let maxAbs = 0;

  for (let py = 0; py < bh; py++) {
    for (let px = 0; px < bw; px++) {
      const cx = (px + 0.5) / bw;
      const cy = (py + 0.5) / bh;

      const tx = Math.abs(px + 0.5 - bw / 2) - (bw / 2 - r);
      const ty = Math.abs(py + 0.5 - bh / 2) - (bh / 2 - r);
      const outside = Math.hypot(Math.max(tx, 0), Math.max(ty, 0));
      const inside = Math.min(Math.max(tx, ty), 0);
      const distPx = Math.max(-(outside + inside - r), 0) / ss;

      const edgeFall = Math.exp(-distPx * cfg.edgeDistance);
      const rimFall = Math.exp(-distPx * cfg.rimDistance);
      const baseFall = 1 - Math.exp(-distPx * cfg.baseDistance);
      const baseComponent = cfg.warp ? baseFall * cfg.baseIntensity : 0;
      const total = baseComponent + edgeFall * cfg.edgeIntensity + rimFall * cfg.rimIntensity;

      let nx = cx - 0.5;
      let ny = cy - 0.5;
      const len = Math.hypot(nx, ny);
      if (len > 0) {
        nx /= len;
        ny /= len;
      }

      const cornerNorm = Math.max(Math.min(cx, 1 - cx), Math.min(cy, 1 - cy)) * minDim;
      const corner = Math.exp(-cornerNorm * 0.3) * cfg.cornerBoost;
      const ripple = Math.sin((distPx / minDim) * 25) * cfg.rippleEffect * rimFall;

      const fx = (nx * (total + corner) - ny * ripple) * pageW;
      const fy = (ny * (total + corner) + nx * ripple) * pageH;

      const i = py * bw + px;
      dx[i] = fx;
      dy[i] = fy;
      maxAbs = Math.max(maxAbs, Math.abs(fx), Math.abs(fy));
    }
  }

  const scale = Math.max(maxAbs * 2, 1e-4);
  const bias = scale * (128 / 255 - 0.5);
  const canvas = document.createElement('canvas');
  canvas.width = bw;
  canvas.height = bh;
  const ctx = canvas.getContext('2d')!;
  const image = ctx.createImageData(bw, bh);
  const data = image.data;
  for (let i = 0; i < bw * bh; i++) {
    data[i * 4] = clampByte(255 * (0.5 + (dx[i] - bias) / scale));
    data[i * 4 + 1] = clampByte(255 * (0.5 + (dy[i] - bias) / scale));
    data[i * 4 + 2] = 128;
    data[i * 4 + 3] = 255;
  }
  ctx.putImageData(image, 0, 0);
  return { mapUrl: canvas.toDataURL('image/png'), scale };
}

function resolveRadius(computed: string, w: number, h: number): number {
  const value = parseFloat(computed) || 0;
  return computed.trim().endsWith('%') ? (value / 100) * Math.min(w, h) : value;
}

function clampByte(v: number): number {
  return Math.max(0, Math.min(255, Math.round(v)));
}
