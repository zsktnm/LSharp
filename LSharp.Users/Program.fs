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
open LSharp.Helpers.Handlers
open LSharp.Helpers.ActionResults
open System.Threading.Tasks
open LSharp.Rabbit
open RabbitMQ.Client
open RabbitMQ.Client.Events


let [<Literal>] exitCode = 0
let [<Literal>] maxFileSize = 524288L // 512 kb

// TODO: centrilize / use settings
let factory = ConnectionFactory(HostName = "localhost")
let exchangeName = "lsharp"
let queueExp = "exp_gain"
let routeName = queueExp
    

let rsaPublic = RSA.Create()

type ExpGain = {
    UserId: string;
    Exp: int;
}


let isValidSymbol symbol = 
    Char.IsLetterOrDigit symbol || symbol = ' ' || symbol = '_'


let checkUserName name = 
    name
    |> fun name -> 
        String.length name < 36 && 
        String.length (name.Trim()) > 0 &&
        String.forall isValidSymbol name
    |> function
        | true -> Some name
        | false -> None


let userInfoHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let! user =
            ctx 
            |> getUserId 
            |> findUserByIdAsync
        match user with
        | Success user -> return! Req.ok user next ctx
        | NotFound _ ->
            return! createAnonimous (getUserId ctx)
            |> actionResultTaskToResponse next ctx
        | _ -> return! Req.badRequest "invalid id" next ctx
    }


let getLevelHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        ctx 
        |> getUserId 
        |> findUserByIdAsync
        |> ActionResult.mapTask (fun u -> u.level)
        |> actionResultTaskToResponse next ctx


let replaceUserNameHandler = 
    let getName (ctx: HttpContext) =
        ctx.TryGetQueryStringValue "name"

    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match (getName ctx |> Option.bind checkUserName) with
        | None -> 
            return! Req.badRequest "Некорректное имя. Имя должно содержать цифры, буквы, пробелы или символ подчеркивания. Длина имени не должна превышать 36 символов" next ctx
        | Some name -> 
            return! ctx 
            |> getUserId 
            |> updateUserNameAsync name
            |> actionResultTaskToResponse next ctx
    }

let changeAvatarAsync id filename = task {
    match! filename with
    | Success filename -> return! updateUserAvatarAsync filename id
    | err -> return err
}

let setPhotoHandler = 
    let getFilename() = 
        Path.Combine("images", Path.GetRandomFileName() + ".png")
    
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match ctx.Request.ContentLength with
        | header when not header.HasValue -> 
            return! Req.badRequest "No content-length header" next ctx
        | header when header.Value > maxFileSize -> 
            return! Req.badRequest "Invalid size" next ctx
        | header -> 
            return! copyPngFile ctx.Request.Body header.Value (getFilename())
            |> changeAvatarAsync (getUserId ctx) 
            |> actionResultTaskToResponse next ctx
    }

let addExpHandler =
    let tryParse (input: string) =
        let mutable res = 0
        match (Int32.TryParse(input, &res)) with
        | true -> Some res
        | false -> None

    fun (next: HttpFunc) (ctx: HttpContext) ->
        ctx.TryGetQueryStringValue("exp")
        |> Option.bind tryParse
        |> Option.map (fun exp -> (getUserId ctx, exp))
        |> ActionResult.fromOption "exp is not specified"
        |> Task.FromResult
        |> ActionResult.bindTask (fun (userId, exp) -> addExpToUser userId exp)
        |> actionResultTaskToResponse next ctx
      


let webApp = choose [
    route "/getinfo" >=> GET >=> authorize >=> userInfoHandler
    route "/setname" >=> GET >=> authorize >=> replaceUserNameHandler
    route "/getlevel" >=> GET >=> authorize >=> getLevelHandler
    route "/setphoto" >=> POST >=> authorize >=> setPhotoHandler
    route "/addexp" >=> GET >=> authorize >=> addExpHandler
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

let subscribe () = 
// TODO: handle errors
    let connection = factory.CreateConnection()
    let channel = connection.CreateModel()

    let listener (args: BasicDeliverEventArgs) =
        printfn "receive message"
        let result = fromJsonBytes<ExpGain> (args.Body.ToArray())
        addExpToUser result.UserId result.Exp
        |> fun t -> t.GetAwaiter().GetResult()
        |> ignore

    startQueueDeclare queueExp
    |> durable
    |> executeQueueDeclare channel
    |> fun ok -> printfn "Start listening. msg count: %d" ok.MessageCount 

    declareDirectExchange exchangeName channel
    bindQueue queueExp exchangeName routeName channel

    channel
    |> createEventConsumer
    ||> listen listener
    ||> startConsume queueExp


[<EntryPoint>]
let main args =
    ignore <| subscribe ()
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

