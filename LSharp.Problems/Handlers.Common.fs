module LSharp.Problems.Handlers.Common

open Giraffe
open Microsoft.AspNetCore.Http
open System.Security.Claims

let badRequest = RequestErrors.BAD_REQUEST
let notFound = RequestErrors.NOT_FOUND
let ok = Successful.OK
let noContent = Successful.NO_CONTENT

let [<Literal>] maxImageSize = 1000000
let [<Literal>] maxZipSize = 5000000

let responseFromResult next ctx result = task {
    match! result with
    | Ok msg -> 
        return! Successful.OK msg next ctx
    | Error msg -> 
        return! RequestErrors.BAD_REQUEST msg next ctx
}

let getUserId (ctx: HttpContext) = 
    ctx.User.FindFirst(ClaimTypes.NameIdentifier).Value

let getQueryValue str (ctx: HttpContext) = 
    ctx.TryGetQueryStringValue(str)

