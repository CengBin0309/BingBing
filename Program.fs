open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.DependencyInjection

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.Configure<ForwardedHeadersOptions>(fun (o: ForwardedHeadersOptions) ->
        o.ForwardedHeaders <- ForwardedHeaders.XForwardedFor ||| ForwardedHeaders.XForwardedProto
        o.KnownNetworks.Clear()
        o.KnownProxies.Clear())
    |> ignore

    // IIS / MonsterASP: ANCM owns the listen URL; do not override with PORT (same idea as Alpha ReadShelfWs host tuning).
    let underIis =
        not (String.IsNullOrEmpty (Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH")))
        || not (String.IsNullOrEmpty (Environment.GetEnvironmentVariable("APP_POOL_ID")))

    if not underIis then
        match Environment.GetEnvironmentVariable("PORT") with
        | null | "" -> ()
        | port -> builder.WebHost.UseUrls(sprintf "http://0.0.0.0:%s" port) |> ignore

    let app = builder.Build()

    app.UseForwardedHeaders() |> ignore
    app.UseStaticFiles() |> ignore
    CourseWorkbench.App.mapRoutes app
    app.Run()
    0
