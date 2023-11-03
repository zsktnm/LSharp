open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Authentication
open Microsoft.IdentityModel.Tokens
open System.Security.Cryptography
open System.IO
open System.Security.Claims
open Microsoft.AspNetCore.Http

open LSharp.Users.Data
open LSharp.Helpers.ImageSaving


let [<Literal>] exitCode = 0
let [<Literal>] maxFileSize = 524288L // 512 kb

let bindTaskResult func taskValue = task {
    let! value = taskValue
    match value with 
    | Error e -> return Error e
    | Ok v -> return func v
}
    

let rsaPublic = RSA.Create()

let authorize = 
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let getUserId (ctx: HttpContext) = 
    ctx.User.FindFirst ClaimTypes.NameIdentifier 
    |> fun claim -> claim.Value


let responseFromResult next ctx result = task {
    match! result with
        | Ok msg -> return! Successful.OK msg next ctx
        | Error msg -> return! RequestErrors.BAD_REQUEST msg next ctx
}

let helloHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match! (ctx |> getUserId |> findUserByIdAsync) with
        | None -> return! RequestErrors.NOT_FOUND "UserId was broken" next ctx
        | Some user -> return! json user next ctx
    }

let getLevelHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match! (ctx |> getUserId |> findUserByIdAsync) with
        | None -> return! RequestErrors.NOT_FOUND "UserId was broken" next ctx
        | Some user -> return! text (string user.level) next ctx
    }


let replaceUserNameHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let name = ctx.TryGetQueryStringValue "name"

        match name with
        | None -> return! RequestErrors.BAD_REQUEST "Invalid name" next ctx
        | Some name -> return! ctx 
                               |> getUserId 
                               |> updateUserNameAsync name
                               |> responseFromResult next ctx
    }

let changeAvatarAsync id filename = task {
    match! filename with
    | Error msg -> return Error msg
    | Ok filename -> return! updateUserAvatarAsync filename id
}

let setPhotoHandler = 
    let getFilename() = 
        Path.Combine("images", Path.GetRandomFileName() + ".png")
    
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match ctx.Request.ContentLength with
        | header when not header.HasValue -> return! RequestErrors.BAD_REQUEST "No content-length header" next ctx
        | header when header.Value > maxFileSize -> return! RequestErrors.BAD_REQUEST "Invalid size" next ctx
        | header -> return! copyPngFile ctx.Request.Body header.Value (getFilename())
                         |> changeAvatarAsync (getUserId ctx) 
                         |> responseFromResult next ctx
    }


let webApp = choose [
    route "/getname" >=> GET >=> authorize >=> helloHandler
    route "/setname" >=> GET >=> authorize >=> replaceUserNameHandler
    route "/getlevel" >=> GET >=> authorize >=> getLevelHandler
    route "/setphoto" >=> POST >=> authorize >=> setPhotoHandler
]

let getJwtOptions (options: JwtBearerOptions) =
    // TODO: explore options
    options.TokenValidationParameters <- TokenValidationParameters(
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = RsaSecurityKey(rsaPublic),
        ValidateAudience = false,
        ValidateIssuer = true,
        ValidIssuer = "LSharp",
        ValidateLifetime = true
    )

let getAuthOptions (options: AuthenticationOptions) = 
    options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
    options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme


[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    builder.Services.AddGiraffe() |> ignore
    
    builder.Services
        .AddAuthentication(Action<AuthenticationOptions> getAuthOptions)
        .AddJwtBearer(Action<JwtBearerOptions> getJwtOptions) 
        |> ignore

    let app = builder.Build()
    rsaPublic.FromXmlString(File.ReadAllText("public.xml"))

    app
        .UseAuthentication()
        .UseGiraffe(webApp) 
    |> ignore


    app.Run()

    exitCode

