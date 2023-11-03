namespace LSharp.Users

open LSharp.Mongodb.Mongo
open LSharp.Mongodb.BuildHelpers

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


    let findUserByIdAsync id = task {
        return! usersCollection 
                |> find {| _id = id |}
                |> oneAsync
    }


    let updateUserNameAsync newName id = task {
        return! usersCollection 
        |> updateOneAsync  
            {| _id = id |}
            (setFields {| name = newName |})
    }


    let updateUserAvatarAsync newPhoto id = task {
        return! usersCollection
        |> updateOneAsync 
            {| _id = id |}
            (setFields {| avatar = newPhoto |})
               
    }


    let updateUser id user = 
        if user._id <> id then
            Error "invalid Id"
        else
            usersCollection 
            |> replaceOne {| _id = id |} user

    
    let updateUserAsync id user = task {
        if user._id <> id then
            return Error "invalid Id"
        else
            return! usersCollection 
            |> replaceOneAsync {| _id = id |} user
    }


