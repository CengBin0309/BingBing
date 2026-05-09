module CourseWorkbench.Html

open System.Net
open CourseWorkbench.Logic

let esc (s: string) = WebUtility.HtmlEncode(s)

let layout (title: string) (body: string) =
    sprintf
        """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>%s</title>
  <link rel="stylesheet" href="/css/app.css" />
</head>
<body>
  <header class="top">
    <a class="brand" href="/">GradeTrack</a>
    <nav>
      <a href="/">Home</a>
      <a href="/estimate">Weighted grade</a>
      <a href="/deadline">Deadline countdown</a>
      <a href="/finalmark">Final planner</a>
    </nav>
  </header>
  <main class="wrap">%s</main>
  <footer class="foot">Local tool — not an official university system.</footer>
</body>
</html>"""
        (esc title)
        body

let home () =
    layout
        "GradeTrack"
        """<section class="hero">
  <h1>Plan coursework before the curve drops</h1>
  <p class="lead">Students often lose points because weights are unclear or a final date sneaks up.
  GradeTrack gives a quick weighted average preview and a plain-language countdown to any due date.</p>
  <div class="actions"><a class="btn primary" href="/estimate">Try weighted calculator</a>
  <a class="btn" href="/deadline">Count down a deadline</a>
  <a class="btn" href="/finalmark">Final exam planner</a></div>
</section>
<section class="grid">
  <article><h2>Weighted average</h2><p>Enter parts (homework, labs, exams) with weights that sum to 1 or 100.</p></article>
  <article><h2>Deadlines</h2><p>Pick a date; see how many full days remain from today in your browser time zone.</p></article>
  <article><h2>Final planner</h2><p>Given your current average and weights, estimate the minimum final exam mark to hit a target overall.</p></article>
</section>"""

let estimateForm (parts: Part list) (message: string option) =
    let rows =
        parts
        |> List.mapi (fun i p ->
            sprintf
                """<tr>
  <td><input name="label_%i" type="text" maxlength="120" value="%s" /></td>
  <td><input name="weight_%i" type="number" step="0.01" min="0" value="%s" /></td>
  <td><input name="score_%i" type="number" step="0.1" min="0" max="100" value="%s" /></td>
</tr>"""
                i
                (esc p.Label)
                i
                (p.Weight.ToString("0.##"))
                i
                (p.Score.ToString("0.#")))

        |> String.concat ""

    let msg =
        match message with
        | None -> ""
        | Some m -> sprintf """<p class="banner">%s</p>""" (esc m)

    layout
        "Weighted grade"
        (sprintf
            """%s
<form method="post" action="/estimate" class="card">
  <h1>Weighted course average</h1>
  <p class="muted">Weights can be fractions (0.3) or percent-like numbers (30) — the math normalises by the sum of weights.</p>
  <table class="tbl">
    <thead><tr><th>Part</th><th>Weight</th><th>Score (0–100)</th></tr></thead>
    <tbody>%s</tbody>
  </table>
  <button class="btn primary" type="submit">Recalculate</button>
</form>"""
            msg
            rows)

let estimateResult (avg: float) =
    layout
        "Result"
        (sprintf
            """<section class="card">
  <h1>Estimated average</h1>
  <p class="big">%.2f / 100</p>
  <p>Letter band (informal): <strong>%s</strong></p>
  <p class="actions"><a class="btn" href="/estimate">Adjust inputs</a>
  <a class="btn" href="/">Home</a></p>
</section>"""
            avg
            (letterGrade avg))

let deadlineForm (hint: string option) =
    let h =
        match hint with
        | None -> ""
        | Some t -> sprintf """<p class="banner warn">%s</p>""" (esc t)

    layout
        "Deadline"
        (sprintf
            """%s
<form method="get" action="/deadline/result" class="card">
  <h1>Days until deadline</h1>
  <label>Target date <input type="date" name="d" required /></label>
  <button class="btn primary" type="submit">Calculate</button>
</form>"""
            h)

let deadlineResult (daysLabel: string) (human: string) =
    layout
        "Countdown"
        (sprintf
            """<section class="card">
  <h1>Days remaining</h1>
  <p class="big">%s</p>
  <p class="muted">%s</p>
  <p class="actions"><a class="btn" href="/deadline">Another date</a>
  <a class="btn" href="/">Home</a></p>
</section>"""
            (esc daysLabel)
            (esc human))

let finalForm (hint: string option) =
    let h =
        match hint with
        | None -> ""
        | Some t -> sprintf """<p class="banner">%s</p>""" (esc t)

    layout
        "Final planner"
        (sprintf
            """%s
<form method="post" action="/finalmark" class="card">
  <h1>What do you need on the final?</h1>
  <p class="muted">Enter your current average on completed work, the weight already in the gradebook, the final exam weight, and the course average you want. Weights can be decimals (0.6 + 0.4) or percent-like (60 + 40).</p>
  <label>Current average (0–100) <input name="avg" type="number" step="0.1" min="0" max="100" required /></label>
  <label>Weight completed <input name="wdone" type="number" step="0.01" min="0" required /></label>
  <label>Final weight <input name="wfinal" type="number" step="0.01" min="0" required /></label>
  <label>Target course average <input name="target" type="number" step="0.1" min="0" max="100" required /></label>
  <button class="btn primary" type="submit">Calculate</button>
</form>"""
            h)

let finalResult (need: float) (target: float) =
    layout
        "Final planner result"
        (sprintf
            """<section class="card">
  <h1>Minimum final mark</h1>
  <p class="big">%.1f / 100</p>
  <p class="muted">Rounded to one decimal, assuming the two weight buckets describe the whole course and the final is the only unknown component.</p>
  <p>Target you asked for: <strong>%.1f</strong></p>
  <p class="actions"><a class="btn" href="/finalmark">Try other numbers</a>
  <a class="btn" href="/">Home</a></p>
</section>"""
            need
            target)

let finalUnreachable (target: float) =
    layout
        "Final planner"
        (sprintf
            """<section class="card">
  <h1>Not reachable at 100 on the final</h1>
  <p class="muted">Even with a perfect final, the weighted mix cannot reach %.1f with the weights you entered.</p>
  <p class="actions"><a class="btn" href="/finalmark">Adjust inputs</a>
  <a class="btn" href="/">Home</a></p>
</section>"""
            target)
