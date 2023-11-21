module LSharp.CodeRunner.Containers

open System.IO
open System.Diagnostics
open System.Threading

// TODO: use settings

let basePath = @"C:\AppTest\"
let baseFolder = @"C:\AppTest\base"
let codefile = "Code.cs"
let testfile = "Tests.cs"
let imageName = "testimage"

let copyFolder () = 
    let newFolder = Path.Combine(basePath, Path.GetRandomFileName())
    Directory.CreateDirectory(newFolder) |> ignore
    Directory.GetFiles(baseFolder)
    |> Array.iter (fun file -> 
        File.Copy(file, Path.Combine(newFolder, Path.GetFileName(file))))
    newFolder

let removeFolder folder = 
    Directory.Delete(folder, true)

let copyCode code test folder = 
    File.WriteAllText(Path.Combine(folder, codefile), code)
    File.WriteAllText(Path.Combine(folder, testfile), test)

let startTests folder = task {
    // TODO: use cancellation token
    // TODO: use stdout to get test errors
    let startInfo = ProcessStartInfo("docker", 
        $"run --rm -v {folder}:/app {imageName}")
    startInfo.RedirectStandardOutput <- true
    startInfo.UseShellExecute <- false
    let proc = Process.Start(startInfo)
    do! proc.WaitForExitAsync()
    match proc.ExitCode with
    | 0 -> return Ok (proc.StandardOutput.ReadToEnd())
    | _ -> return Error (proc.StandardOutput.ReadToEnd())
}

let executeTests code tests = task {
    let folder = copyFolder ()
    do copyCode code tests folder
    match! startTests folder with
    | Error err -> 
        do removeFolder folder
        return Error err
    | Ok msg -> 
        do removeFolder folder
        return Ok msg
}