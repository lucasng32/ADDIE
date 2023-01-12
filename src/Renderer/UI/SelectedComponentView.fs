(*
    SelectedComponentView.fs

    View for the selected component in the right tab.
*)

module SelectedComponentView

open Fulma
open Fable.React
open Fable.React.Props

open JSHelpers
open ModelType
open CommonTypes
open PopupView
open Notifications
open Sheet.SheetInterface
open DrawModelType
open FilesIO
open CatalogueView
open FileMenuView
open NumberHelpers

module Constants =
    let labelUniqueMess = "Components must have a unique label within one sheet"


let private readOnlyFormField name body =
    Field.div [] [
        Label.label [] [ str name ]
        body
    ]



let private textValueFormField isRequired name defaultValue isBad onChange =        
    Field.div [] [
        Label.label [] [ str name ]
        Input.text [
            Input.Props [ 
                Id "labelInputElement"; 
                OnPaste preventDefault; 
                SpellCheck false; 
                Name name; 
                AutoFocus true; 
                Style [ Width "200px"]; ]
            Input.DefaultValue defaultValue
            Input.CustomClass "www"
            Input.Placeholder (if isRequired then "Name (required)" else "Name (optional)")
            Input.OnChange (getTextEventValue >> onChange)
        ]
        br []
        span [Style [FontStyle "Italic"; Color "Red"]; Hidden (isBad = None)] [str <| Option.defaultValue "" isBad]
    ]


let private textFormField isRequired name defaultValue isBad onChange onDeleteAtEnd =
    let onDelete (ev: Browser.Types.KeyboardEvent) =
        if ev.key = "Delete" then  
            let textEl: Browser.Types.HTMLInputElement = unbox (Browser.Dom.document.getElementById "labelInputElement")
            let length = textEl.value.Length
            let start = textEl.selectionStart
            if length = start then
                // Delete pressed at end of input box should go to draw block as
                // a single component DELETE action - since that was probably wanted.
                // NB it will only happen if just one component is highlighted
                onDeleteAtEnd()
            
    Field.div [] [
        Label.label [] [ str name ]
        Input.text [
            Input.Props [ 
                Id "labelInputElement"; 
                OnPaste preventDefault; 
                SpellCheck false; 
                Name name; 
                AutoFocus true; 
                Style [ Width "200px"]; 
                OnKeyDown onDelete]
            Input.DefaultValue defaultValue
            Input.CustomClass "www"
            Input.Placeholder (if isRequired then "Name (required)" else "Name (optional)")
            Input.OnChange (getTextEventValue >> onChange)
        ]
        br []
        span [Style [FontStyle "Italic"; Color "Red"]; Hidden (isBad = None)] [str <| Option.defaultValue "" isBad]
    ]

let private textFormFieldSimple name defaultValue onChange =
    Field.div [] [
        Label.label [] [ str name ]
        Input.text [
            Input.Props [ OnPaste preventDefault; SpellCheck false; Name name; AutoFocus true; Style [ Width "200px"]]
            Input.DefaultValue defaultValue
            Input.Type Input.Text
            Input.OnChange (getTextEventValue >> onChange)
        ] 
    ]


let private intFormField name (width:string) defaultValue minValue onChange =
    Field.div [] [
        Label.label [] [ str name ]
        Input.number [
            Input.Props [Style [Width width]; Min minValue]
            Input.DefaultValue <| sprintf "%d" defaultValue
            Input.OnChange (getIntEventValue >> onChange)
        ]
    ]

let private floatFormField name (width:string) defaultValue minValue onChange =
    Field.div [] [
        Label.label [] [ str name ]
        Input.number [
            Input.Props [Style [Width width]; Min minValue]
            Input.DefaultValue <| sprintf "%A" defaultValue
            Input.OnChange (getFloatEventValue >> onChange)
        ]
    ]

let private int64FormField name (width:string) defaultValue minValue onChange =
    Field.div [] [
        Label.label [] [ str name ]
        Input.number [
            Input.Props [Style [Width width]; Min minValue]
            Input.DefaultValue <| sprintf "%d" defaultValue
            Input.OnChange (getInt64EventValue >> onChange)
        ]
    ]

let private intFormFieldNoMin name defaultValue onChange =
    Field.div [] [
        Label.label [] [ str name ]
        Input.number [
            Input.Props [Style [Width "60px"]]
            Input.DefaultValue <| sprintf "%d" defaultValue
            Input.OnChange (getIntEventValue >> onChange)
        ]
    ]

