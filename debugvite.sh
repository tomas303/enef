#!/bin/bash
# based on https://github.com/fable-compiler/Fable/issues/3631  --verbose prevent freezing of fable
# other workaround was run vit separately and not via fable
dotnet fable watch src --verbose --run npx vite