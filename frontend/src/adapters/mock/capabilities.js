import { TOPICS } from "../../state/topics";
import { getAll } from "../../capabilities/catalog";
let timer = null;
export const mockCapabilitiesAdapter = {
    start(store) {
        timer = setTimeout(() => {
            const payload = {
                status: "ok",
                data: [...getAll()],
            };
            store.publish(TOPICS.CAPABILITIES, payload);
        }, 200);
    },
    stop() {
        if (timer !== null) {
            clearTimeout(timer);
            timer = null;
        }
    },
};
