import { signal, WritableSignal } from "@angular/core";

interface ToggleableFunction {
	/**
	 * Creates a signal that can toggle between two values
	 * @param initialValue Initial value
	 * @param otherValue Value to toggle to
	 */
	<T extends string | number | symbol | object>(initialValue: T, otherValue: T): ToggleableSignal<T>;
	/**
	 * Creates a boolean signal that can be toggled
	 * @param initalState Initial boolean value
	 */
	(initalState: boolean): ToggleableSignal<boolean>;
}

/**
 * Extends WritableSignal to add toggle functionality
 */
export interface ToggleableSignal<T> extends WritableSignal<T> {
	/**
	 * Interchange la valeur du signal
	 */
	toggle(): void;
	/**
	 * Réinitialise le signal à sa valeur initiale
	 */
	reset(): void;
}

function toggleSignal<T>(initialValue: T, otherValue?: T): ToggleableSignal<T> {
	const sig = signal(initialValue);

	if (typeof initialValue === 'boolean' && otherValue === undefined) {
		// Type assertion nécessaire car TypeScript ne peut pas inférer que T est boolean
		const boolSig = sig as WritableSignal<boolean>;
		return Object.assign(boolSig, {
			toggle: () => boolSig.update(x => !x)
		}) as ToggleableSignal<T>;
	}

	return Object.assign(sig, {
		toggle: () => sig.update(x => x === initialValue ? otherValue as T : initialValue),
		reset: () => sig.set(initialValue)
	});
}

export const toggle: ToggleableFunction = (() => toggleSignal)();
