import "./style.css";
import { bootstrapCatalog } from "./catalog-bootstrap";
import { escapeHtml } from "./utils";

const maybeRoot = document.querySelector<HTMLDivElement>("#app");
if (!maybeRoot) throw new Error("Missing #app root");

bootstrapCatalog(maybeRoot).catch((error: unknown) => {
  maybeRoot.innerHTML = `<div class="empty">Could not load catalog: ${escapeHtml(String(error))}</div>`;
});
