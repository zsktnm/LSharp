module Lsharp.Layout

open Giraffe
open Giraffe.ViewEngine


let Layout content = 
    let headSection () = head[] [
        link [ _rel "stylesheet"; _href "/libs/bulma/css/bulma.css" ]
        link [ _rel "stylesheet"; _href "/styles/main.css" ]
    ]

    let brand () = div [ _class "navbar-brand" ] [
        a [ _class "navbar-item"; _href "/" ] [
            img [ _src "/img/logo.png" ]
        ]
    ]

    let buttons () = div [_class "navbar-end"] [
        div [ _class "navbar-item" ] [
            div [ _class "buttons" ] [
                a [ _class "button is-primary"; _href "/registration" ] [
                    str "Регистрация"
                ]
                a [ _class "button is-light"; _href "/login" ] [
                    str "Вход в систему"
                ]
            ]
        ]
    ]
        

    html [] [
        headSection ()
        body[] [
            header [ _class "box" ] [
                nav [_class "navbar"] [
                    brand ()
                    buttons()
                ]
            ]
            content
            
        ]
    ]


