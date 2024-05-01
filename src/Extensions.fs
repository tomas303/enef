[<AutoOpen>]
module Extensions

open Elmish

module Cmd =
  let fromAsync (operation : Async<'msg>) : Cmd<'msg> =
    let delayedCmd (dispatch : 'msg -> unit) : unit =
      let delayedDispatch =
        async {
          let! msg = operation
          dispatch msg
        }

      Async.StartImmediate delayedDispatch

    Cmd.ofEffect delayedCmd

type Deferred<'t> =
  | HasNotStartedYet
  | InProgress
  | Resolved of 't

type AsyncOperationEvent<'t> =
  | Started
  | Finished of 't

module Async =
  let map<'t, 'u> (mapping : 't -> 'u) (input : Async<'t>) : Async<'u> =
    async {
      let! x = input
      return mapping x
    }
