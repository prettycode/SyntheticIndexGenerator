import { constants, PathLike } from 'fs';
import { access, stat, mkdir, readFile, writeFile } from 'node:fs/promises';

/**
 * Will return false on-error instead of throwing.
 */
export const dirExists = async (path: PathLike): Promise<boolean> => {
    try {
        await access(path, constants.F_OK);
    } catch (err) {
        return false;
    }

    const stats = await stat(path);

    return stats.isDirectory();
};

/**
 * May throw an error if the directory cannot be created.
 */
export const makeDir = async (path: PathLike): Promise<void> => {
    await mkdir(path, { recursive: true });
};

/**
 * Will return false on-error instead of throwing.
 */
export const fileExists = async (path: PathLike): Promise<boolean> => {
    try {
        await access(path, constants.F_OK);
    } catch (err) {
        return false;
    }

    return true;
};

/**
 * May throw an error if the file cannot be read.
 */
export const getFileText = async (path: PathLike): Promise<string> => {
    return readFile(path, 'utf-8');
};

/**
 * May throw an error if the file cannot be written. Will overwrite, not append, if the file exists already.
 */
export const putFileText = async (file: PathLike, contents: string): Promise<void> => {
    return writeFile(file, contents, 'utf-8');
};