let private int64FormFieldNoMin name (defaultValue:int64) (currentText:string option) onChange =
    Field.div [] [
        Label.label [] [ str name ]
        Input.text [
            Input.Props [Style [Width "180px"]]
            Input.DefaultValue <| Option.defaultValue $"{defaultValue}" currentText
            Input.OnChange (getTextEventValue >> onChange)
        ]
    ]


let private makeScaleAdjustmentField model (comp:Component) dispatch =
    let sheetDispatch sMsg = dispatch (Sheet sMsg)
    
    let textw =  
        match comp.SymbolInfo with
        |Some si ->
            match si.HScale with
            |Some no -> no
            |None -> 1.0
        |None -> 1.0
    let texth =  
        match comp.SymbolInfo with
        |Some si ->
            match si.VScale with
            |Some no -> no
            |None -> 1.0
        |None -> 1.0

    div [] [
        floatFormField "Width Scale" "60px" textw 0.0 (
            fun (newWidth) ->
                if newWidth < 0.0
                then
                    let props = errorPropsNotification "Invalid number of bits."
                    dispatch <| SetPropertiesNotification props
                else
                    model.Sheet.ChangeScale sheetDispatch (ComponentId comp.Id) newWidth Horizontal
                    dispatch ClosePropertiesNotification
        )
        floatFormField "Height Scale" "60px" texth 0.0 (
            fun (newWidth) ->
                if newWidth < 0.0
                then
                    let props = errorPropsNotification "Invalid number of bits."
                    dispatch <| SetPropertiesNotification props
                else
                    model.Sheet.ChangeScale sheetDispatch (ComponentId comp.Id) newWidth Vertical
                    dispatch ClosePropertiesNotification
        )
    ]



let mockDispatchS msgFun msg =
    match msg with
    | Sheet (SheetT.Msg.Wire (BusWireT.Msg.Symbol sMsg)) ->
        msgFun msg
    | _ -> ()



let msgToS = 
    BusWireT.Msg.Symbol >> SheetT.Msg.Wire >> Msg.Sheet
  
/// Return dialog fileds used by constant, or default values
let constantDialogWithDefault (w,cText) dialog =
    let w = Option.defaultValue w dialog.Int
    let cText = Option.defaultValue cText dialog.Text
    w, cText

///// Create react to chnage constant properties
//let makeConstantDialog (model:Model) (comp: Component) (text:string) (dispatch: Msg -> Unit): ReactElement =
//        let symbolDispatch msg = dispatch <| msgToS msg
//        let wComp, txtComp =
//            match comp.Type with | Constant1( w,_,txt) -> w,txt | _ -> failwithf "What? impossible" 
//        let w = Option.defaultValue wComp model.PopupDialogData.Int
//        let cText = Option.defaultValue txtComp model.PopupDialogData.Text
//        let reactMsg, compTOpt = CatalogueView.parseConstant 64 w cText
//        match compTOpt with
//        | None -> ()
//        | Some (Constant1(w,cVal,cText) as compT) ->
//            if compT <> comp.Type then
//                model.Sheet.ChangeWidth (Sheet >> dispatch) (ComponentId comp.Id) w
//                symbolDispatch <| SymbolT.ChangeConstant (ComponentId comp.Id, cVal, cText)
//                dispatch (ReloadSelectedComponent w)
//                dispatch ClosePropertiesNotification
//        | _ -> failwithf "What? impossible"

//        div [] [
//                makeNumberOfBitsField model comp text dispatch
//                br []
//                reactMsg
//                br []
//                textFormFieldSimple 
//                    "Enter constant value in decimal, hex, or binary:" 
//                    cText 
//                    (fun txt -> 
//                        printfn $"Setting {txt}"
//                        dispatch <| SetPopupDialogText (Some txt))
                
//            ]              


let private makeValueField model (comp:Component) text dispatch =
    let sheetDispatch sMsg = dispatch (Sheet sMsg)
    
    let title, width =
        match comp.Type with
        | Resistor (v,s) -> "Resistance value", s
        | Capacitor (v,s) -> "Capacitance value", s
        | Inductor (v,s) -> "Inductance value", s
        | _ -> failwithf "makeNumberOfBitsField called with invalid component"
    textValueFormField true title (string width) None (
        fun newValue ->
            if textToFloatValue newValue = None
            then
                let props = errorPropsNotification "Invalid number value"
                dispatch <| SetPropertiesNotification props
            else
                model.Sheet.ChangeRLCValue sheetDispatch (ComponentId comp.Id) (Option.get (textToFloatValue newValue)) newValue
                //SetComponentLabelFromText model comp text' // change the JS component label
                let lastUsedWidth = model.LastUsedDialogWidth 
                dispatch (ReloadSelectedComponent (lastUsedWidth)) // reload the new component
                dispatch <| SetPopupDialogText (Some newValue)
                dispatch ClosePropertiesNotification
    )


