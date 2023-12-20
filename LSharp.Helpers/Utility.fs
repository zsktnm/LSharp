namespace LSharp.Helpers

module Utility =

    let taskBind func value = task {
        let! v = value
        return func v
    }