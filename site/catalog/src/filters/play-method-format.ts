import { PLATFORM_LABELS } from "../game-accessors";

export function formatPlayMethodOption(key: string): string {
  const pipe = key.indexOf("|");
  if (pipe < 0) return key;
  const platformKey = key.slice(0, pipe);
  const label = key.slice(pipe + 1);
  const platform = PLATFORM_LABELS[platformKey] ?? platformKey;
  return `${platform} · ${label}`;
}
