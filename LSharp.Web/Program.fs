open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Giraffe
open Microsoft.AspNetCore.Http
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
    fun next (ctx: HttpContext) -> task {
        let! data = ctx.BindFormAsync<Registration.RegistrationViewModel>()

        let! result = 
            data
            |> function
                | data when data.Password = data.RepeatPassword -> Success data
                | _ -> BadRequest ["Пароли не совпадают"]
            |> Task.FromResult
            |> ActionResult.bindTask (fun d -> 
                jsonContent d
                |> httpPost "https://localhost:7177/registration"
                |> asJson<string list>
            )
        
        match result with
        | BadRequest errors ->
            return! htmlView  (Registration.Registration { data with Errors = errors }) next ctx
        | Success _ -> return! htmlView (Index.Index()) next ctx
        | _ -> 
            return! htmlView  (Registration.Registration { data with Errors = ["Не удалось зарегистрироваться, попробуйте позже"] }) next ctx
    }


let webApp = choose [
    route "/" >=> GET >=> indexHandler;
    route "/registration" >=> GET >=> registrationHandler;
    route "/registration" >=> POST >=> proccessRegistrationHandler;
]


[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    app.UseStaticFiles() |> ignore
    app.UseGiraffe(webApp) |> ignore

    app.Run()

    0 // Exit code

