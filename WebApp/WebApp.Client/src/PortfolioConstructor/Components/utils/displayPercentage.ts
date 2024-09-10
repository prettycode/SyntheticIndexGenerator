/**
 * Returns a string representation of a percentage with one decimal. Removes the ending ".0" if present.
 */
export const displayPercentage = (percentage: number): string => percentage.toFixed(2).replace(/\.0$/, '');
