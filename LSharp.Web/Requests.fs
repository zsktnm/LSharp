module Lsharp.Requests

open System
open System.Threading
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

let asString (response: HttpResponseMessage Task) = task {
    use! r = response
    let! content = r.Content.ReadAsStringAsync()
    match r with
    | _ when r.IsSuccessStatusCode -> 
        return Success content
    | _ when r.StatusCode |> int |> inRange (500, 599) -> 
        return InternalError content
    | _ when r.StatusCode = HttpStatusCode.NotFound -> 
        return NotFound content
    | _ -> return BadRequest content
}

let asJson<'a, 'b> (response: HttpResponseMessage Task) = task {
    use! r = response
    match r with
    | _ when r.IsSuccessStatusCode -> 
        let! result = r.Content.ReadFromJsonAsync<'a>()
        return Success result
    | _ when r.StatusCode |> int |> inRange (500, 599) -> 
        let! result = r.Content.ReadFromJsonAsync<'b>()
        return InternalError result
    | _ when r.StatusCode = HttpStatusCode.NotFound -> 
        let! result = r.Content.ReadFromJsonAsync<'b>()
        return InternalError result
    | _ -> 
        let! result = r.Content.ReadFromJsonAsync<'b>()
        return InternalError result
}