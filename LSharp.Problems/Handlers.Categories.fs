module LSharp.Problems.Handlers.Categories

open LSharp.Problems.Data
open LSharp.Problems.DataTransfer
open LSharp.Problems.Handlers.Common
open LSharp.Helpers.ImageSaving
open LSharp.Helpers.ActionResults
open LSharp.Helpers.Handlers

open Giraffe
open Microsoft.AspNetCore.Http
open System.IO
open System.Threading.Tasks
open System


let findCategoryHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        getQueryValue "id" ctx
        |> ActionResult.fromOption "Id is empty"    
        |> Task.FromResult
        |> ActionResult.bindTask (fun id -> getCategoryById id)
        |> actionResultTaskToResponse next ctx


let getCategoriesHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let! categories = getAllCategories()
        return! json categories next ctx
    } 

let addCategoryFromDto dto = task {
    do! addCategory (dto |> toCategory)
    return Success "Success"
}

let addCategoryHandler =  
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        readDto<CategoryDTO> ctx
        |> ActionResult.fromResultTask BadRequest
        |> ActionResult.bindTask (fun dto -> addCategoryFromDto dto)
        |> actionResultTaskToResponse next ctx


let updateCategoryResult id dto next ctx = 
    updateCategory id dto
    |> actionResultTaskToResponse next ctx


let updateCategoryHandler = 
    let asyncMapResult func result = task {
        let! result' = result
        return result' |> Result.map func
    }

    let idWithDto ctx id =
        readDto<CategoryDTO> ctx 
        |> asyncMapResult (fun dto -> (id, dto))
        |> ActionResult.fromResultTask BadRequest

    let (>>=) l r = ActionResult.bindTask r l

    fun (next: HttpFunc) (ctx: HttpContext) -> 
        getQueryValue "id" ctx
        |> ActionResult.fromOption "Id is empty"    
        |> Task.FromResult
        >>= idWithDto ctx
        >>= fun (id, dto) -> updateCategory id dto
        |> actionResultTaskToResponse next ctx


let updateCategoryImageHandler = 
    let getFilename() = 
        Path.Combine("images", Path.GetRandomFileName() + ".png")

    let checkContentLength id (header: Nullable<int64>) = 
        match header with
        | header when not header.HasValue -> 
            BadRequest "No content-length header"
        | header when header.Value > maxImageSize -> 
            BadRequest "Invalid size" 
        | header -> 
            Success (id, header.Value)

    let copyFile body len filename id = 
        copyPngFile body len filename
        |> ActionResult.bindTask (fun filename -> updateCategoryImage filename id)


    fun (next: HttpFunc) (ctx: HttpContext) -> 
        getQueryValue "id" ctx
        |> ActionResult.fromOption "Id is empty"
        |> ActionResult.bind (fun id -> checkContentLength id (ctx.Request.ContentLength))
        |> Task.FromResult
        |> ActionResult.bindTask (fun (id, length) -> copyFile ctx.Request.Body length (getFilename ()) id)
        |> actionResultTaskToResponse next ctx

