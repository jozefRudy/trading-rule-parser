import { GenericEditorComponent } from '../components/generic-editor/generic-editor.component';
import { catchError, EMPTY, Observable } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { match, P } from 'ts-pattern';
import { MatSnackBar } from '@angular/material/snack-bar';

export type ParserError = {
  message: string;
  line: number;
  column: number;
  index: number;
};
export type ApiError = {
  case: 'parserError';
  fields: ParserError;
};

export const universeModule = (items: string[]): string => {
  const universe = items.join(', ');

  // language=typescript
  return `
  declare module "universe" {
    /**
     * @description
     * Select from 390+ crypto instruments.
     *
     * @example
     export const universe = [Instrument.BTCUSDT, Instrument.ETHUSDT];
     */
    export enum Instrument {
    ${universe}
    }};`;
};

// language=javascript
export const code = `import { Mode, main } from "helper-methods";
import { Instrument } from "universe";

export const entry = main.priceMinute() > main.priceMinute().trend.sma(200);
export const exit = main.priceMinute() < main.priceMinute().trend.sma(200);

export const universe = [Instrument.BTC, Instrument.ETH];

export const name = "strategy1";

export const start = new Date(2020, 1, 1);

export const mode = Mode.Backtest;`;

// language=typescript
export const helperMethods = `
declare module "helper-methods" {
  type ArrayOp = {

    /**
     * @description
     * Adds two time series together element by element.
     *
     * Creates a new time series where each value is the sum of the
     * corresponding values from both input series. Useful for combining
     * multiple indicators or creating composite signals.
     *
     * @example
     * Add price with its moving average
     * const combined = main.priceMinute().arrayOp.add(main.priceMinute().trend.sma(20));
     */
    add(other: Timeseries): Timeseries;

    /**
     * @description
     * Subtracts one time series from another element by element.
     *
     * Creates a new time series where each value is the difference between
     * the corresponding values of the two input series. Useful for measuring
     * divergence between indicators or creating spread calculations.
     *
     * @example
     * Calculate the difference between price and its moving average
     * const spread = main.priceMinute().arrayOp.subtract(main.priceMinute().trend.sma(20));
     */
    subtract(other: Timeseries): Timeseries;

    /**
     * @description
     * Divides one time series by another element by element.
     *
     * Creates a new time series where each value is the quotient of the
     * corresponding values from both input series. Useful for calculating
     * ratios, percentages, or normalized values.
     *
     * @example
     * Calculate the ratio between price and its moving average
     * const priceRatio = main.priceMinute().arrayOp.divide(main.priceMinute().trend.sma(20));
     */
    divide(other: Timeseries): Timeseries;

    /**
     * @description
     * Multiplies two time series together element by element.
     *
     * Creates a new time series where each value is the product of the
     * corresponding values from both input series. Useful for scaling
     * indicators or combining percentage-based signals.
     *
     * @example
     * Scale an indicator by a weight factor
     * const result = main.priceMinute().arrayOp.multiply(main.constant(2));
     */
    multiply(other: Timeseries): Timeseries;
  }
  
  type Oscillator = {
    trix(period: number): Timeseries;
    bollingerBands(period: number): Timeseries;
    absoluteMomentum(period: number): Timeseries;
    relativeMomentum(period: number): Timeseries;
    /**
     * @description
     * Calculates the Relative Strength Index (RSI) for the time series.
     *
     * RSI is a momentum indicator that measures the magnitude of recent price
     * changes to evaluate overbought or oversold conditions. The RSI ranges
     * from 0 to 100, with values above 70 typically indicating overbought
     * conditions and values below 30 indicating oversold conditions.
     *
     * @example
     * Calculate a 14-period RSI
     * const rsiMinute = main.priceMinute().rsi(14);
     * const rsiDay = main.priceDay().oscillator.rsi(14);
     */    
    rsi(period: number): Timeseries;
    chandeMomentumOsc(period: number): Timeseries;
    absolutePriceOsc(fast: number, slow: number): Timeseries;
    percentagePriceOsc(fast: number, slow: number): Timeseries;
    macd(signal: number, fast: number, slow: number): Timeseries;
    stochasticRsi(period: number, k: number, d: number): Timeseries;
    mamaFast(): Timeseries;
    mamaSlow(): Timeseries;
  }
  
  type TrendIndicators = {
    /**
     * @description
     * Calculates a moving average of the time series.
     *
     * Smoothes price data by computing the average of a specified number
     * of consecutive data points, helping to identify underlying trends
     * and reduce short-term price fluctuations.
     *
     * @example
     * Calculate a 20-period simple moving average and compare it with current price
     * const entry = main.PriceMinute() > main.priceMinute().trend.sma(20);
     */
    sma(period: number): Timeseries;

    /**
     * @description
     * Calculates an exponential moving average of the time series.
     *
     * Applies more weight to recent prices while still considering older data,
     * resulting in a faster response to price changes compared to SMA.
     * Commonly used for identifying trend direction and potential reversals.
     *
     * @example
     * Calculate a 14-period exponential moving average and compare it with latest price
     * const entry = main.PriceMinute() > main.priceMinute().trend.ema(14);
     */
    ema(period: number): Timeseries;

    /**
     * @description
     * Calculates a weighted moving average of the time series.
     *
     * Applies linear weighting to price data, giving more importance to
     * recent prices and less to older ones, helping to identify
     * current market direction.
     *
     * @example
     * Calculate a 20-period weighted moving average and compare it with latest price
     * const entry = main.priceMinute() > main.priceMinute().trend.wma(20);
     */    
    wma(period: number): Timeseries;

    /**
     * @description
     * Calculates a double exponential moving average of the time series.
     *
     * Reduces lag by applying the EMA calculation twice, providing a more
     * responsive indicator that helps identify trend changes earlier than
     * traditional moving averages.
     *
     * @example
     * Calculate a 20-period double exponential moving average and compare it with latest price
     * const entry = main.priceMinute() > main.priceMinute().trend.dema(20);
     */    
    dema(period: number): Timeseries;

    /**
     * @description
     * Calculates the Kaufman Adaptive Moving Average of the time series.
     *
     * Adjusts sensitivity automatically based on market volatility, becoming
     * more responsive in trending markets and more stable in sideways
     * markets.
     *
     * @example
     * Calculate a 10-period Kaufman adaptive moving average and compare it with latest price
     * const entry = main.priceMinute() > main.priceMinute().trend.kama(10);
     */    
    kama(period: number): Timeseries;

    /**
     * @description
     * Calculates a triple exponential moving average of the time series.
     *
     * Applies EMA calculation three times to reduce lag and price noise
     * while maintaining responsiveness to significant price movements.
     *
     * @example
     * Calculate a 20-period triple exponential moving average and compare it with latest price
     * const entry = main.priceMinute() > main.priceMinute().trend.tema(20);
     */    
    tema(period: number): Timeseries;

    /**
     * @description
     * Calculates Tim Tillson's T3 moving average of the time series.
     *
     * Combines multiple EMAs with optimized weighting to produce a smooth,
     * responsive indicator that minimizes both lag and price noise.
     *
     * @example
     * Calculate a 10-period T3 moving average and compare it with latest price
     * const entry = main.priceMinute() > main.priceMinute().trend.t3(10);
     */    
    t3(period: number): Timeseries;

    /**
     * @description
     * Calculates a triangular moving average of the time series.
     *
     * Double-smooths price data by averaging the SMA, resulting in a
     * smoother indicator that helps identify longer-term market
     * trends.
     *
     * @example
     * Calculate a 20-period triangular moving average and compare it with latest price
     * const entry = main.priceMinute() > main.priceMinute().trend.trima(20);
     */    
    trima(period: number): Timeseries;
    
    /**
     * @description
     * Calculates the midpoint price over a specified period.
     *
     * Averages the highest and lowest values of a close-price series
     * over the specified period to identify the mean price level and
     * potential trend direction.
     *
     * @example
     * Calculate a 14-period midpoint of the closing price and compare it with latest price
     * const entry = main.priceMinute().trend.midpointOverPeriod(14) > main.priceMinute();
     */
    midpointOverPeriod(period: number): Timeseries;

    /**
     * @description
     * Shifts the time series data backward by the specified number of periods.
     *
     * Creates a new time series where each value is offset backward by the
     * specified number of periods. This is useful for comparing current values
     * with historical values or creating custom indicators based on past data.
     *
     * @example
     * Enter when price is higher than 5 minutes ago
     * export const entry = main.priceMinute() > main.priceMinute().trend.lag(5)
     */    
    lag(period: number): Timeseries;

    /**
     * @description
     * Calculates the Hilbert Transform Trendline.
     *
     * Uses cycle analysis to generate a smooth trendline that filters
     * out market noise while maintaining the underlying trend
     * direction.
     *
     * @example
     * Calculate Hilbert Transform Trendline and compare it with latest price
     * const entry = main.priceMinute() > main.priceMinute().trend.htTrendline();
     */    
    htTrendline(): Timeseries;
  }
  
  type Timeseries = {
    /**
     * @description
     * Selection of Indicators calculating price adjustment
     * These should generally be compared with raw price to arrive at signal
     */
    trend: TrendIndicators;

    /**
     * @description
     * Selection of oscillators
     * These oscillate around 0, some are range bound (e.g. rsi between 0 and 100), some are not
     */
    oscillator: Oscillator;
    
    /**
     * @description
     * Array operations that operate on 2 arrays
     */    
    arrayOp: ArrayOp;
  }

  export const main: {
    /**
     * @description
     * price of instrument that changes every minute
     */
    priceMinute(): Timeseries;

    /**
     * @description
     * price of instrument that changes once per day (UTC time)
     */
    priceDay(): Timeseries;

    /**
     * @description
     * constant value, useful to compare to range bound indicator as e.g. RSI
     *
     * @example
     * const entry = main.priceMinute().rsi(14) < main.constant(30);
     */
    constant(value: number): Timeseries;
  };

  export enum Freq {
    Minute = 0,
    Day = 1,
  }

  /**
   * @description Indicator Type.
   */
  export enum Type {
    Price = 0,
  }

  enum Mode {
    /**
     * @description 
     * It will run strategy from now (user-defined start is ignored), updating results every minute.
     * 
     * <i>You need a paid subscription to use this feature.<i>
     */
    Live = 0,

    /**
     * @description
     * It will run backtest from defined start until present.
     */
    Backtest = 1,
  }
}
`;

export const catchSnackbarError = <T>(snackBar: MatSnackBar) => {
  return (source: Observable<T>) =>
    source.pipe(
      catchError((e: HttpErrorResponse) => {
        console.log(e);
        snackBar.open('Server error', 'Dismiss', {
          duration: 5000,
        });
        return EMPTY;
      }),
    );
};

export const catchEditorError = <T>(editor: GenericEditorComponent) => {
  return (source: Observable<T>) =>
    source.pipe(
      catchError((e: HttpErrorResponse) => {
        return match([e.status, e.error as ApiError])
          .with(
            [
              422,
              {
                case: 'parserError',
                fields: P.select(),
              },
            ],
            (parserError: ParserError) => {
              console.log('setting error');
              editor.setError(parserError);
              return EMPTY;
            },
          )
          .otherwise(() => {
            throw e;
          });
      }),
    );
};