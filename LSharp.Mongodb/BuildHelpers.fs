namespace LSharp.Mongodb
open MongoDB.Driver
open Mongo

module BuildHelpers = 
    let oid id = {| ``$oid`` = id.ToString() |}

    let setFields object = {| ``$set`` = object |}

