/**
 * (Serializes to JSON.)
 */
export const dumbHash = <T>(data: T): string => JSON.stringify(data);

/**
 * Unique number between 0 and 2^32-1.
 */
export const hashCode = <T>(data: T): number =>
    dumbHash(data)
        .split('')
        .reduce((s, c) => (Math.imul(31, s) + c.charCodeAt(0)) | 0, 0);
