namespace LSharp.Users

// TODO: load from settings/database
module LevelUp = 
    type LevelInfo = {
        Exp: int;
        Next: int;
        Level: int;
    }

    let levels = 
        [ 
            100;
            100;
            125;
            175;
            200;
            250;
            300;
            375;
            500;
            750;
        ]

    let aggrLevels = 
        levels
        |> List.mapFold (fun state value -> state + value, state + value) 0
        |> fst
        |> List.mapi (fun index levelExp -> (levelExp, index + 1))

    let getLevel exp = 
        aggrLevels
        |> Seq.takeWhile (fun (levelExp, level) -> levelExp <= exp)
        |> Seq.tryLast 
        |> function 
            | None -> 0
            | Some (_, level) -> level
    
    let getExp exp = exp

    let getNextExp exp = 
        aggrLevels
        |> Seq.skipWhile (fun (levelExp, level) -> levelExp <= exp)
        |> Seq.tryHead
        |> function 
            | None -> -1
            | Some (nextExp, _) -> nextExp


    let getLevelInfo exp = {
        Exp = getExp exp;
        Next = getNextExp exp;
        Level = getLevel exp;
    }