namespace Sesame

open System
open System.Collections.Generic

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
type ValList<'T> (subscribe, subscribeRemove, subscribeClear) =

    member val Subscribe : (int -> 'T -> unit) -> IDisposable = subscribe with get, set

    member val SubscribeRemove : (int -> unit) -> IDisposable = subscribeRemove with get, set

    member val SubscribeClear : (unit -> unit) -> IDisposable = subscribeClear with get, set

[<Sealed>]
type VarList<'T> (initialValue: IEnumerable<'T>) as this =

    let callbacks = ResizeArray<int -> 'T -> unit> ()
    let removeCallbacks = ResizeArray<int -> unit> ()
    let clearCallbacks = ResizeArray<unit -> unit> ()

    member val Value : ResizeArray<'T> = ResizeArray(initialValue) with get, set

    member this.Remove i =
        this.Value.RemoveAt (i)

        removeCallbacks
        |> Seq.iter (fun removeCallback -> removeCallback i)

    member this.Add item =
        let index = this.Value.Count
        this.Value.Add item

        callbacks
        |> Seq.iter (fun callback -> callback index item)

    member this.Clear () =
        let count = this.Value.Count
        this.Value.Clear ()

        clearCallbacks
        |> Seq.iter (fun callback -> callback ())

    member val Val =
        ValList<'T> (
            (fun callback ->
                let index = callbacks.Count
                callbacks.Add callback

                this.Value
                |> Seq.iteri (callback)

                {
                    new IDisposable with
                        member this.Dispose () =
                            callbacks.Remove callback |> ignore
                }
            ),
            (fun removeCallback ->
                removeCallbacks.Add removeCallback

                {
                    new IDisposable with
                        member this.Dispose () =
                            removeCallbacks.Remove removeCallback |> ignore
                }
            ),
            (fun clearCallback ->
                clearCallbacks.Add clearCallback

                {
                    new IDisposable with
                        member this.Dispose () =
                            clearCallbacks.Remove clearCallback |> ignore
                }
            )
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

    // This is a kludgy mess.
    member this.SinkList (o: 'Obj) f g h (va: ValList<'T>) =
        let weak = WeakReference (o)
        let subscriptionRef : Ref<Option<IDisposable>> = ref None
        let subscriptionRef2 : Ref<Option<IDisposable>> = ref None
        let subscriptionRef3 : Ref<Option<IDisposable>> = ref None

        let update index value =
            if weak.IsAlive then
                f (weak.Target :?> 'Obj) index value
                //GC.Collect() // debug purposes
            else
                match !subscriptionRef with
                | Some d -> d.Dispose ()
                | _ -> ()

                match !subscriptionRef2 with
                | Some d -> d.Dispose ()
                | _ -> ()

                match !subscriptionRef3 with
                | Some d -> d.Dispose ()
                | _ -> ()

        let updateRemove index =
            if weak.IsAlive then
                g (weak.Target :?> 'Obj) index
                //GC.Collect() // debug purposes
            else
                match !subscriptionRef with
                | Some d -> d.Dispose ()
                | _ -> ()

                match !subscriptionRef2 with
                | Some d -> d.Dispose ()
                | _ -> ()

                match !subscriptionRef3 with
                | Some d -> d.Dispose ()
                | _ -> ()

        let updateClear () =
            if weak.IsAlive then
                h (weak.Target :?> 'Obj)
                //GC.Collect() // debug purposes
            else
                match !subscriptionRef with
                | Some d -> d.Dispose ()
                | _ -> ()

                match !subscriptionRef2 with
                | Some d -> d.Dispose ()
                | _ -> ()

                match !subscriptionRef3 with
                | Some d -> d.Dispose ()
                | _ -> ()

        let callback =
            fun index value ->
                if syncContext = System.Threading.SynchronizationContext.Current then
                    update index value
                else
                    invokeOnMainThread(fun () -> update index value)

        let removeCallback =
            fun index ->
                if syncContext = System.Threading.SynchronizationContext.Current then
                    updateRemove index
                else
                    invokeOnMainThread(fun () -> updateRemove index)

        let clearCallback =
            fun () ->
                if syncContext = System.Threading.SynchronizationContext.Current then
                    updateClear ()
                else
                    invokeOnMainThread(fun () -> updateClear ())

        subscriptionRef := Some (va.Subscribe callback)
        subscriptionRef2 := Some (va.SubscribeRemove removeCallback)
        subscriptionRef3 := Some (va.SubscribeClear clearCallback)

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