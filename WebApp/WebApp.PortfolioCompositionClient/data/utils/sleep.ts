export const sleep = async ({ ms, sec }: { ms?: number; sec?: number }): Promise<void> =>
    new Promise((resolve) => setTimeout(resolve, ms ?? (sec ?? 0) * 1000));
