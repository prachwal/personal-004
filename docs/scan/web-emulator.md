# web-emulator

- Zrodlo: `/home/prachwal/source/emulators/web-emulator`.
- Stos: TypeScript/Vite; `package.json`, `vite.config.ts`, `vitest.config.ts`; worker/WASM bridge.
- UI/debug: `src/app/DebugOverlay.tsx`, pipeline video i konfiguracja maszyn.
- Rendering: WebGL2/WebGPU, tryby tekstowe/bitmapowe, dekodery CGA/C64/Kaypro/CPC/HGC i inne.
- Monitoring/monitory: `src/video/monitors/` oraz dokumentacja monitorow dla wielu maszyn.
- PET/Apple 1: dema `src/video/text/demos/pet.ts`, `apple1.ts`; fonty PET, C64, VIC-20, Apple 1 i innych w `public/fonts/`.
- Kandydat reuse: format kontraktow display/video, font registry i webowy overlay debugowania.
