open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers
open Newtonsoft.Json

type TZInfo = {
    zoneName: string; 
    minDiff: float; 
    localTime: string; 
    utcOffset: float
}

let getClosest () =
    let timezones = TimeZoneInfo.GetSystemTimeZones()

    let formattedTimezones = [
      for timezone in timezones do
        let localTimezone = TimeZoneInfo.ConvertTime(DateTime.Now, timezone)
        let fiveOClockPM = DateTime(localTimezone.Year, localTimezone.Month, localTimezone.Day, 17, 0, 0)   
        let minDifference = (localTimezone - fiveOClockPM).TotalMinutes

        yield {
            zoneName = timezone.StandardName
            minDiff = minDifference
            localTime = localTimezone.ToString("hh:mm tt")
            utcOffset = timezone.BaseUtcOffset.TotalHours
        }
    ]

    formattedTimezones
        |> List.filter (fun (i: TZInfo) -> i.minDiff >= 0.0)
        |> List.minBy (fun (i:TZInfo) -> i.minDiff)


let startServer (argv:string[]) = 
    let port = if argv.Length = 0 then 8080 else (int argv.[0])

    let cfg =
              { defaultConfig with
                  bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" port]}

    let app =
        choose
            [ GET >=> choose
                [ 
                    path "/" >=> request (fun _ -> OK <| JsonConvert.SerializeObject(getClosest()))
                        >=> setMimeType "application/json; charset=utf-8"
                ]
            ]

    startWebServer cfg app

[<EntryPoint>]
let main argv =
    startServer argv
    0
