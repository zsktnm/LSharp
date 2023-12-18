module Lsharp.Views

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

let Index () =
    let markup = 
        div [ _class "block container"] [
            h1 [ _class "title"] [str "Добро пожаловать!"]
            p [ _class "content"] [
                rawText "Практикуйтесь в программировании на языке C# с помощью системы l-sharp. <br /> Быстро, легко и совершенно бесплатно!"
            ]
            a [ _class "button is-primary"; _href "/registration" ] [
                str "Начать сейчас"
            ]
        ]

    Layout markup


let Registration () = 
    let markup = 
        form [ _class "container" ] [
            h1 [ _class "title" ] [ str "Регистрация" ]
            div [ _class "field" ] [
                label [ _class "label" ] [ str "Email: " ]
                div [ _class "control" ] [
                    input [ _class "input"; _type "text"; _placeholder "mail@example.com" ]
                ]
            ]
            div [ _class "field" ] [
                label [ _class "label" ] [ str "Пароль: " ]
                div [ _class "control" ] [
                    input [ _class "input"; _type "password"; _placeholder "Укажите надежный пароль" ]
                ]
            ]
            div [ _class "field" ] [
                label [ _class "label" ] [ str "Повторите пароль: " ]
                div [ _class "control" ] [
                    input [ _class "input"; _type "password"; _placeholder "Повторите свой надежный пароль" ]
                ]
            ]
            div [ _class "field" ] [
                div [ _class "control" ] [
                    button [ _class "button is-primary mt-5" ] [ str "Зарегистрироваться" ]
                ]
            ]
        ]

    Layout markup