let private makeDescription (comp:Component) model dispatch =
    match comp.Type with
    | IO -> str "IO"
    | IOLabel -> div [] [
        str "Label on Wire or Bus. Labels with the same name connect wires. Each label has input on left and output on right. \
            No output connection is required from a set of labels. Since a set represents one wire of bus, exactly one input connection is required. \
            Labels can be used:"  
        br [] ;
        str "To name wires and document designs."; br []
        str "To join inputs and outputs without wires."; br []
        str "To prevent an unused output from giving an error."
        ]
    |Resistor _ |Capacitor _ |Inductor _ |CurrentSource _ |VoltageSource _|Diode |Ground |Opamp ->
        div [] [ str "PENDING"]
    | Custom custom ->
        let styledSpan styles txt = span [Style styles] [str <| txt]
        let boldSpan txt = styledSpan [FontWeight "bold"] txt
        let italicSpan txt = styledSpan [FontStyle "italic"] txt

        let toHTMLList =
            List.map (fun label -> li [] [str <| sprintf "%s" label])
        
        let symbolExplanation = ": user defined (custom) component."
            //TODO: remaining

        //let origLdc =
        //    match model.CurrentProj with
        //    |Some p -> p.LoadedComponents |> List.find (fun ldc -> ldc.Name = custom.Name)
        //    |None -> failwithf "What? current project cannot be None at this point in finding custom component description"
        let sheetDescription = 
            match custom.Description with
            |Some sheetDescription-> 
                div [] [
                    p [] [str "----------------"]
                    p [] [str sheetDescription]
                    p [] [str "----------------"]
                ]
            |None -> 
                br []
        let portOrderExplanation = $"Input or Output ports are displayed on the '{custom.Name}' symbol sorted by the \
                    vertical position on the design sheet of the Input or Output components at the time the symbol is added."
            //TODO: remaining

        div [] [
            boldSpan $"{custom.Name}"
            span [] [str <| symbolExplanation]
            sheetDescription
            br []
            p [  Style [ FontStyle "italic"; FontSize "12px"; LineHeight "1.1"]] [
                str <| portOrderExplanation]
            br []
            span [Style [FontWeight "bold"; FontSize "15px"]] [str <| "Inputs"]
            ul [] (toHTMLList custom.IOLabels)
            br []
            makeScaleAdjustmentField model comp dispatch
        ]
        

let private makeExtraInfo model (comp:Component) text dispatch : ReactElement =
    match comp.Type with
    | Resistor _ 
    | Capacitor _ 
    | Inductor _ ->
        div []
            [
                makeValueField model comp text dispatch
            ]
    | _ -> div [] []





