module PgPriceSeries

open Feliz
open Feliz.Recharts
open Feliz.UseDeferred
open WgEdit
open Lib

type DataPoint = { date: string; temperature: int; humidity: int }

type GesSeriePoint = { date: string; amountmwh: float; totalprice: float }

let sampleData = [
    { date = "2025-10-01"; temperature = 18; humidity = 60 }
    { date = "2025-10-02"; temperature = 21; humidity = 55 }
    { date = "2025-10-03"; temperature = 19; humidity = 70 }
    { date = "2025-10-04"; temperature = 23; humidity = 65 }
    { date = "2025-10-05"; temperature = 20; humidity = 62 }
]


[<ReactComponent>]
let PgPriceSeries() =
    let (fromdate, setFromdate) = React.useState(System.DateTime.Now.AddYears(-1))
    let (todate, setTodate) = React.useState(System.DateTime.Now)
    let (gasSeries, setgasSeries) = React.useState([])
    
    let loadGas (fromTo : System.DateTime * System.DateTime) = 
        let (fromDate, toDate) = fromTo  // Destructure the tuple
        Api.PriceSeries.loadGas (Utils.localDateTimeToUnixTime fromdate) (Utils.localDateTimeToUnixTime todate)

    let loadGasSerie =
        React.useDeferredCallback(loadGas,
            (fun x ->
                match x with
                | Deferred.HasNotStartedYet -> ()
                | Deferred.InProgress -> ()
                | Deferred.Failed exn ->  ()
                | Deferred.Resolved content ->
                    match content with
                        | Ok gasPriceSeries ->
                            let gasSerie = 
                                gasPriceSeries 
                                |> List.map ( fun x ->
                                    let sliceend = Utils.unixTimeToLocalDateTime(x.SliceEnd)
                                    { 
                                        date = sliceend.ToString("yyyy-MM-dd")
                                        amountmwh = x.AmountMwh
                                        totalprice = x.TotalPrice
                                    }
                                )
                            setgasSeries gasSerie
                        | Error error -> ()
            )
        )


    Html.div [
        // prop.style [ 
        //     style.width (length.percent 100)
        //     // style.height 400 // or style.height (length.percent 100) for full height
        //     style.height (length.percent 100)
        // ]
        prop.children [
            Html.div [
                WgDateTime "fromdate" fromdate setFromdate
                WgDateTime "todate" todate setTodate
                WgButton "run" ( fun () -> loadGasSerie (fromdate,todate) )
            ]
            Html.div [
                prop.style [ 
                    style.width (length.percent 100)
                    // style.height 400 
                ]
                prop.children [
                    Recharts.responsiveContainer [
                        responsiveContainer.width (length.percent 100)
                        // responsiveContainer.height (length.percent 100)
                        responsiveContainer.height 400
                        responsiveContainer.chart(
                            Recharts.lineChart [
                                // lineChart.data sampleData
                                // lineChart.children [
                                //     Recharts.cartesianGrid [ cartesianGrid.strokeDasharray [| 3; 3 |] ]
                                //     Recharts.xAxis [ xAxis.dataKey (fun p -> p.date) ]
                                //     Recharts.yAxis []
                                //     Recharts.tooltip []
                                //     Recharts.legend []
                                //     Recharts.line [
                                //         line.dataKey (fun p -> p.temperature)
                                //         line.stroke "red"
                                //         line.name "Temperature"
                                //     ]
                                //     Recharts.line [
                                //         line.dataKey (fun p -> p.humidity)
                                //         line.stroke "blue"
                                //         line.name "Humidity"
                                //     ]
                                // ]
                                lineChart.data gasSeries
                                lineChart.children [
                                    Recharts.cartesianGrid [ cartesianGrid.strokeDasharray [| 3; 3 |] ]
                                    Recharts.xAxis [ xAxis.dataKey (fun p -> p.date) ]
                                    Recharts.yAxis []
                                    Recharts.tooltip []
                                    Recharts.legend []
                                    Recharts.line [
                                        line.dataKey (fun p -> p.amountmwh)
                                        line.stroke "red"
                                        line.name "amount mwh"
                                    ]
                                    // Recharts.line [
                                    //     line.dataKey (fun p -> p.totalprice)
                                    //     line.stroke "blue"
                                    //     line.name "Total price"
                                    // ]
                                ]
                            ]
                        )
                    ]
                ]
            ]
        ]
    ]
