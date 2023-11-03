namespace LSharp.Helpers

open SixLabors.ImageSharp
open System.IO

module ImageSaving = 
    let [<Literal>] basePath = "wwwroot"

    let isValidPng (bytes: byte array) = 
        try
            let format = Image.DetectFormat(bytes)
            if format.DefaultMimeType = "image/png" then 
                Ok bytes
            else
                Error "Invalid file format"
        with
            | _ -> Error "Invalid file format"


    let emunerateStream (body: Stream) (len: int64) = seq {
        let mutable size = len
        let buf = Array.zeroCreate 102400

        while size > 0 do
            let count = body.Read(buf, 0, buf.Length)
            yield buf |> Array.take count
    }

    let bodyToBytesAsync (body: Stream) (len: int64) = task {
        try 
            let buffer = Array.zeroCreate (1024 * 16)
            use stream = new MemoryStream()

            let rec loop size = task {
                match size with
                | l when l <= 0L -> do ignore()
                | _ -> 
                        let! count = body.ReadAsync(buffer, 0, buffer.Length)
                        do! stream.WriteAsync(buffer, 0, count)
                        return! loop (size - (int64 count))
            }

            do! loop len
            return Ok (stream.ToArray())
        with
            | error -> return Error ("Error while copying " + error.Message)
    } 

    let copyPngFile (body: Stream) size filename = task {
        let (>>=) l r = Result.bind r l

        let saveToFile (bytes: byte array) = task {
            try
                do! File.WriteAllBytesAsync(Path.Combine(basePath, filename), bytes)
                return Ok filename
            with
                | err -> return Error $"Error while copying {err.Message}" 
        }

        try
            let! result = bodyToBytesAsync body size
            match (result >>= isValidPng) with
            | Error msg -> return Error msg
            | Ok bytes -> return! saveToFile bytes
        with
            | err -> return Error $"Error while copying {err.Message}" 
    }


    let copyFile (body: Stream) size filename = task {
        let saveToFile (bytes: byte array) = task {
            try
                do! File.WriteAllBytesAsync(Path.Combine(basePath, filename), bytes)
                return Ok filename
            with
                | err -> return Error $"Error while copying {err.Message}" 
        }
        try
            let! result = bodyToBytesAsync body size
            match result with
            | Error msg -> return Error msg
            | Ok bytes -> return! saveToFile bytes
        with
            | err -> return Error $"Error while copying {err.Message}"
    }

