﻿module SymbolView

open Fable.React
open Fable.React.Props
open Elmish


open CommonTypes
open DrawHelpers
open DrawModelType.SymbolT
open Symbol


//-----------------------------------------DRAWING HELPERS ---------------------------------------------------

/// Text adding function with many parameters (such as bold, position and text)
let addText (pos: XYPos) name alignment weight size =
    let text =
            {defaultText with TextAnchor = alignment; FontWeight = weight; FontSize = size}
    [makeText pos.X pos.Y name text]

/// Add one or two lines of text, two lines are marked by a . delimiter
let addLegendText (pos: XYPos) (name:string) alignment weight size =
    let text =
            {defaultText with TextAnchor = alignment; FontWeight = weight; FontSize = size}
    match name.Split([|','|]) with
    | [|oneLine|] -> 
        [makeText pos.X pos.Y name text]
    | [|topLine;bottomLine|] ->
        [makeText pos.X pos.Y topLine text;
         makeText pos.X (pos.Y+Constants.legendLineSpacingInPixels) bottomLine text]
    | _ ->
        failwithf "addLegendText does not work with more than two lines demarcated by ,"

let addCompValue (symbol: Symbol) (name:string)  weight size =
    let symbolPos = {X=0.;Y=0.}
    let h,w = getRotatedHAndW symbol
    let pos,align = 
        match symbol.STransform.Rotation with
        |Degree0 |Degree180 -> symbolPos + {X=(5.*w/8.);Y=(-20.)},"left"
        |Degree270 |Degree90 -> symbolPos + {X=(w+3.);Y=0.75*h},"left"
        //|Degree90 -> symbolPos + {X=(-10.);Y=0.75*h},"right"
    
    let text =
            {defaultText with TextAnchor = align; FontWeight = weight; FontSize = size}
    [makeText pos.X pos.Y name text]
    

let addStyledText (style:Text) (pos: XYPos) (name: string) = 
    makeText pos.X pos.Y name style

/// Generate circles on ports
let inline private portCircles (pos: XYPos) (show:ShowPorts)= 
    [makeCircle pos.X pos.Y portCircle]

