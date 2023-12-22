open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Session
open Microsoft.Extensions.DependencyInjection
open Giraffe

open Lsharp.Requests
open LSharp.Helpers.ActionResults
open System.Threading.Tasks

open Lsharp

let indexHandler = 
    fun next (ctx: HttpContext) -> htmlView (Index.Index()) next ctx

let registrationHandler =
    fun next (ctx: HttpContext) -> 
        htmlView (Registration.newViewModel () |> Registration.Registration) next ctx

let proccessRegistrationHandler =
    let send json = task {
        try
            return! jsonContent json
            |> httpPost "https://localhost:7177/registration"
            |> asJson<string, string list>
        with
        | _ -> return InternalError [ "Не удалось зарегистрироваться, попробуйте позже" ]
    }

    fun next (ctx: HttpContext) -> task {
        let! data = ctx.BindFormAsync<Registration.RegistrationViewModel>()
        let! result = 
            data
            |> function
                | data when data.Password = data.RepeatPassword -> Success data
                | _ -> BadRequest ["Пароли не совпадают"]
            |> Task.FromResult
            |> ActionResult.bindTask send

        match result with
        | BadRequest errors ->
            return! htmlView  (Registration.Registration { data with Errors = errors }) next ctx
        | Success _ -> return! htmlView (Index.Index()) next ctx
        | InternalError err | NotFound err -> 
            return! htmlView  (Registration.Registration { data with Errors = err }) next ctx
    }

let loginHandler = 
    fun next (ctx: HttpContext) ->
        htmlView (Login.newViewModel () |> Login.Login) next ctx

let proccessLoginHandler = 
    let send json = task {
        try
            return! jsonContent json
            |> httpPost "https://localhost:7177/login"
            |> asJson<Login.TokenDTO, string list>
        with
        | _ -> return InternalError [ "Сервис недоступен, попробуйте позже" ]
    }

    fun next (ctx: HttpContext) -> task {
        let! data = ctx.BindFormAsync<Login.LoginViewModel>()
        
        let! result = send data
        
        match result with
        | Success tokens -> 
            ctx.Response.Cookies.Append("token", tokens.Token)
            ctx.Response.Cookies.Append("refreshToken", tokens.RefreshToken)
            return! htmlView (Index.Index()) next ctx
        | InternalError err 
        | NotFound err 
        | BadRequest err -> 
            return! htmlView  (Login.Login { data with Errors = err }) next ctx

    }

let authorize (next: HttpFunc) (ctx: HttpContext) = 
    match ctx.GetCookieValue("token") with
    | Some token -> 
        next ctx
    | None -> 
        loginHandler next ctx


let webApp = choose [
    route "/" >=> GET >=> indexHandler;
    route "/registration" >=> GET >=> registrationHandler;
    route "/registration" >=> POST >=> proccessRegistrationHandler;
    route "/login" >=> GET >=> loginHandler;
    route "/login" >=> POST >=> proccessLoginHandler;

]


[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    builder.Services
        .AddDistributedMemoryCache()
        .AddSession(fun options ->
            options.Cookie.HttpOnly <- true
            // TODO: settings
        )
    |> ignore
    let app = builder.Build()

    app.UseSession() |> ignore
    app.UseStaticFiles() |> ignore
    app.UseGiraffe(webApp) |> ignore

    app.Run()

    0 // Exit code

