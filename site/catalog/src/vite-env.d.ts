/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_STEAM_SYNC_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
