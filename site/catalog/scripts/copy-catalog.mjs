import { copyFileSync, existsSync, mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "../../..");
const dataDir = resolve(here, "../public/data");
mkdirSync(dataDir, { recursive: true });

const copies = [
  ["data/compatibility/catalog-v2.json", "catalog-v2.json"],
  ["data/compatibility/price-history-v1.json", "price-history-v1.json"],
];

for (const [srcRel, name] of copies) {
  const source = resolve(root, srcRel);
  const target = resolve(dataDir, name);
  if (existsSync(source)) {
    copyFileSync(source, target);
    console.log(`copied ${name}`);
  }
}
