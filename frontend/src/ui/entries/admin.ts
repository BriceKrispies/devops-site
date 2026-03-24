import { requireAuth } from "../../platform/auth-guard";
import { initAreaLanding } from "./area-landing";

requireAuth().then((ok) => {
  if (!ok) return;
  const teardown = initAreaLanding("admin");
  window.addEventListener("unload", teardown);
});
