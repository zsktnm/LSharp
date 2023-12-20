open System
open System.IO
open System.Security.Cryptography

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Authentication
open Microsoft.IdentityModel.Tokens
open Microsoft.EntityFrameworkCore
open Microsoft.AspNetCore.Identity

open Giraffe
open LSharp.Identity.Core
open Microsoft.AspNetCore.Http
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open LSharp.Helpers.Handlers
open LSharp.Helpers.ActionResults
open LSharp.Identity.Data
open LSharp.Identity.DataTransfer

let [<Literal>] exitCode = 0

let rsaPublic = RSA.Create()
let rsaPrivate = RSA.Create()


let getTokensOf (user: LsharpUser) (roles: string) = 
    let descriptor = SecurityTokenDescriptor()
    descriptor.SigningCredentials <- SigningCredentials(RsaSecurityKey(rsaPrivate), SecurityAlgorithms.RsaSha256)
    descriptor.Claims <- Map [ 
        ClaimTypes.NameIdentifier, user.Id; 
        ClaimTypes.Email, user.Email; 
        ClaimTypes.Role, roles
    ]
    descriptor.Issuer <- "LSharp" 
    descriptor.Expires <- DateTime.UtcNow.AddHours(24000); // TODO: change
    let tokenHandler = JwtSecurityTokenHandler()
    let securityToken = tokenHandler.CreateToken(descriptor);
    { 
        Token = tokenHandler.WriteToken(securityToken);
        RefreshToken = user.RefreshToken
    }

let signIn (userService: UserManager<LsharpUser>) (user: LsharpUser) = task {
    user.GenerateRefreshToken()
    let! roles = userService.GetRolesAsync(user) 
    match! userService.UpdateAsync(user) with
    | r when r.Succeeded -> return roles |> String.concat ";" |> getTokensOf user |> Success
    | r -> return BadRequest (r.Errors |> Seq.map (fun err -> err.Description) |> List.ofSeq )
}


let hasEmailHandler email = 
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        ctx.GetService<UserManager<LsharpUser>>()
        |> hasEmail email
        |> actionResultTaskToResponse next ctx


let registrationHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        let userService = ctx.GetService<UserManager<LsharpUser>>()
        let! data = ctx.BindModelAsync<UserDTO>()
        return! userService 
        |> createUser data.Email data.Password
        |> ActionResult.bindTask (fun user -> addRole user "user" userService)
        |> actionResultTaskToResponse next ctx
    }
    

let loginHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        let userService = ctx.GetService<UserManager<LsharpUser>>()
        let! data = ctx.BindJsonAsync<UserDTO>()
        return! userService 
        |> checkPassword data.Email data.Password
        |> ActionResult.bindTask (fun u -> signIn userService u)
        |> actionResultTaskToResponse next ctx
    }

let refreshHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        let userService = ctx.GetService<UserManager<LsharpUser>>()
        let! data = ctx.BindJsonAsync<TokenDTO>()
        return! userService
        |> findByRefreshToken data.RefreshToken
        |> ActionResult.bindTask (fun u -> signIn userService u)
        |> actionResultTaskToResponse next ctx
    }


let webApp = choose [
    route "/registration" >=> POST >=> registrationHandler
    route "/login" >=> POST >=> loginHandler
    route "/refresh" >=> POST >=> refreshHandler
    routef "/hasEmail/%s" hasEmailHandler
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

let getIdentityOptions (options: IdentityOptions) = 
    options.Password.RequireDigit <- true
    options.Password.RequireUppercase <- true
    options.Password.RequireNonAlphanumeric <- true
    options.Password.RequireLowercase <- true
    options.Password.RequiredLength <- 8
    options.User.RequireUniqueEmail <- true
    

[<EntryPoint>]
let main args =
    rsaPublic.FromXmlString(File.ReadAllText("public.xml"))
    rsaPrivate.FromXmlString(File.ReadAllText("private.xml"))
    let builder = WebApplication.CreateBuilder(args)
    let connString = builder.Configuration.GetConnectionString("local")
    builder.Services.AddGiraffe() |> ignore
    builder.Services
        .AddDbContext<UsersDbContext>(fun opt -> opt.UseSqlite(connString) |> ignore)
        .AddIdentityCore<LsharpUser>(Action<IdentityOptions> getIdentityOptions)
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<UsersDbContext>()
        |> ignore

    builder.Services
        .AddAuthentication(Action<AuthenticationOptions> getAuthOptions)
        .AddJwtBearer(Action<JwtBearerOptions> getJwtOptions)
        |> ignore
    let app = builder.Build()

    
    app
        .UseAuthentication()
        .UseGiraffe(webApp)
    
    app.Run()

    exitCode

