import { copyFileSync, mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "../../..");
const source = resolve(root, "data/compatibility/catalog-v2.json");
const target = resolve(here, "../public/data/catalog-v2.json");

mkdirSync(dirname(target), { recursive: true });
copyFileSync(source, target);
console.log(`copied catalog to ${target}`);
