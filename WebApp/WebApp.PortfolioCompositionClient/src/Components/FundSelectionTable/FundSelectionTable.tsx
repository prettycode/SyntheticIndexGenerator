import React, { useEffect, useRef, useState } from 'react';
import { Fund } from '../../Fund/models/Fund/Fund';
import { fetchCustomFunds } from '../../Fund/services/fetchCustomFunds';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
    faBan,
    faChevronDown,
    faChevronLeft,
    faChevronRight,
    faChevronUp,
    faPlus,
    faTrash
} from '@fortawesome/free-solid-svg-icons';
import { FundAllocation } from '../../Fund/models/Fund/FundAllocation';
import FundAnalysis from '../FundAnalysis/FundAnalysis';
import cloneDeep from 'lodash.clonedeep';
import { FundSelectionDropdown, FundSelectionDropdownOptionType } from '../FundSelectionDropdown/FundSelectionDropdown';
import { FundSelectionTableState } from './FundSelectionTableState';
import { FundSelectionTableRow } from './FundSelectionTableRow';
import { displayPercentage } from '../utils/displayPercentage';

import './FundSelectionTable.css';
import { sum } from '../../Fund/utils/sum';

export const UNSELECTED_FUND_FUNDID: string = '00000000-0000-0000-0000-000000000000';

export interface FundSelectionTableProps {
    onCalculatePortfolios: (rows: Array<FundSelectionTableRow>) => void;
    state?: FundSelectionTableState;
}

