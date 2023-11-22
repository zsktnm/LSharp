namespace LSharp.Users

open LSharp.Mongodb.Mongo
open LSharp.Mongodb.BuildHelpers
open LSharp.Helpers.ActionResults

module Data = 
    let [<Literal>] connectionString = "mongodb://localhost:27017"
    let [<Literal>] databaseName = "LsharpData"
    let [<Literal>] collectionName = "users"


    [<CLIMutable>]
    type User = {
        _id: string;
        name: string;
        avatar: string;
        exp: int;
        next: int;
        level: int
    }


    let usersCollection = 
            connectionString 
            |> client 
            |> database databaseName
            |> collection<User> collectionName


    let findUserByIdAsync id =
        usersCollection 
        |> find {| _id = id |}
        |> oneAsync
        |> ActionResult.fromOptionTask "Id is not found"
    

    let updateUserNameAsync newName id = 
        usersCollection 
        |> updateOneAsync  
            {| _id = id |}
            (setFields {| name = newName |})
        |> ActionResult.fromResultTask InternalError


    let updateUserAvatarAsync newPhoto id =
        usersCollection
        |> updateOneAsync 
            {| _id = id |}
            (setFields {| avatar = newPhoto |})
        |> ActionResult.fromResultTask InternalError


    let updateUser id user = 
        if user._id <> id then
            BadRequest "invalid Id"
        else
            usersCollection 
            |> replaceOne {| _id = id |} user
            |> ActionResult.fromResult InternalError

    
    let updateUserAsync id user = task {
        if user._id <> id then
            return BadRequest "invalid Id"
        else
            return! usersCollection 
            |> replaceOneAsync {| _id = id |} user
            |> ActionResult.fromResultTask InternalError
    }