/// Puts name on ports
let private portText (pos: XYPos) name edge =
    let pos' = 
            match edge with 
            | Left -> pos + {X = 5.; Y = -6.}
            | Top -> pos + {X = 0.; Y = 5.}
            | Right -> pos + {X = -5.; Y = -6.}
            | Bottom -> pos + {X = 0.; Y = -15.}

    let align = 
            match edge with
            | Right -> "end"
            | Left -> "start"
            | _ -> "middle"
    (addText pos' name align Constants.portTextWeight Constants.portTextSize)


/// Print the name of each port 
let drawPortsText (portList: list<Port>) (listOfNames: list<string>) (symb: Symbol) = 
    let getPortName name x = portText (getPortPosToRender symb portList[x]) name (symb.PortMaps.Orientation[portList.[x].Id])
    if listOfNames.Length < 1
    then []
    else 
        [0..(portList.Length-1)]
        |> List.map2 getPortName listOfNames 
        |> List.collect id

/// Function to draw ports using getPortPos. The ports are equidistant     
let drawPorts (portList: Port List) (showPorts:ShowPorts) (symb: Symbol)= 
    if not (portList.Length < 1) then       
        match (showPorts) with
        |ShowBoth -> [0..(portList.Length-1)] |> List.collect (fun x -> (portCircles (getPortPosToRender symb portList[x]) showPorts ))  
        |(_) -> []
    else []


//------------------------------HELPER FUNCTIONS FOR DRAWING SYMBOLS-------------------------------------
let private createPolygon points colour opacity = 
    [makePolygon points {defaultPolygon with Fill = colour; FillOpacity = opacity}]

let createBiColorPolygon points colour strokeColor opacity strokeWidth (comp:Component)= 
    if strokeColor <> "black" then 
        [makePolygon points {defaultPolygon with Fill = colour; Stroke = strokeColor; FillOpacity = opacity; StrokeWidth=strokeWidth}]
    else   
        [makePolygon points {defaultPolygon with Fill = colour; FillOpacity = opacity; StrokeWidth = strokeWidth}]

let addClock (pos: XYPos) colour opacity =
    let points = sprintf $"{pos.X},{pos.Y-1.},{pos.X+8.},{pos.Y-7.},{pos.X},{pos.Y-13.}"
    createPolygon points colour opacity
    |> List.append (addText (pos + {X = 10.; Y = -13.} ) " clk" "start" "normal" "12px")

let addHorizontalLine posX1 posX2 posY opacity = // TODO: Line instead of polygon?
    let points = sprintf $"{posX1},{posY},{posX2},{posY}"
    createPolygon points "lightgray" opacity

let outlineColor (color:string) =
    match color.ToLower() with
    | "lightgray" |"lightblue" | "#E8D0A9" | "rgba(255,255,0,0.15)"  -> "black"
    | c -> c

let addHorizontalColorLine posX1 posX2 posY opacity (color:string) = // TODO: Line instead of polygon?
    let points = sprintf $"{posX1},{posY} {posX2},{posY}"
    let outlineColor = outlineColor color
    [makePolygon points {defaultPolygon with Fill = "olcolor"; Stroke=outlineColor; StrokeWidth = "2.0"; FillOpacity = opacity}]

/// Takes points, height and width of original shape and returns the points for it given a rotation / flipped status.
/// Degree0 rotation has TopLeft = top left coordinate of the outline, which is a box of dimensions W X H.
/// Rotation rotates the box about its centre point, keeping TopLeft fixed.
let rotatePoints (points) (centre:XYPos) (transform:STransform) = 
    let offset = 
            match transform.Rotation with
            | Degree0 | Degree180 -> centre
            | Degree90 | Degree270 -> {X = centre.Y; Y = centre.X}

    let relativeToCentre = Array.map (fun x -> x - centre)
    let rotateAboutCentre pointsIn = 
        match transform.Rotation with
        | Degree0   -> pointsIn
        | Degree270 -> Array.map (fun (pos:XYPos) -> {X = -pos.Y ; Y = pos.X}) pointsIn
        | Degree180 -> Array.map (fun (pos:XYPos) -> {X = -pos.X ; Y = -pos.Y}) pointsIn
        | Degree90  -> Array.map (fun (pos:XYPos) -> {X = pos.Y ; Y = -pos.X}) pointsIn

    let relativeToTopLeft = Array.map (fun x -> x + offset ) 
    /// Flips the points, needed some hacks to avoid saving transforms somewhere / saving current points
    /// Also can't guarantee it will work if there are changes to rotation / flip with funkier shapes
    let flipIfNecessary pts =
        if not transform.flipped then pts
        else
            match transform.Rotation with
            | _ -> Array.map (fun (point:XYPos) -> {X = -point.X; Y = point.Y}) pts

    points
    |> relativeToCentre
    |> rotateAboutCentre
    |> flipIfNecessary
    |> relativeToTopLeft



/// --------------------------------------- SYMBOL DRAWING ------------------------------------------------------ ///  

let drawSymbol (symbol:Symbol) (theme:ThemeType) =
    let appear = symbol.Appearance
    let colour = appear.Colour
    let showPorts = appear.ShowPorts
    let opacity = appear.Opacity
    let comp = symbol.Component
    let h,w = getRotatedHAndW symbol
    let H = float comp.H*(Option.defaultValue 1.0 symbol.VScale)
    let W = float comp.W*(Option.defaultValue 1.0 symbol.HScale)
    let transform = symbol.STransform


    /// Points that define the edges of the symbol
    let points =
        let toString = Array.fold (fun x (pos:XYPos) -> x + (sprintf $" {pos.X},{pos.Y}")) "" 
        let originalPoints =
            match comp.Type with
            | Resistor _ ->
                [|{X=0;Y=0.5*H};{X=0.125*W;Y=0.5*H};{X=0.1875*W;Y=0};{X=0.3125*W;Y=H};{X=0.4375*W;Y=0};{X=0.5625*W;Y=H};{X=0.6875*W;Y=0};{X=0.8125*W;Y=H};{X=0.875*W;Y=0.5*H};{X=W;Y=0.5*H};{X=0.875*W;Y=0.5*H};{X=0.8125*W;Y=H};{X=0.6875*W;Y=0};{X=0.5625*W;Y=H};{X=0.4375*W;Y=0};{X=0.3125*W;Y=H};{X=0.1875*W;Y=0};{X=0.125*W;Y=0.5*H}|]
            | Ground ->
                [|{X=0;Y=0.3*H};{X=0.5*W;Y=H};{X=W;Y=0.3*H};{X=0.5*W;Y=0.3*H};{X=0.5*W;Y=0};{X=0.5*W;Y=0.3*H}|]
            | DiodeL |DiodeR ->
                [|{X=0;Y=0};{X=0;Y=H};{X=W;Y=0.5*H};{X=W;Y=H};{X=W;Y=0};{X=W;Y=0.5*H}|]
            | Opamp ->
                [|{X=0;Y=0};{X=0;Y=H};{X=W;Y=H/2.} |]   
            | _ -> // other symbols will be created explicitly below in 'createdSymbol'
                [|{X=0;Y=0};{X=0;Y=H};{X=W;Y=H};{X=W;Y=0}|]
        rotatePoints originalPoints {X=W/2.;Y=H/2.} transform
        |> toString 


    let outlineColour, strokeWidth =
        match colour,comp.Type with
        |"Red",Ground |"Red",DiodeL |"Red",Opamp |"Red",DiodeR -> "black", "1.0"
        |"Red",_ -> "Red", "2.5"
        |_,Resistor _  -> "darkblue", "2.5"
        | _ -> "black", "1.0"


    let symbolLine = {defaultLine with Stroke = outlineColour; StrokeWidth = strokeWidth}
    let symbolCircle = {defaultCircle with Stroke = outlineColour; StrokeWidth = strokeWidth}
    let capacitorLine = {symbolLine with StrokeWidth = "2.5px";}
    
    let createdSymbol = 
        match comp.Type with
        | CurrentSource _ -> 
            match transform.Rotation with 
            | Degree0 -> [makeCircle 30 30 {symbolCircle with R=30.0} ; makeLine 15 30 45 30 symbolLine; makeLine 35 20 45 30 symbolLine; makeLine 35 40 45 30 symbolLine]
            | Degree90 -> [makeCircle 30 30 {symbolCircle with R=30.0} ; makeLine 30 45 30 15 symbolLine; makeLine 20 25 30 15 symbolLine; makeLine 40 25 30 15 symbolLine]
            | Degree180 -> [makeCircle 30 30 {symbolCircle with R=30.0} ; makeLine 45 30 15 30 symbolLine; makeLine 25 40 15 30 symbolLine; makeLine 25 20 15 30 symbolLine]
            | Degree270 -> [makeCircle 30 30 {symbolCircle with R=30.0} ; makeLine 30 15 30 45 symbolLine; makeLine 20 35 30 45 symbolLine; makeLine 40 35 30 45 symbolLine]
        | VoltageSource _ -> 
            match transform.Rotation with
            | Degree0 -> [makeCircle 30 30 {symbolCircle with R=30.0} ; makeLine 40 30 55 30 symbolLine; makeLine 47.5 37.5 47.5 22.5 symbolLine; makeLine 12.5 37.5 12.5 22.5 symbolLine]
            | Degree90 -> [makeCircle 30 30 {symbolCircle with R=30.0} ; makeLine 22.5 50 37.5 50 symbolLine; makeLine 22.5 15 37.5 15 symbolLine; makeLine 30 22.5 30 7.5 symbolLine]
            | Degree180 -> [makeCircle 30 30 {symbolCircle with R=30.0} ; makeLine 45 37.5 45 22.5 symbolLine; makeLine 15 37.5 15 22.5 symbolLine; makeLine 7.5 30 22.5 30 symbolLine]
            | Degree270 -> [makeCircle 30 30 {symbolCircle with R=30.0} ; makeLine 22.5 45 37.5 45 symbolLine; makeLine 30 37.5 30 52.5 symbolLine; makeLine 22.5 15 37.5 15 symbolLine ]
        |Capacitor _ ->
            match transform.Rotation with
            | Degree0 | Degree180 -> [makeLine 0 15 25 15 capacitorLine; makeLine 25 0 25 30 capacitorLine;makeLine 35 0 35 30 capacitorLine;makeLine 35 15 60 15 capacitorLine]
            | Degree90 | Degree270 -> [makeLine 15 60 15 35 capacitorLine; makeLine 0 35 30 35 capacitorLine; makeLine 0 25 30 25 capacitorLine; makeLine 15 25 15 0 capacitorLine]
        | Inductor _ -> 
            match transform.Rotation with
            | Degree0 | Degree180 ->
                let arcs = [makePartArcAttr 10 10 10 10 10;makePartArcAttr 10 10 10 10 10;makePartArcAttr 10 10 10 10 10]
                let startingPoint = {X=15;Y=15}

                let renderedSegmentList : ReactElement List = 
                    arcs
                    |> String.concat " "
                    |> (fun attr -> [makeAnyPath startingPoint attr {defaultPath with StrokeWidth = "2.5px"; Stroke = outlineColour}])
                renderedSegmentList
                |> List.append [makeLine 0 15 15 15 capacitorLine; makeLine 75 15 90 15 capacitorLine]
            | Degree90 | Degree270 -> 
                let arcs = [makePartArcAttr 10 10 10 -10 -10; makePartArcAttr 10 10 10 -10 -10;makePartArcAttr 10 10 10 -10 -10]
                let startingPoint = {X=15;Y=15}

                let renderedSegmentList : ReactElement List = 
                    arcs
                    |> String.concat " "
                    |> (fun attr -> [makeAnyPath startingPoint attr {defaultPath with StrokeWidth = "2.5px"; Stroke = outlineColour}])
                renderedSegmentList
                |> List.append [makeLine 15 0 15 15 capacitorLine; makeLine 15 75 15 90 capacitorLine]
        | Opamp ->
            let plusMinus = 
                match transform.Rotation with
                | Degree0 -> [makeLine (0.1*W) (2.*H/3.) (0.2*W) (2.*H/3.) symbolLine ; makeLine (0.1*W) (H/3.) (0.2*W) (H/3.) symbolLine ; makeLine (0.15*W) (0.85*H/3.) (0.15*W) (1.15*H/3.) symbolLine ;  ] 
                | Degree90 -> [makeLine (2.*W/3.) (0.8*H)  (2.*W/3.) (0.9*H) symbolLine ; makeLine (W/3.) (0.8*H) (W/3.) (0.9*H) symbolLine ; makeLine  (0.85*W/3.) (0.85*H) (1.15*W/3.) (0.85*H) symbolLine ;  ] 
                | Degree180 -> [makeLine (0.8*W) (H/3.) (0.9*W) (H/3.) symbolLine ; makeLine (0.8*W) (2.*H/3.) (0.9*W) (2.*H/3.) symbolLine ; makeLine (0.85*W) (1.85*H/3.) (0.85*W) (2.15*H/3.) symbolLine ;  ] 
                | Degree270 -> [makeLine (W/3.) (0.1*H)  (W/3.) (0.2*H) symbolLine ; makeLine (2.*W/3.) (0.1*H) (2.*W/3.) (0.2*H) symbolLine ; makeLine  (1.85*W/3.) (0.15*H) (2.15*W/3.) (0.15*H) symbolLine ;  ] 
            
            plusMinus
            |> List.append (createBiColorPolygon points colour outlineColour opacity strokeWidth comp)

            
        | _ -> createBiColorPolygon points colour outlineColour opacity strokeWidth comp



    /// to deal with the label
    let addComponentLabel (compLabel: string) transform colour = 
        let weight = Constants.componentLabelStyle.FontWeight // bold or normal
        let style = {Constants.componentLabelStyle with FontWeight = weight}
        let box = symbol.LabelBoundingBox
        let margin = Constants.componentLabelOffsetDistance

        let pos = box.TopLeft - symbol.Pos
        
        let text = addStyledText {style with DominantBaseline="hanging"} pos compLabel
        match colour with
        | "lightgreen" ->
            let x,y = pos.X - margin*0.8, pos.Y - margin*0.8
            let w,h = box.W - margin*0.4, box.H - margin * 0.4
            let polyStyle = {defaultPolygon with Fill = "lightgreen"; StrokeWidth = "0"}
            let poly = makePolygon $"{x},{y} {x+w},{y} {x+w},{y+h} {x},{y+h}" polyStyle 
            [ poly ; text ]
        | _ ->
            [text] // add ;corners (uncommenting corners) for box corner display

    
    let labelcolour = outlineColor symbol.Appearance.Colour
    

    let componentValue =
        match comp.Type with
        |Capacitor (v,s) -> if s<>"" then (s + "F") else string v + "F"
        |Inductor (v,s) -> if s<>"" then (s + "H") else string v + "H"
        |Resistor (v,s) -> if s<>"" then (s + omegaString) else string v + omegaString
        |CurrentSource (v,s) -> if s<>"" then (s + "A") else string v + "A"
        |VoltageSource (DC v) -> string v + "V"
        |VoltageSource (Sine (a,dc,f,p))  -> "Sine("+string a+"V,"+NumberHelpers.floatValueToText f+"Hz, " + string dc + "DC)"
        |VoltageSource(Pulse (v,_,_)) -> string v + "V"
        |_ -> ""
   

    // Put everything together 
    (drawPorts comp.IOPorts showPorts symbol)
    |> List.append (drawPortsText comp.IOPorts (portNames comp.Type) symbol)
    |> List.append (addCompValue symbol componentValue "bold" "16px")
    |> List.append (addComponentLabel comp.Label transform labelcolour)
    |> List.append (createdSymbol)



let init () = 
    { 
        Symbols = Map.empty; CopiedSymbols = Map.empty
        Ports = Map.empty ; IOPortsConnected= Set.empty
        Theme = Colourful
    }, Cmd.none

//----------------------------View Function for Symbols----------------------------//
type private RenderSymbolProps =
    {
        Symbol : Symbol 
        Dispatch : Dispatch<Msg>
        key: string
        Theme: ThemeType
    }

/// View for one symbol. Using FunctionComponent.Of to improve efficiency (not printing all symbols but only those that are changing)
let private renderSymbol =
    
    FunctionComponent.Of(
        fun (props : RenderSymbolProps) ->
            let symbol = props.Symbol
            let ({X=fX; Y=fY}:XYPos) = symbol.Pos
            let appear = symbol.Appearance
            g ([ Style [ Transform(sprintf $"translate({fX}px, {fY}px)") ] ]) 
                (drawSymbol props.Symbol props.Theme)
            
        , "Symbol"
        , equalsButFunctions
        )
    
/// View function for symbol layer of SVG
let MapsIntoLists map =
    let listMoving = 
        Map.filter (fun _ sym -> not sym.Moving) map
        |>Map.toList
        |>List.map snd
    let listNotMoving =
        Map.filter (fun _ sym -> sym.Moving) map
        |>Map.toList
        |>List.map snd
    listMoving @ listNotMoving


let view (model : Model) (dispatch : Msg -> unit) =    
    /// View function for symbol layer of SVG
    let toListOfMovingAndNot map =
        let listMoving = 
            Map.filter (fun _ sym -> not sym.Moving) map
            |> Map.toList
            |> List.map snd
        let listNotMoving =
            Map.filter (fun _ sym -> sym.Moving) map
            |> Map.toList
            |> List.map snd
        listMoving @ listNotMoving

    let start = TimeHelpers.getTimeMs()
    model.Symbols
    |> toListOfMovingAndNot
    |> List.map (fun ({Id = ComponentId id} as symbol) ->
        renderSymbol
            {
                Symbol = symbol
                Dispatch = dispatch
                key = id
                Theme = model.Theme
            }
    )
    |> ofList
    |> TimeHelpers.instrumentInterval "SymbolView" start