const FundSelectionTable: React.FC<FundSelectionTableProps> = ({ state, onCalculatePortfolios }) => {
    const defaultFundId = UNSELECTED_FUND_FUNDID;
    const defaultColumnsCount = 3;
    const defaultRowsCount = 3;

    const createRow = (columnCountInRow?: number | undefined): FundSelectionTableRow => ({
        fundId: defaultFundId,
        percentage: new Array(!columnCountInRow ? getColumnsCount() : columnCountInRow).fill(0)
    });
    const getColumnsCount = (): number => rows.reduce((max, row) => Math.max(max, row.percentage.length), 0);
    const sumColumn = (columnIndex: number): number =>
        sum(
            rows.map((row) => +row.percentage[columnIndex]),
            4
        );
    const sumSelectedFunds = (): number => rows.reduce((sum, row) => sum + (row.fundId === defaultFundId ? 0 : 1), 0);

    const [funds, setFunds] = useState<Array<Fund>>([]);
    const [fundComparison, setFundComparison] = useState<Array<string>>([]);
    const [rows, setRows] = useState<Array<FundSelectionTableRow>>(
        state?.rows ?? Array.from({ length: defaultRowsCount }, () => createRow(defaultColumnsCount))
    );
    const [customPortfolios, setCustomPortfolios] = useState<Array<Array<FundAllocation>> | undefined>(undefined);

    const triggerCalculation = useRef(false);

    // Load funds into state for lookup dropdown
    useEffect(() => {
        (async () => setFunds([...(await fetchCustomFunds())]))();
    }, []);

    useEffect(() => {
        if (triggerCalculation.current) {
            onCalculate();
            triggerCalculation.current = false;
        }
    });

    const onAddTableRow = () => {
        setRows([...rows, createRow()]);
    };

    const onClearTable = () => {
        setRows(Array.from({ length: rows.length }, () => createRow()));
    };

    const onResetTable = () => {
        setRows(Array.from({ length: defaultRowsCount }, () => createRow(defaultColumnsCount)));
    };

    const onAddColumn = () => {
        const rowsShallowCopy = [...rows];
        rowsShallowCopy.forEach((row) => row.percentage.push(0));
        setRows(rowsShallowCopy);
    };

    const onChangePercentage = (rowIndex: number, columnIndex: number, value: string) => {
        const newRows = [...rows];
        newRows[rowIndex].percentage[columnIndex] = value;
        setRows(newRows);
    };

    const onFundSelected = (rowIndex: number, selectedFund: FundSelectionDropdownOptionType | null) => {
        const newRows = [...rows];
        newRows[rowIndex].fundId = selectedFund?.value || defaultFundId;
        setRows(newRows);
    };

    const onMoveRowUp = (rowIndex: number) => {
        if (rowIndex === 0) {
            throw new Error('Cannot move the first row up.');
        }

        const newRows = [...rows];
        const tempRow = newRows[rowIndex];
        newRows[rowIndex] = newRows[rowIndex - 1];
        newRows[rowIndex - 1] = tempRow;
        setRows(newRows);
    };

    const onMoveRowDown = (rowIndex: number) => {
        if (rowIndex === rows.length - 1) {
            throw new Error('Cannot move the last row down.');
        }

        const newRows = [...rows];
        const tempRow = newRows[rowIndex];
        newRows[rowIndex] = newRows[rowIndex + 1];
        newRows[rowIndex + 1] = tempRow;
        setRows(newRows);
    };

    const onCompare = () => {
        if (!fundComparison.length) {
            throw new Error('Now funds selected to compare.');
        }

        const newRows = fundComparison.map((fundId, index) => {
            const row = createRow();
            row.fundId = fundId;
            row.percentage[index] = 100;
            return row;
        });

        setRows(newRows);
        triggerCalculation.current = true;
    };

    const onCalculate = () => {
        const portfolios = [];
        const columnsCount = getColumnsCount();

        for (let columnIndex = 0; columnIndex < columnsCount; columnIndex++) {
            const portfolio: Array<FundAllocation> = [];

            for (const row of rows) {
                // Ignore rows where no fund has been selected
                if (row.fundId === defaultFundId) {
                    break;
                }

                const percentageNum = Number(row.percentage[columnIndex]);

                // Clean-up UI by setting invalid and zeros to blanks
                if (Number.isNaN(percentageNum) || percentageNum === 0) {
                    row.percentage[columnIndex] = '';
                    continue;
                }

                portfolio.push({ fundId: row.fundId, percentage: percentageNum });
            }

            if (portfolio.length && sum(portfolio.map((fund) => fund.percentage)) === 100) {
                portfolios.push(portfolio);
            }
        }

        // Set the custom portfolios and recalculate
        setCustomPortfolios(portfolios);
        onCalculatePortfolios(cloneDeep(rows));
    };

    const onFundComparisonSelected = (fundSelection: Array<FundSelectionDropdownOptionType> | null): void => {
        setFundComparison(!fundSelection ? [] : fundSelection.map((fund) => fund.value));
    };

    function deleteColumn(columnIndex: number) {
        const newRows = [...rows];
        newRows.forEach((row) => row.percentage.splice(columnIndex, 1));
        setRows(newRows);
    }

    function deleteRow(rowIndex: number): void {
        const newRows = [...rows];
        newRows.splice(rowIndex, 1);
        setRows(newRows);
    }

    function disableRow(rowIndex: number): void {
        throw new Error(`Function not implemented. Cannot disable row ${rowIndex}`);
    }

    function disableColumn(columnIndex: number): void {
        throw new Error(`Function not implemented. Cannot disable column ${columnIndex}`);
    }

    function onMoveColumnLeft(columnIndex: number): void {
        const newRows = [...rows];
        newRows.forEach((row) => {
            const temp = row.percentage[columnIndex - 1];
            row.percentage[columnIndex - 1] = row.percentage[columnIndex];
            row.percentage[columnIndex] = temp;
        });
        setRows(newRows);
    }

    function onMoveColumnRight(columnIndex: number): void {
        const newRows = [...rows];
        newRows.forEach((row) => {
            const temp = row.percentage[columnIndex + 1];
            row.percentage[columnIndex + 1] = row.percentage[columnIndex];
            row.percentage[columnIndex] = temp;
        });
        setRows(newRows);
    }

    return (
        <>
            <h3>Custom Portfolios</h3>
            <table className="table table-borderless">
                <thead>
                    <tr>
                        <th></th>
                        <th></th>
                        <th>Compare Assets</th>
                    </tr>
                    <tr>
                        <th></th>
                        <th></th>
                        <th style={{ fontWeight: 'normal', display: 'flex', alignItems: 'center', flexWrap: 'nowrap' }}>
                            <span style={{ width: '100%' }}>
                                <FundSelectionDropdown
                                    onFundSelected={
                                        onFundComparisonSelected as unknown as (
                                            selection: FundSelectionDropdownOptionType | null
                                        ) => void
                                    }
                                    isMulti
                                    funds={funds}
                                />
                            </span>
                            <button
                                disabled={fundComparison.length === 0}
                                type="button"
                                onClick={onCompare}
                                className="btn btn-outline-primary float-start me-1"
                                style={{ marginLeft: 10 }}
                            >
                                Compare
                            </button>
                        </th>
                    </tr>
                    <tr>
                        <th></th>
                        <th></th>
                        <th></th>
                        <th
                            className="text-center"
                            colSpan={getColumnsCount()}
                        >
                            <span>Weight (%) in Portfolios</span>
                        </th>
                    </tr>
                    <tr>
                        <th></th>
                        <th></th>
                        <th
                            scope="col"
                            style={{ width: '100%' }}
                        >
                            Assets in Portfolios
                        </th>
                        {Array.from({ length: getColumnsCount() }).map((_, columnIndex) => (
                            <th
                                key={columnIndex}
                                className="text-center"
                                title={`Portfolio ${columnIndex + 1}`}
                            >
                                {columnIndex > 0 && (
                                    <button
                                        className="btn btn-xs"
                                        style={{ padding: '0 4px' }}
                                        onClick={() => onMoveColumnLeft(columnIndex)}
                                    >
                                        <FontAwesomeIcon icon={faChevronLeft} />
                                    </button>
                                )}
                                &nbsp;
                                {columnIndex < getColumnsCount() - 1 && (
                                    <button
                                        className="btn btn-xs"
                                        style={{ padding: '0 4px' }}
                                        onClick={() => onMoveColumnRight(columnIndex)}
                                    >
                                        <FontAwesomeIcon icon={faChevronRight} />
                                    </button>
                                )}
                                <br />P{columnIndex + 1}
                                <span style={{ fontSize: 'small' }}>
                                    <FontAwesomeIcon
                                        icon={faBan}
                                        className="ms-1 text-faded"
                                        cursor={'pointer'}
                                        onClick={() => disableColumn(columnIndex)}
                                    ></FontAwesomeIcon>
                                    <FontAwesomeIcon
                                        icon={faTrash}
                                        className="ms-1 text-faded"
                                        cursor={'pointer'}
                                        onClick={() => deleteColumn(columnIndex)}
                                    ></FontAwesomeIcon>
                                </span>
                            </th>
                        ))}
                        <th>
                            <button
                                title="Add new column"
                                className="btn btn-xs"
                                style={{ padding: '2px 4px' }}
                                onClick={onAddColumn}
                            >
                                <FontAwesomeIcon
                                    icon={faPlus}
                                    fixedWidth={true}
                                />
                            </button>
                        </th>
                    </tr>
                </thead>
                <tbody>
                    {rows.map((row, rowIndex) => (
                        <tr key={rowIndex}>
                            <td style={{ padding: 0, verticalAlign: 'middle' }}>
                                {rowIndex === rows.length - 1 && (
                                    <button
                                        title="Add new row"
                                        className="btn btn-xs"
                                        style={{ padding: '2px 4px' }}
                                        onClick={onAddTableRow}
                                    >
                                        <FontAwesomeIcon icon={faPlus} />
                                    </button>
                                )}
                                {rows.length > 1 && !(rowIndex === rows.length - 1) && (
                                    <div
                                        style={{
                                            display: 'flex',
                                            flexDirection: 'column',
                                            alignItems: 'center',
                                            justifyContent: 'center',
                                            height: '100%'
                                        }}
                                    >
                                        {rowIndex !== 0 && (
                                            <button
                                                className="btn btn-xs"
                                                style={{ padding: '0 4px' }}
                                                onClick={() => onMoveRowUp(rowIndex)}
                                            >
                                                <FontAwesomeIcon icon={faChevronUp} />
                                            </button>
                                        )}
                                        {rowIndex !== rows.length - 1 && (
                                            <button
                                                className="btn btn-xs"
                                                style={{ padding: '0 4px' }}
                                                onClick={() => onMoveRowDown(rowIndex)}
                                            >
                                                <FontAwesomeIcon icon={faChevronDown} />
                                            </button>
                                        )}
                                    </div>
                                )}
                            </td>
                            <td
                                style={{ fontSize: 'small' }}
                                className="text-faded"
                            >
                                <FontAwesomeIcon
                                    icon={faBan}
                                    cursor={'pointer'}
                                    onClick={() => disableRow(rowIndex)}
                                ></FontAwesomeIcon>
                                <FontAwesomeIcon
                                    icon={faTrash}
                                    cursor={'pointer'}
                                    onClick={() => deleteRow(rowIndex)}
                                ></FontAwesomeIcon>
                            </td>
                            <td>
                                <FundSelectionDropdown
                                    funds={funds}
                                    selectedFundId={row.fundId}
                                    onFundSelected={(selectedOption: FundSelectionDropdownOptionType | null) =>
                                        onFundSelected(rowIndex, selectedOption)
                                    }
                                />
                            </td>
                            {row.percentage.map((percentageInColumn, columnIndex) => (
                                <td key={columnIndex}>
                                    <input
                                        className="form-control"
                                        style={{ textAlign: 'center', width: 85 }}
                                        type="text"
                                        maxLength={7}
                                        value={percentageInColumn.toString()}
                                        onChange={(e) => onChangePercentage(rowIndex, columnIndex, e.target.value)}
                                    />
                                </td>
                            ))}
                        </tr>
                    ))}
                    <tr>
                        <td></td>
                        <td></td>
                        <td className="align-top">Total: {sumSelectedFunds()} asset(s)</td>
                        {Array.from({ length: getColumnsCount() }, (_, columnIndex) => (
                            <td
                                className="align-top text-center"
                                key={columnIndex}
                            >
                                <span className={sumColumn(columnIndex) !== 100 ? 'text-danger' : 'text-success'}>
                                    {displayPercentage(sumColumn(columnIndex))}&thinsp;%
                                </span>
                                <br />
                                <span
                                    className="font-weight-lighter"
                                    style={{ fontSize: '0.8rem' }}
                                >
                                    {sumColumn(columnIndex) !== 100 &&
                                        `add ${displayPercentage(100 - sumColumn(columnIndex))}%`}
                                </span>
                            </td>
                        ))}
                    </tr>
                </tbody>
                <tfoot>
                    <tr>
                        <td></td>
                        <td></td>
                        <td colSpan={getColumnsCount() - 1}>
                            <div className="clearfix">
                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline-secondary float-start me-1"
                                    onClick={onAddTableRow}
                                >
                                    Add Row
                                </button>
                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline-secondary float-start me-1"
                                    onClick={onAddColumn}
                                >
                                    Add Column
                                </button>
                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline-danger float-start me-1"
                                    onClick={onClearTable}
                                >
                                    Clear
                                </button>
                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline-danger  btn-outline-secondary float-start me-1"
                                    onClick={onResetTable}
                                >
                                    Reset
                                </button>
                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline-primary float-start me-1"
                                    onClick={onCalculate}
                                >
                                    Calculate
                                </button>
                            </div>
                        </td>
                    </tr>
                </tfoot>
            </table>

            {customPortfolios && (
                <div style={{ marginTop: '40px' }}>
                    <FundAnalysis fundAllocations={customPortfolios} />
                </div>
            )}
        </>
    );
};

export default FundSelectionTable;
