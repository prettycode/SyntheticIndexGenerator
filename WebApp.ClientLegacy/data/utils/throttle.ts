import { sleep } from './sleep';

/**
 * Pause before allowing a promise to be resolved. A dumb-simple rate-limiter.
 */
export async function throttle<T>({ ms, sec }: { ms?: number; sec?: number }, promise: Promise<T>): Promise<T> {
    await sleep({ ms, sec });
    return promise;
}
