import { initAreaLanding } from "./area-landing";
const teardown = initAreaLanding("queues");
window.addEventListener("unload", teardown);