let viewSelectedComponent (model: ModelType.Model) dispatch =

    let checkIfLabelIsUnique chars (symbols: SymbolT.Symbol list)  =
        match List.exists (fun (s:SymbolT.Symbol) -> s.Component.Label = chars) symbols with
        |true -> Error Constants.labelUniqueMess
        |false -> Ok chars

    let allowNoLabel =
        let symbols = model.Sheet.Wire.Symbol.Symbols
        match model.Sheet.SelectedComponents with
        | [cid] ->
            match Map.tryFind cid symbols with
            //| Some {Component ={Type=MergeWires | SplitWire _ | BusSelection _}} -> true
            | _ -> false
        | _ -> false

    let sheetDispatch sMsg = dispatch (Sheet sMsg)

    /// return an OK label text, or an error message
    let formatLabelText (txt: string) compId =
        let comp = SymbolUpdate.extractComponent model.Sheet.Wire.Symbol compId
        let allowedDotPos =
            match comp.Type with
            | Custom {Name = name} -> name.Length
            | _ -> -1
        txt.ToUpper()
        |> (fun chars -> 
            let symbols = model.Sheet.Wire.Symbol.Symbols |> Map.toList |> List.filter (fun (i,s) -> i <> compId) |> List.map snd
            let badChars = 
                chars 
                |> Seq.indexed
                |> Seq.filter (fun (i,ch) -> not (System.Char.IsLetterOrDigit ch) && (ch <> '.'  || i <> allowedDotPos))
                |> Seq.map snd
                |> Seq.map string |> String.concat ""
            match String.length chars with 
            | 0 when allowNoLabel -> 
                Ok ""
            | 0 -> 
                Error "Empty label is not allowed for this component"
            | _ when not (System.Char.IsLetter chars[0]) ->
                Error "Labels must start with a character"
            | _ when badChars.Contains "." && allowedDotPos > 0 ->
                Error $"Custom Component labels can only contain a '.' immediately after the name"
            | _ when badChars.Contains "."  ->
                Error $"Labels of normal components can only contain letters and digits, not '.'"
            | _ when badChars <> "" ->
                Error $"Labels can only contain letters and digits, not '{badChars}'"
            | _ -> 
                let currSymbol = model.Sheet.Wire.Symbol.Symbols[compId]
                match currSymbol.Component.Type with
                |IOLabel ->
                    let allSymbolsNotWireLabel = symbols |> List.filter(fun s -> s.Component.Type <> IOLabel)
                    checkIfLabelIsUnique chars allSymbolsNotWireLabel
                |_ ->
                    checkIfLabelIsUnique chars symbols           
            )
    match model.Sheet.SelectedComponents with
    | [ compId ] ->
        let comp = SymbolUpdate.extractComponent model.Sheet.Wire.Symbol compId
        div [Key comp.Id] [
            // let label' = extractLabelBase comp.Label
            // TODO: normalise labels so they only contain allowed chars all uppercase
            let defaultText = 
                match model.PopupDialogData.Text with
                | None -> comp.Label
                | Some text -> text
            let label' = formatLabelText defaultText compId // No formatting atm
            let labelText = match label' with Ok s -> s | Error e -> defaultText
            readOnlyFormField "Description" <| makeDescription comp model dispatch
            makeExtraInfo model comp labelText  dispatch
            let required = 
                match comp.Type with 
                //| SplitWire _ | MergeWires | BusSelection _ -> false 
                | _ -> true
            let isBad = 
                if model.PopupDialogData.BadLabel then 
                    match label' with 
                    | Ok _ -> None
                    | Error msg -> Some msg
                else    None

            //printfn $"{comp.Label}:{label'} - {isBad} - {label'}"
            textFormField 
                required 
                "Component Name" 
                defaultText 
                isBad 
                (fun text -> // onChange
                    match formatLabelText text compId with
                    | Error errorMess ->
                        dispatch <| SetPopupDialogBadLabel (true)
                        dispatch <| SetPopupDialogText (Some text)
                    | Ok label -> 
                        setComponentLabel model sheetDispatch comp label
                        dispatch <| SetPopupDialogText (Some label)
                        dispatch <| SetPopupDialogBadLabel (false)
                    dispatch (ReloadSelectedComponent model.LastUsedDialogWidth)) // reload the new component
                ( fun () -> // onDeleteAtEndOfBox
                    let sheetDispatch sMsg = dispatch (Sheet sMsg)
                    let dispatchKey = SheetT.KeyPress >> sheetDispatch
                    dispatchKey SheetT.KeyboardMsg.DEL)
        ]    
    | _ -> 
        match model.CurrentProj with
        |Some proj ->
            let sheetName = proj.OpenFileName
            let sheetLdc = proj.LoadedComponents |> List.find (fun ldc -> ldc.Name = sheetName)
            let sheetDescription = sheetLdc.Description
            match sheetDescription with
            |None ->
                div [] [
                    p [] [str "Select a component in the diagram to view or change its properties, for example number of bits." ]    
                    br []
                    Label.label [] [str "Sheet Description"]
                    Button.button
                        [ 
                            Button.Color IsSuccess
                            Button.OnClick (fun _ ->
                                createSheetDescriptionPopup model None sheetName dispatch
                            )
                        ]
                        [str "Add Description"]
                    ]
            |Some descr ->
                div [] [
                    p [] [str "Select a component in the diagram to view or change its properties, for example number of bits." ]    
                    br []
                    Label.label [] [str "Sheet Description"]
                    p [] [str descr]
                    br []
                    Button.button
                        [
                            Button.Color IsSuccess
                            Button.OnClick (fun _ ->
                                createSheetDescriptionPopup model sheetDescription sheetName dispatch
                            )
                        ]
                        [str "Edit Description"]
                    ]
        |None -> null

