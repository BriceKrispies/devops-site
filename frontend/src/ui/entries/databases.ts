import { initAreaLanding } from "./area-landing";

const teardown = initAreaLanding("databases");
window.addEventListener("unload", teardown);
