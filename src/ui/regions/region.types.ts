export type RegionState = "loading" | "resolved" | "error" | "empty";

export type RenderFn<T> = (data: T) => string;
