﻿module Exercise1
  open Base
  
  (* StateMonad *)
  type State<'S, 'a> = State of ('S -> 'S * 'a)
  
  type StateMonad() =
    static member Bind sm f = State (fun s0 -> let (s1, a1) = match sm with | State h -> h s0
                                               let (s2, a2) = match f a1 with | State h1 -> h1 s1
                                               (s2, a2))
    static member Return(a) = State (fun s -> s, a)

  let (>>=) sm f = StateMonad.Bind sm f
  let Return = StateMonad.Return

  (* StateBuilder *)
  type StateBuilder() =
    member m.Delay (f)    = State (fun s -> match (f()) with | State h -> h s)
    member m.Bind (sm, f) = State (fun s0 -> let (s1, a1) = match sm with | State h -> h s0
                                             let (s2, a2) = match f a1 with | State h1 -> h1 s1
                                             (s2, a2))
    member m.Return (a)   = State (fun s -> s, a)

  let state = StateBuilder()

  let GetState = State (fun s -> s, s)
  let SetState s = State (fun _ -> s, ())

  let Eval sm s =
    match sm with
    | State f -> f s |> fst

  let Exec sm s =
    match sm with
    | State f -> f s |> snd

  (* StateBuilder Labeller *)
  let labelTreeWithStateBuilder tree initialState incrementer =
    let rec labelTree t incrementer =
      match t with
      | Leaf(c)      -> state { let! s = GetState
                                do! SetState (incrementer s)
                                return Leaf((s, c)) }

      | Branch(l, r) -> state { let! l = labelTree l incrementer
                                let! r = labelTree r incrementer
                                return Branch(l, r) }

    Exec (labelTree tree incrementer) initialState

  (* Static StateMonad Labeller *)
  let labelTreeWithStaticStateMonad tree initialState incrementer =
    let rec labelTree t incrementer =
      match t with
      | Leaf(c)      -> incrementer () >>= fun s -> Return (Leaf((s, c)))
      | Branch(l, r) -> labelTree l incrementer >>=
                          fun newLeft -> labelTree r incrementer >>=
                                           fun newRight -> Return (Branch(newLeft, newRight))
                             
    Exec (labelTree tree incrementer) initialState