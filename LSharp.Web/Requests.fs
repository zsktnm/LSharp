module Lsharp.Requests

open System
open System.Threading.Tasks
open System.Text.Json
open System.Net
open System.Net.Http
open System.Net.Http.Json
open LSharp.Helpers
open LSharp.Helpers.ActionResults


let (>~) l r = task {
    let! value = l
    return! r value
}  

let protect func = 
    try
        Some func
    with
    | _ -> None


let inRange (low, high) value = 
    value >= low && value <= high

let httpGet (url: string) = task { 
    use client = new HttpClient()
    return! client.GetAsync(url) 
}

let jsonContent content = 
    JsonContent.Create(content)

let httpPost (url: string) content = task {
    use client = new HttpClient()
    return! client.PostAsync(url, content) 
}


let private readResponse (response: HttpResponseMessage Task) (reader: HttpResponseMessage -> 'a Task) = task {
    use! r = response
    let! content = reader r
    match r with
    | _ when r.IsSuccessStatusCode -> 
        return Success content
    | _ when r.StatusCode |> int |> inRange (500, 599) -> 
        return InternalError content
    | _ when r.StatusCode = HttpStatusCode.NotFound -> 
        return NotFound content
    | _ -> return BadRequest content
}

let asString response = 
    readResponse response (fun r -> r.Content.ReadAsStringAsync())

let asJson<'a> response = 
    readResponse response (fun r -> r.Content.ReadFromJsonAsync<'a>())