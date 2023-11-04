namespace LSharp.Mongodb
open MongoDB.Driver

module BuildHelpers = 
    let oid id = {| ``$oid`` = id.ToString() |}

    let setFields object = {| ``$set`` = object |}

    let pushTo object = 
        {|
            ``$push`` = object
        |}