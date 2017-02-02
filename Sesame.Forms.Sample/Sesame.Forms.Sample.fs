namespace Sesame.Forms.Sample

open Sesame
open Sesame.Forms

module Test =

    let exoplanetItemView (exoplanet: Exoplanet) =
        stackLayout [ verticalOptions LayoutOptions.Center ]
            [
                label [ text exoplanet.pl_hostname ]
            ]

    let rec createView () =
        let varList = VarList ([])
        let isRefreshing = Var.create false

        let kepler = new KeplerService ()

        let startCollectingPlanets () =
            async {
                isRefreshing.Set true

                let! exoplanets = kepler.GetConfirmedExoplanets ()

                varList.Clear ()
                exoplanets
                |> Seq.iter (fun exoplanet ->
                    varList.Add (Cell.View (exoplanetItemView exoplanet, 0.))
                )

                isRefreshing.Set false
            } |> Async.StartImmediate

        let appearing = Event.appearing startCollectingPlanets

        let refreshing = Event.refreshing startCollectingPlanets

        stackLayout [ horizontalOptions LayoutOptions.Center ] 
            [
                listView [ Dynamic.isRefreshing isRefreshing.Val; appearing; refreshing ] varList.Val
            ]

type App() as this =
    inherit Xamarin.Forms.Application()

    do
        Page.Content(Test.createView(), "Kepler's Confirmed Exoplanets", [ ]).Build(this)
