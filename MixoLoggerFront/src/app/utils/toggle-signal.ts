import { signal, WritableSignal } from "@angular/core";

/**
 * Extends WritableSignal to add toggle functionality
 */
export interface ToggleableSignal<T> extends WritableSignal<T> {
  /**
   * Toggles between two values
   */
  toggle(): void;
}

/**
 * Creates a signal that can toggle between two values
 * @param initialValue Initial value
 * @param otherValue Value to toggle to
 */
/**
 * Creates a boolean signal that can be toggled
 * @param initialValue Initial boolean value
 */
export function toggle(initialValue: boolean): ToggleableSignal<boolean>;
/**
 * Creates a signal that can toggle between two values
 * @param initialValue Initial value
 * @param otherValue Value to toggle to
 */
export function toggle<T>(initialValue: T, otherValue: T): ToggleableSignal<T>;
export function toggle<T>(initialValue: T, otherValue?: T): ToggleableSignal<T> {
  const sig = signal(initialValue);

  if (typeof initialValue === 'boolean' && otherValue === undefined) {
    // Type assertion nécessaire car TypeScript ne peut pas inférer que T est boolean
    const boolSig = sig as WritableSignal<boolean>;
    return Object.assign(boolSig, {
      toggle: () => boolSig.set(!boolSig())
    }) as ToggleableSignal<T>;
  }

  return Object.assign(sig, {
    toggle: () => sig.set(sig() === initialValue ? otherValue as T : initialValue)
  });
}
