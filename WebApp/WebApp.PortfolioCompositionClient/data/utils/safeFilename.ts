/**
 * Good-enough filename sanitization.
 */
export const safeFilename = (filename: string): string =>
    filename
        .replace(/[^a-zA-Z0-9\s.-]/g, '_')
        .trim()
        .replace(/^\.+|\.+$/g, '');
