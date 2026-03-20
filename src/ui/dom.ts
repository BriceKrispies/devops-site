export function queryRegion(name: string): HTMLElement | null {
  return document.querySelector<HTMLElement>(`[data-region="${name}"]`);
}
