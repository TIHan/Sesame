namespace Sesame

open System

[<Sealed>]
type Val<'T> (subscribe) =

    member val Subscribe : ('T -> unit) -> IDisposable = subscribe with get, set 

[<Sealed>]
type Var<'T> (initialValue) as this =

    let callbacks = ResizeArray<'T -> unit> ()

    member val Value = initialValue with get, set

    member this.Set value =
        this.Value <- value
        this.Notify ()

    member this.Update f =
        this.Value <- f this.Value
        this.Notify ()

    member this.Notify () =
        callbacks
        |> Seq.toList
        |> Seq.iter (fun f -> f this.Value)

    member val Val =
        Val<'T> (fun callback ->
            callbacks.Add callback
            callback this.Value
            {
                new IDisposable with
                    member this.Dispose () =
                        callbacks.Remove callback |> ignore
            }
        )

[<Sealed>]
type CmdVal<'T> (subscribe) =

    member val Subscribe : ('T -> unit) -> IDisposable = subscribe with get, set 

[<Sealed>]
type Cmd<'T> () =

    let mutable callback = fun _ -> ()

    member this.Execute value =
        callback value

    member val Val =
        CmdVal<'T> (fun callback' ->
            callback <- callback'
            {
                new IDisposable with
                    member this.Dispose () =
                        callback <- fun _ -> ()
            }
        )

[<RequireQualifiedAccess>]
module Var =

    let create initialValue =
        Var (initialValue)

[<RequireQualifiedAccess>]
module Val =
 
    let constant value =
        Val<'T> (fun callback -> 
            callback value
            {
                new IDisposable with
                    member this.Dispose () = ()
            }
        )

    let map f (va: Val<'T>) =
        Val<_> (fun callback ->
            va.Subscribe (fun x -> callback (f x))
        )

    let mapList f (va: Val<'T list>) =
        Val<_> (fun callback ->
            va.Subscribe (fun x ->
                callback (List.map f x)
            )
        )

[<RequireQualifiedAccess>]
module Cmd =

    let create () =
        Cmd ()

[<RequireQualifiedAccess>]
module CmdVal =

    let map f (va: CmdVal<'T>) =
        CmdVal<_> (fun callback ->
            va.Subscribe (fun x -> callback (f x))
        )

    let filter f (va: CmdVal<'T>) =
        CmdVal<_> (fun callback ->
            va.Subscribe (fun x ->
                if f x then
                    callback x
            )
        )

type Context (invokeOnMainThread) =

    let disposables = ResizeArray<IDisposable> ()

    let syncContext = System.Threading.SynchronizationContext.Current

    member this.Sink (o: 'Obj) f (va: Val<'T>) =
        let weak = WeakReference (o)
        let subscriptionRef : Ref<Option<IDisposable>> = ref None

        let update value =
            if weak.IsAlive then
                f (weak.Target :?> 'Obj) value
                //GC.Collect() // debug purposes
            else
                match !subscriptionRef with
                | Some d -> d.Dispose ()
                | _ -> ()

        let callback =
            fun value ->
                if syncContext = System.Threading.SynchronizationContext.Current then
                    update value
                else
                    invokeOnMainThread(fun () -> update value)

        subscriptionRef := Some (va.Subscribe callback)

    member this.SinkCmd (o: 'Obj) f (va: CmdVal<'T>) =
        let weak = WeakReference (o)
        let subscriptionRef : Ref<Option<IDisposable>> = ref None

        let update value =
            if weak.IsAlive then
                f (weak.Target :?> 'Obj) value
                //GC.Collect() // debug purposes
            else
                match !subscriptionRef with
                | Some d -> d.Dispose ()
                | _ -> ()

        let callback =
            fun value ->
                if syncContext = System.Threading.SynchronizationContext.Current then
                    update value
                else
                    invokeOnMainThread(fun () -> update value)

        subscriptionRef := Some (va.Subscribe callback)

    member this.AddDisposable x =
        disposables.Add(x)

    member this.ClearDisposables () =
        disposables
        |> Seq.iter (fun x -> x.Dispose ())
        disposables.Clear ()