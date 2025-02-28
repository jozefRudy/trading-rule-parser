module Ast

open System

type Arithmetic =
    | Add
    | Subtract
    | Multiply
    | Divide

type IntFunc =
    | Sma
    | Ema
    | Wma
    | Dema
    | Kama
    | Tema
    | T3
    | Trima
    | Rsi
    | Lag
    | MidPointOverPeriod
    | ChandeMomentumOsc
    | Momentum
    | RateOfChange
    | Trix
    | BollingerBands

type NoParamFunc =
    | HtTrendline
    | MamaFast
    | MamaSlow

type TwoIntFunc =
    | AbsolutePriceOsc
    | PercentagePriceOsc

type ThreeIntFunc =
    | Macd
    | StochRsi

type Frequency =
    | Day
    | Minute

type Indicator = Price of Frequency

type Formula =
    | NoParamOp of NoParamFunc * Formula
    | SingleParamOp of IntFunc * int * Formula
    | TwoParamOp of TwoIntFunc * int * int * Formula
    | ThreeParamOp of ThreeIntFunc * int * int * int * Formula
    | ArithmeticOp of Formula * Arithmetic * Formula
    | Constant of float
    | Indicator of Indicator

let nullaryParamOp p f = NoParamOp(p, f)
let singleParamOp p i f = SingleParamOp(p, i, f)
let binaryParamOp p i1 i2 f = TwoParamOp(p, i1, i2, f)
let ternaryParamOp p i1 i2 i3 f = ThreeParamOp(p, i1, i2, i3, f)

let arithmeticOp l op r = ArithmeticOp(l, op, r)


type Comparison =
    | LT
    | GT
    | EQ
    | LTE
    | GTE

type BooleanOp =
    | And
    | Or

type Logical =
    | CompoundLogical of Logical * BooleanOp * Logical
    | CompoundFormula of Formula * Comparison * Formula
    | Never
    | Always

let compoundLogical l op r = CompoundLogical(l, op, r)
let compoundFormula l op r = CompoundFormula(l, op, r)

type Import = Import of alias: string * from: string
let import alias from = Import(alias, from)

type StringUniverse =
    | StringUniverse of string list

    member this.Value = let (StringUniverse tickers) = this in tickers


type StrategyName = string

type Period = DateOnly

type Status =
    | NotStarted
    | InProgress
    | Finished
    | Stopped


type Mode =
    | Backtest
    | Live


type Declaration =
    | Logical of name: string * Logical
    | Universe of name: string * StringUniverse
    | StrategyName of name: string * StrategyName
    | StartPeriod of name: string * Period
    | Mode of name: string * Mode

type File = Declaration list

type Backtest =
    { Mode: Mode
      Since: Period option
      Entry: Logical
      Exit: Logical
      Universe: StringUniverse
      Name: StrategyName }

type Request = { Content: string }

type Content = { Content: string }