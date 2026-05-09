module CourseWorkbench.Logic

open System

type Part =
    {
        Label: string
        Weight: float
        Score: float
    }

let weightedAverage (parts: Part list) =
    let wSum = parts |> List.sumBy (fun p -> p.Weight)

    if wSum <= 0.0 then
        None
    else
        let num = parts |> List.sumBy (fun p -> p.Weight * p.Score)
        Some(num / wSum)

let parsePart (label: string) (wText: string) (sText: string) =
    let lab = label.Trim()

    match Double.TryParse(wText), Double.TryParse(sText) with
    | (true, w), (true, s) when w >= 0.0 && s >= 0.0 && s <= 100.0 -> Some { Label = lab; Weight = w; Score = s }
    | _ -> None

let daysUntil (fromDate: DateTime) (deadline: DateTime) =
    let span = deadline.Date - fromDate.Date
    span.Days

let letterGrade (avg: float) =
    if avg >= 90.0 then "A"
    elif avg >= 80.0 then "B"
    elif avg >= 70.0 then "C"
    elif avg >= 60.0 then "D"
    else "F"

let defaultParts =
    [
        {
            Label = "Homework"
            Weight = 0.3
            Score = 88.0
        }
        {
            Label = "Midterm"
            Weight = 0.3
            Score = 76.0
        }
        {
            Label = "Final"
            Weight = 0.4
            Score = 0.0
        }
    ]

/// `weightCompleted` and `weightFinal` are shares of the whole course (they need not sum to 1; they are normalised).
/// Returns the minimum final mark in \[0,100\] needed for `targetOverall` if it is reachable with a single final component.
let neededFinalMark (avgCompleted: float) (weightCompleted: float) (weightFinal: float) (targetOverall: float) =
    let wSum = weightCompleted + weightFinal

    if wSum <= 0.0 || weightFinal <= 0.0 then
        None
    else
        let wc = weightCompleted / wSum
        let wf = weightFinal / wSum
        let raw = (targetOverall - wc * avgCompleted) / wf

        if raw > 100.0 then
            None
        elif raw < 0.0 then
            Some 0.0
        else
            Some raw
