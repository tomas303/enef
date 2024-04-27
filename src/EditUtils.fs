[<AutoOpen>]
module EditUtils

open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
type Field = {
  Input: string
  Valid: bool
}


let editIsValid (regex: string) (input: string) =
  Regex.IsMatch(input, regex, RegexOptions.Singleline)

