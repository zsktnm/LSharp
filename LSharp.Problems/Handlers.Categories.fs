module LSharp.Problems.Handlers.Categories

open LSharp.Problems.Data
open LSharp.Problems.DataTransfer
open LSharp.Problems.Handlers.Common
open LSharp.Helpers.ImageSaving

open Giraffe
open Microsoft.AspNetCore.Http
open System.IO


let findCategoryResult id next ctx = task {
    match! getCategoryById id with
    | None -> 
        return! notFound "not found" next ctx
    | Some category -> 
        return! json category next ctx
}


let findCategoryHandler = 
   fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let id = ctx.TryGetQueryStringValue("id")
        match id with
        | None -> 
            return! notFound "not found" next ctx
        | Some id -> 
            return! findCategoryResult id next ctx
    } 


let getCategoriesHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let! categories = getAllCategories()
        return! json categories next ctx
    } 


let addCategoryHandler =  
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match! readDto<CategoryDTO> ctx with
        | Error errors -> 
            return! badRequest errors next ctx
        | Ok dto -> 
            do! addCategory (dto |> toCategory)
            return! Successful.NO_CONTENT next ctx
    }


let updateCategoryResult id dto next ctx = task {
    match! (updateCategory id dto) with
    | Ok _ -> return! Successful.NO_CONTENT next ctx
    | Error err -> return! badRequest err next ctx
}


let updateCategoryHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let id = ctx.TryGetQueryStringValue("id")
        let! dto = readDto<CategoryDTO> ctx
        return! 
            match (id, dto) with
            | (None, _) -> 
                badRequest "id is undefined" next ctx
            | (_, Error validationErrors) -> 
                badRequest validationErrors next ctx
            | (Some id, Ok dto) -> 
                updateCategoryResult id dto next ctx
    }


let updateCategoryImageAsync maybeId filenameTask = task {
    let! filename = filenameTask
    match (maybeId, filename) with
    | (Some id, Ok filename) -> return! updateCategoryImage filename id
    | (_, Error msg) -> return Error msg
    | (None, _) -> return Error "Invalid Id"
}


let updateCategoryImageHandler = 
    let getFilename() = 
        Path.Combine("images", Path.GetRandomFileName() + ".png")

    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let maybeId = ctx.TryGetQueryStringValue("id")
        match ctx.Request.ContentLength with
        | header when not header.HasValue -> 
            return! RequestErrors.BAD_REQUEST "No content-length header" next ctx
        | header when header.Value > maxImageSize -> 
            return! RequestErrors.BAD_REQUEST "Invalid size" next ctx
        | header -> 
            return! copyPngFile ctx.Request.Body header.Value (getFilename())
            |> updateCategoryImageAsync maybeId
            |> responseFromResult next ctx
    }

