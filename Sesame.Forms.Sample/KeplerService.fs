namespace Sesame.Forms.Sample

open System
open System.Net.Http
open System.Collections.Generic

type Exoplanet =
    {
        pl_hostname: string
    }

type KeplerService () =

    let url = "http://exoplanetarchive.ipac.caltech.edu/cgi-bin/nstedAPI/nph-nstedAPI?table=exoplanets&format=json"
    let http = new HttpClient ()

    member this.GetConfirmedExoplanets () = 
        async {
            use req = new HttpRequestMessage (HttpMethod.Get, url)

            use! resp = http.SendAsync (req) |> Async.AwaitTask
            let! data = resp.Content.ReadAsStringAsync () |> Async.AwaitTask

            return Newtonsoft.Json.JsonConvert.DeserializeObject<Exoplanet seq> (data)
        }


