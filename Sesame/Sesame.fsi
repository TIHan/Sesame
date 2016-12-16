namespace Sesame

open System

[<Sealed>]
type Val<'T>

[<Sealed>]
type Var<'T> =

    member Value : 'T

    member Set : 'T -> unit

    member Update : ('T -> 'T) -> unit

    member Val : Val<'T>

[<Sealed>]
type CmdVal<'T>

[<Sealed>]
type Cmd<'T> =

    member Execute : 'T -> unit

    member Val : CmdVal<'T>

[<RequireQualifiedAccess>]
module Var =

    val create : initialValue: 'T -> Var<'T>

[<RequireQualifiedAccess>]
module Val =

    val constant : 'T -> Val<'T>

    val map : ('T -> 'U) -> Val<'T> -> Val<'U>

    val mapList : ('T -> 'U) -> Val<'T list> -> Val<'U list>

    //val map2 : ('T1 -> 'T2 -> 'U) -> Val<'T1> -> Val<'T2> -> Val<'U>

[<RequireQualifiedAccess>]
module Cmd =

    val create : unit -> Cmd<_>

[<RequireQualifiedAccess>]
module CmdVal =

    val map : ('T -> 'U) -> CmdVal<'T> -> CmdVal<'U>

    val filter : ('T -> bool) -> CmdVal<'T> -> CmdVal<'T>

type Context =

    new : ((unit -> unit) -> unit) -> Context

    member Sink : 'Obj -> ('Obj -> 'T -> unit) -> Val<'T> -> unit

    member SinkCmd : 'Obj -> ('Obj -> 'T -> unit) -> CmdVal<'T> -> unit

    member AddDisposable : IDisposable -> unit

    member ClearDisposables : unit -> unit
