import Decimal from 'decimal.js';

export function sum(numbers: Array<number>, decimalDigits: number = 0): number {
    return numbers
        .reduce((total, num) => {
            return total.plus(num);
        }, new Decimal(0))
        .toDecimalPlaces(decimalDigits)
        .toNumber();
}
