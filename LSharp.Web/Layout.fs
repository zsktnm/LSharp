module Lsharp.Layout

open Giraffe
open Giraffe.ViewEngine


let Layout content = 
    let headSection () = head[] [
        link [ _rel "stylesheet"; _href "/styles/main.css" ]
    ]

    let brand () = div [ _class "navbar-brand" ] [
        a [ _href "/" ] [
            img [ _src "/images/lsharp_logo.png" ]
        ]
    ]

    let buttons () = div [_class "navbar-buttons"] [
        a [ _class "button primary-color"; _href "/login" ] [
            str "Вход в систему"
        ]
        a [ _class "button primary-outlined"; _href "/registration" ] [
            str "Регистрация"
        ]
    ]
        

    html [] [
        headSection ()
        body[] [
            header [ _class "box-shadow" ] [
                nav [_class "navbar"] [
                    brand ()
                    buttons()
                ]
            ]
            main [] [
                content
            ]
            
        ]
    ]


