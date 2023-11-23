module LSharp.Identity.Data

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



let hasEmail email (manager: UserManager<LsharpUser>) = task {
    match! manager.FindByEmailAsync(email) with
    | null -> return NotFound "Not found"
    | _ -> return Success email
}

let findByEmail email (manager: UserManager<LsharpUser>) = task {
    match! manager.FindByEmailAsync(email) with
    | null -> return NotFound "Not found"
    | user -> return Success user
}

let createUser email password (manager: UserManager<LsharpUser>) = task {
    let! result = manager.CreateAsync(LsharpUser(email), password)
    match result with
    | r when not r.Succeeded -> 
        return BadRequest (r.Errors |> Seq.map (fun err -> err.Description) |> String.concat "\n")
    | _ -> return! findByEmail email manager
}

let addRole user role (manager: UserManager<LsharpUser>) = task {
    let! result = manager.AddToRoleAsync(user, role)
    match result with
    | r when not r.Succeeded -> 
        return BadRequest (r.Errors |> Seq.map (fun err -> err.Description) |> String.concat "\n")
    | _ -> return Success "Success"
}

let checkPassword email password (manager: UserManager<LsharpUser>) = 
    let checkPasswordByUser user password (manager: UserManager<LsharpUser>) = task {
        match! manager.CheckPasswordAsync(user, password) with
        | false -> return BadRequest "Invalid password"
        | true -> return! findByEmail email manager
    }
    manager
    |> findByEmail email
    |> ActionResult.bindTask (fun u -> checkPasswordByUser u password manager)



let findByRefreshToken token (manager: UserManager<LsharpUser>) = task {
    match! manager.Users.FirstOrDefaultAsync(fun u -> u.RefreshToken = token) with
    | null -> return NotFound "Invalid token"
    | user -> return Success user
}