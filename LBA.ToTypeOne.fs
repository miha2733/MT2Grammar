namespace MT
open MTTypes

module internal LBATypes =
    type Cent = Cent
    type Dollar = Dollar
    type TrackSymbolLBA = TrackSymbol of trackSymbol | StartSym of Cent | EndSym of Dollar
    type LBA = state Set * letterOfAlphabet Set * TrackSymbolLBA Set * deltaFunc * state * state Set

    type axiom = char
    type VarAndVal = trackSymbol * letterOfAlphabet
    type CompoundNonTerminal =
        | PtrAtLeftAllBounds of state * VarAndVal   // [q, ¢, X, a, $]
        | PtrAtSymbAllBounds of state * VarAndVal   // [¢, q, X, a, $]
        | PtrAtRightAllBounds of VarAndVal * state  // [¢, X, a, q, $]
        | PtrNoBounds of state * VarAndVal          // [q, X, a]
        | PtrAtSymbRightBound of state * VarAndVal  // [q, X, a, $]
        | PtrAtRightRightBound of VarAndVal * state // [X, a, q, $]
        | LeftBoundAndSymb of VarAndVal             // [¢, X, a]
        | VarAndVal of VarAndVal                    // [X, a]
        | RightBoundAndSymb of VarAndVal            // [X, a, $]
    type NonTerminal =
        | RawNonTerminal of axiom
        | CompoundNonTerminal of CompoundNonTerminal
    type terminal = letterOfAlphabet
    type symbol = NonTerminal of NonTerminal | Terminal of terminal
    type production = symbol list * symbol list
    type Grammar = NonTerminal Set * terminal Set * production Set * axiom

module internal GrammarOnePrimitives =
    open LBATypes

    let cent = StartSym Cent
    let dollar = EndSym Dollar

    let mkPtrAtLeftAllBounds q Xa = PtrAtLeftAllBounds(q, Xa)
    let mkPtrAtSymbAllBounds q Xa = PtrAtSymbAllBounds(q, Xa)
    let mkPtrAtRightAllBounds q Xa = PtrAtRightAllBounds(Xa, q)
    let mkPtrNoBounds q Xa = PtrNoBounds(q, Xa)
    let mkPtrAtSymbRightBound q Xa = PtrAtSymbRightBound(q, Xa)
    let mkPtrAtRightRightBound q Xa = PtrAtRightRightBound(Xa, q)
    let mkProduction x y = (x, y)


module internal LBAToGrammarOne =
    open GrammarOnePrimitives
    open HelpFunctions
    open Primitives
    open LBATypes

    let transformationT1 ((states, alphabet, tapeAlph, delta, initialState, finalStates) : LBA) : Grammar =
        let tapeAlphNoBounds =
            Seq.choose (function | StartSym _ | EndSym _ -> None | TrackSymbol t -> Some t) tapeAlph
        let allVarAndVals = Coroutine2 tupleSymbol tapeAlphNoBounds alphabet

        let axiomA = RawNonTerminal 'A'
        let axiomB = RawNonTerminal 'B'

        let nonTerminals : Set<NonTerminal> =
            let allNonTerminals =
                let allVarsAndValsInCompoundNonTerminal =
                    Seq.collect
                        (fun Xa -> [LeftBoundAndSymb(Xa); VarAndVal(Xa); RightBoundAndSymb(Xa)])
                        allVarAndVals
                    |> Set.ofSeq
                [
                    mkPtrAtLeftAllBounds
                    mkPtrAtSymbAllBounds
                    mkPtrAtRightAllBounds
                    mkPtrNoBounds
                    mkPtrAtSymbRightBound
                    mkPtrAtRightRightBound
                ]
                |> List.map (fun f -> Set.ofSeq <| Coroutine2 f states allVarAndVals)
                |> fun lst -> allVarsAndValsInCompoundNonTerminal :: lst
                |> Set.unionMany
                |> Set.map CompoundNonTerminal
            allNonTerminals
            |> Set.add axiomA
            |> Set.add axiomB

        let Step1 =
            Set.map (fun a -> mkProduction [axiomA] [PtrAtLeftAllBounds(initialState, (TLetter a, a))]) alphabet
//        let Step2dot1 =
//            let lefts = Map.filter (fun k -> function PtrAtLeftAllBounds(_, (TLetter x, y)) -> x = y | _ -> false) allNonTerminalsMap
//            Set.map (fun i -> mkProduction [RawNonTerminal 'A'] [i]) <| getNums lefts

//        let prod = List.map
//            (fun vlvr -> mkProduction [RawNonTerminal 'A'] [mkPtrAtLeftAllBounds initialState vlvr])
//            (Seq.zip alphabet alphabet)

        (nonTerminals, alphabet, set[], 'A')
