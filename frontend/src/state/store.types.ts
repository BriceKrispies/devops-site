export type Unsubscribe = () => void;

export interface Store {
  publish<T>(topic: string, data: T): void;
  subscribe<T>(topic: string, callback: (data: T) => void): Unsubscribe;
  get<T>(topic: string): T | undefined;
}
