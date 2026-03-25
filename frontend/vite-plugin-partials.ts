import { readFileSync } from "fs";
import { resolve, relative, dirname } from "path";
import type { Plugin } from "vite";

const VALID_NAV_VALUES = [
  "login",
  "dashboard",
  "trace",
  "queues",
  "databases",
  "logs",
  "operations",
  "admin",
] as const;

const VALID_INCLUDE_NAMES = ["head", "topbar", "sidebar"] as const;

const PAGE_RE = /<!--\s*page:\s*(\{.*?\})\s*-->/;
const INCLUDE_RE = /<!--\s*include:\s*(\w+)\s*-->/g;
const ACTIVE_RE = /\{\{active:(\w+)\}\}/g;
const HREF_RE = /\{\{href:(.*?)\}\}/g;

interface PageMeta {
  title: string;
  nav: string;
}

export function htmlPartialsPlugin(): Plugin {
  let root = "";

  return {
    name: "html-partials",

    configResolved(config) {
      root = config.root;
    },

    transformIndexHtml: {
      order: "pre",
      handler(html: string, ctx) {
        // Only process files that contain our directives
        if (!html.includes("<!-- page:") && !html.includes("<!-- include:")) {
          return html;
        }

        const filename: string = ctx.filename;

        // --- Extract and validate page metadata ---

        const pageMatch = html.match(PAGE_RE);
        if (!pageMatch) {
          throw new Error(
            `[html-partials] File contains directives but no <!-- page: {...} --> metadata: ${filename}`,
          );
        }

        let raw: Record<string, unknown>;
        try {
          raw = JSON.parse(pageMatch[1]);
        } catch (e) {
          throw new Error(
            `[html-partials] Invalid JSON in <!-- page: --> directive in ${filename}: ${(e as Error).message}`,
          );
        }

        if (typeof raw.title !== "string" || raw.title.length === 0) {
          throw new Error(
            `[html-partials] Missing or empty required field "title" in page metadata: ${filename}`,
          );
        }
        if (typeof raw.nav !== "string" || raw.nav.length === 0) {
          throw new Error(
            `[html-partials] Missing or empty required field "nav" in page metadata: ${filename}`,
          );
        }
        if (!(VALID_NAV_VALUES as readonly string[]).includes(raw.nav)) {
          throw new Error(
            `[html-partials] Unknown nav value "${raw.nav}" in ${filename}. Valid values: ${VALID_NAV_VALUES.join(", ")}`,
          );
        }

        const meta: PageMeta = { title: raw.title, nav: raw.nav };

        // Remove the page directive comment from the output
        html = html.replace(PAGE_RE, "");

        // --- Inject data-page on <body> ---

        html = html.replace(/<body([\s>])/, `<body data-page="${meta.nav}"$1`);

        // --- Compute current file's directory relative to root ---

        const currentFileDir = relative(root, dirname(filename));

        // --- Replace include directives ---

        html = html.replace(INCLUDE_RE, (_match, name: string) => {
          if (!(VALID_INCLUDE_NAMES as readonly string[]).includes(name)) {
            throw new Error(
              `[html-partials] Unknown include "${name}" in ${filename}. Valid includes: ${VALID_INCLUDE_NAMES.join(", ")}`,
            );
          }

          const partialPath = resolve(root, "src", "partials", `${name}.html`);
          let content: string;
          try {
            content = readFileSync(partialPath, "utf-8");
          } catch {
            throw new Error(
              `[html-partials] Partial file not found: ${partialPath}`,
            );
          }

          // --- Partial-specific substitutions ---

          if (name === "head") {
            content = content.replace("{{title}}", meta.title);
          }

          if (name === "sidebar") {
            content = content.replace(ACTIVE_RE, (_m, navId: string) => {
              return navId === meta.nav ? "active" : "";
            });
            // Reset global regex state before reuse
            HREF_RE.lastIndex = 0;
            content = content.replace(HREF_RE, (_m, targetPath: string) => {
              return resolveHref(targetPath, currentFileDir, root);
            });
          }

          return content;
        });

        return html;
      },
    },
  };
}

/**
 * Resolve a target path to a relative href from the current file's directory.
 *
 * @param targetPath  - Path relative to the project root (e.g., "src/pages/trace.html" or "./")
 * @param currentDir  - Directory of the current HTML file, relative to root
 * @param root        - Absolute path to the Vite project root
 * @returns A relative URL string (e.g., "./trace.html" or "../../")
 */
function resolveHref(
  targetPath: string,
  currentDir: string,
  root: string,
): string {
  const targetAbs = resolve(root, targetPath);
  const currentAbs = resolve(root, currentDir);
  let rel = relative(currentAbs, targetAbs);

  // Normalize Windows backslashes to forward slashes
  rel = rel.replace(/\\/g, "/");

  // Same-directory case: path.relative returns ""
  if (rel === "") {
    rel = ".";
  }

  // Ensure the result starts with "./" or "../"
  if (!rel.startsWith(".")) {
    rel = "./" + rel;
  }

  // Preserve trailing slash for directory references
  if (targetPath.endsWith("/") && !rel.endsWith("/")) {
    rel += "/";
  }

  return rel;
}
