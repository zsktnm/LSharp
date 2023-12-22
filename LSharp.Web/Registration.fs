module Lsharp.Registration

open Giraffe.ViewEngine
open Layout


[<CLIMutable>]
type RegistrationViewModel = {
    Email: string;
    Password: string;
    RepeatPassword: string;
    Errors: string list;
}
 
let newViewModel () = {
    Email = ""
    Password = ""
    RepeatPassword = ""
    Errors = []
}

let Registration (viewModel: RegistrationViewModel) = 
    let markup = 
        div [ _class "content-wrapper" ] [
            form [ _class "vertical-stack max-1200"; _method "POST" ] [
                h1 [ _class "title" ] [ str "Регистрация" ]
                label [ _class "label" ] [ str "Email: " ]
                input [ 
                    _class "input" 
                    _type "email" 
                    _placeholder "mail@example.com"
                    _name "Email" 
                    _value viewModel.Email
                ]
                label [ _class "label" ] [ str "Пароль: " ]
                input [ 
                    _class "input"
                    _type "password"
                    _placeholder "Укажите надежный пароль"
                    _name "Password" 
                    _value viewModel.Password
                    _required
                ]
                label [ _class "label" ] [ str "Повторите пароль: " ]
                input [ 
                    _class "input" 
                    _type "password" 
                    _placeholder "Повторите свой надежный пароль" 
                    _name "RepeatPassword" 
                    _value viewModel.RepeatPassword
                    _required
                ]

                ul [] (viewModel.Errors 
                    |> List.map (fun err -> li [] [str err]))

                button [ _class "button primary-color" ] [ str "Зарегистрироваться" ]

            ]
        ]

    Layout markup