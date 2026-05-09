module CourseWorkbench.App

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open CourseWorkbench.Logic
open CourseWorkbench.Html

let private writeHtml (ctx: HttpContext) (html: string) =
    ctx.Response.ContentType <- "text/html; charset=utf-8"
    ctx.Response.WriteAsync(html)

let private writeText (ctx: HttpContext) (text: string) =
    ctx.Response.ContentType <- "text/plain; charset=utf-8"
    ctx.Response.WriteAsync(text)

let private textOrEmpty (t: string | null) =
    match t with
    | null -> ""
    | x -> x

let private formGet (form: IFormCollection) (name: string) =
    let mutable s = StringValues.Empty
    let ok = form.TryGetValue(name, &s)

    if ok then
        textOrEmpty (s.ToString())
    else
        ""

let mapRoutes (app: WebApplication) =
    app.MapGet("/health", RequestDelegate(fun ctx -> writeText ctx "GradeTrack ok"))
    |> ignore

    app.MapGet("/", RequestDelegate(fun ctx -> writeHtml ctx (home ())))
    |> ignore

    app.MapGet("/estimate", RequestDelegate(fun ctx -> writeHtml ctx (estimateForm defaultParts None)))
    |> ignore

    app.MapPost(
        "/estimate",
        RequestDelegate(fun ctx ->
            task {
                let! form = ctx.Request.ReadFormAsync()

                let parts =
                    [ 0..5 ]
                    |> List.choose (fun i ->
                        let lab = formGet form (sprintf "label_%i" i)
                        let w = formGet form (sprintf "weight_%i" i)
                        let s = formGet form (sprintf "score_%i" i)

                        if String.IsNullOrWhiteSpace(lab) && String.IsNullOrWhiteSpace(w) then
                            None
                        else
                            parsePart lab w s)
                    |> List.truncate 8

                match parts with
                | [] ->
                    return! writeHtml ctx (estimateForm defaultParts (Some "Add at least one row with a label and valid numbers."))
                | ps ->
                    match weightedAverage ps with
                    | None ->
                        return! writeHtml ctx (estimateForm ps (Some "Weights must sum to a positive number."))
                    | Some avg ->
                        return! writeHtml ctx (estimateResult avg)
            })
    )
    |> ignore

    app.MapGet("/deadline", RequestDelegate(fun ctx -> writeHtml ctx (deadlineForm None)))
    |> ignore

    app.MapGet(
        "/deadline/result",
        RequestDelegate(fun ctx ->
            let q = ctx.Request.Query

            match q.TryGetValue("d") with
            | true, v when v.Count > 0 ->
                let raw = v.[0]

                let dtxt =
                    match raw with
                    | null -> ""
                    | s when s.Length = 0 -> ""
                    | s -> s

                match DateTime.TryParse(dtxt) with
                | true, target ->
                    let today = DateTime.Now
                    let d = daysUntil today target

                    let label = sprintf "%i" d

                    let human =
                        if d < 0 then sprintf "That date was %i days ago." (-d)
                        elif d = 0 then "Due today — finish what you can."
                        else sprintf "You have %i full calendar days before that date (local time)." d

                    writeHtml ctx (deadlineResult label human)
                | _ -> writeHtml ctx (deadlineForm (Some "Could not parse date."))
            | _ -> writeHtml ctx (deadlineForm (Some "Pick a date first.")))
    )
    |> ignore

    app.MapGet("/finalmark", RequestDelegate(fun ctx -> writeHtml ctx (finalForm None)))
    |> ignore

    app.MapPost(
        "/finalmark",
        RequestDelegate(fun ctx ->
            task {
                let! form = ctx.Request.ReadFormAsync()
                let avgT = formGet form "avg"
                let wDoneT = formGet form "wdone"
                let wFinT = formGet form "wfinal"
                let tgtT = formGet form "target"

                match Double.TryParse(avgT), Double.TryParse(wDoneT), Double.TryParse(wFinT), Double.TryParse(tgtT) with
                | (true, avg), (true, wDone), (true, wFin), (true, target) when
                    avg >= 0.0
                    && avg <= 100.0
                    && wDone >= 0.0
                    && wFin > 0.0
                    && target >= 0.0
                    && target <= 100.0
                    ->
                    match neededFinalMark avg wDone wFin target with
                    | Some need -> return! writeHtml ctx (finalResult need target)
                    | None -> return! writeHtml ctx (finalUnreachable target)
                | _ ->
                    return!
                        writeHtml
                            ctx
                            (finalForm (Some "Use numbers only; final weight must be positive; averages within 0–100."))
            })
    )
    |> ignore
