import { initAreaLanding } from "./area-landing";

const teardown = initAreaLanding("logs");
window.addEventListener("unload", teardown);
