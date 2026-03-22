import { initAreaLanding } from "./area-landing";

const teardown = initAreaLanding("admin");
window.addEventListener("unload", teardown);
