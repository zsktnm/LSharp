module Lsharp.Index

open Lsharp.Layout
open Giraffe.ViewEngine


let Index () =
    let markup = 
        div [ _class "content-wrapper"] [
            div[ _class "full-size" ] [
                h1 [ _class "title"] [str "Добро пожаловать!"]
                p [ _class "text-block"] [
                    rawText "Практикуйтесь в программировании на языке C# с помощью системы l-sharp. <br /> Быстро, легко и совершенно бесплатно!"
                ]
                a [ _class "button primary-color"; _href "/registration" ] [
                    str "Начать сейчас"
                ]
            ]
        ]

    Layout markup