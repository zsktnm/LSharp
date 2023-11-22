module LSharp.Helpers.Handlers

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
open ActionResults

module Req = 
    let badRequest = RequestErrors.BAD_REQUEST
    let notFound = RequestErrors.NOT_FOUND
    let internalError = ServerErrors.INTERNAL_ERROR
    let ok = Successful.OK
    let noContent = Successful.NO_CONTENT

let authorize<'a> = 
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let getUserId (ctx: HttpContext) = 
    ctx.User.FindFirst ClaimTypes.NameIdentifier 
    |> fun claim -> claim.Value

let responseFromResult next ctx result = task {
    match! result with
        | Ok msg -> return! Req.ok msg next ctx
        | Error msg -> return! Req.badRequest msg next ctx
}

let actionResultToResponse next ctx result = task {
        match result with
        | Success x -> return! Req.ok x next ctx 
        | NotFound msg -> return! Req.notFound msg next ctx
        | BadRequest msg -> return! Req.badRequest msg next ctx
        | ServerError msg -> return! Req.internalError msg next ctx
    }

let actionResultTaskToResponse next ctx result = task {
        match! result with
        | Success x -> return! Req.ok x next ctx 
        | NotFound msg -> return! Req.notFound msg next ctx
        | BadRequest msg -> return! Req.badRequest msg next ctx
        | ServerError msg -> return! Req.internalError msg next ctx
    }

