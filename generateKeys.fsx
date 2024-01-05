open System.IO
open System.Security.Cryptography

// key size
let [<Literal>] keySize = 2048

// Identity project
let identityPath = "LSharp.Identity"

// projects with public key
let catalogs = [
    "LSharp.Problems";
    "LSharp.Users";
    "LSharp.CodeRunner"
]


let writeFile catalog filename text = File.WriteAllText(Path.Combine(catalog, filename), text)
let publicKeyOf (rsa: RSA) = rsa.ToXmlString(false)
let privateKeyOf (rsa: RSA) = rsa.ToXmlString(true)


let rsa = RSA.Create(keySize)

writeFile identityPath "private.xml" (privateKeyOf rsa)
writeFile identityPath "public.xml" (publicKeyOf rsa)

catalogs
|> List.iter (fun path -> writeFile path "public.xml" (publicKeyOf rsa))
