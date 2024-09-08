/**
 * `JSON.stringify(...)` with pretty-printing.
 */
export const jsonSerialize = <T>(data: T): string => JSON.stringify(data, null, 4);

/**
 * Avoid having to `JSON.parse(...) as T`.
 */
export const jsonDeserialize = <T>(data: string): T => JSON.parse(data);

/**
 * `jsonDeserialize()` with a wrapper that swallows and returns undefined when deserialization fails.
 */
export const tryJsonDeserialize = <T>(data: string): T => {
    try {
        return jsonDeserialize(data);
    } catch (error) {
        return undefined;
    }
};
