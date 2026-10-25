# Evidencia del incremento TDD — Cronos (Grupo 3, PSWE-06)

## Entorno
- SDK .NET: **10.0.400** (MSBuild 18.9.6). Las pruebas y la cobertura usan el target `net8.0` sobre el runtime **Microsoft.NETCore.App 8.0.30**. El análisis estático compila `src/Cronos` para `net6.0`. Detalle en `00-environment.txt`.
  - **Desviación registrada:** el prompt pedía un SDK 8.x, pero la máquina solo tiene el SDK 10.0.400. El usuario autorizó en el chat (2026-10-25) seguir con el SDK 10 apuntando a net8.0. Los analizadores que se usaron son los del SDK 10 (`analysislevel_10_all.globalconfig`).
  - **Desviación registrada:** `dotnet tool install dotnet-reportgenerator-globaltool` falla dentro del repo con `NU3034: Este paquete está firmado, pero no por un firmante de confianza`. El `NuGet.config` del repo exige `signatureValidationMode=require` y una lista cerrada de propietarios, y el autor de ReportGenerator (danielpalme) no está en ella. La herramienta (v5.5.11, desde nuget.org) se instaló ejecutando el comando **fuera** del repo con `--tool-manifest <repo>/dotnet-tools.json`. No se modificó `NuGet.config`. Para que otro integrante pueda restaurarla tendrá que hacer lo mismo, o el equipo tendrá que decidir si agrega a danielpalme como firmante de confianza.
- Sistema operativo: Windows 11 Pro 10.0.26300 (win-x64).
- Fecha: 2026-10-25.
- Commit base del fork: `11234183dd5db3795b0b34710b75f8df120847e6` (2026-09-10, "chore: Fix typo in job-scheduling example comment (#98)"). Coincide con el esperado (`00-base-commit.txt`).
- Rama de trabajo: `feature/parser-jitter-validation` (solo local, sin upstream, sin push).

## Línea base
- Pruebas (`01-baseline-tests.txt`): total **1877** / pasaron **1877** / fallaron **0** / omitidas **0** / duración **6,7912 s**.
- Cobertura de CronParser (`02-coverage-baseline/`): líneas **99.4 %** (379/381), ramas **97.9 %** (284/290). Ensamblado Cronos: líneas 92.1 %, ramas 91.2 %.
- Advertencias de analizadores por regla (`03-analyzers-baseline.txt`, `src/Cronos` net6.0, `AnalysisMode=All`): **CA: 0, IDE: 0, CS: 0**. Hay 1 advertencia sin código de regla (compatibilidad del paquete System.IO.Hashing 10.0.12 con net6.0; no es de análisis de código). Las reglas IDE no se reportan en build porque `EnforceCodeStyleInBuild` no está activo.
- Verificación de `dotnet build Cronos.sln -f net6.0` con el `.editorconfig` (`03-solution-build-check.txt`): falla con **NETSDK1005 en benchmarks/Cronos.Benchmarks**, un proyecto que solo apunta a net8.0. El fallo **ya existía** y también aparece sin `.editorconfig`. `src` y `tests` compilan sin errores. El `.editorconfig` agrega advertencias en `tests` (CA1707 ×212 por los guiones bajos en los nombres de prueba, CA1305 ×2, xUnit1042 ×4, xUnit2024 ×4), pero ningún error, así que se mantuvo la severidad `warning`.

## Ciclos TDD

### Ciclo 1 — HU-02: sintaxis mal formada alrededor de `H`
- Pruebas: `CronParserJitterFacts.Parse_ThrowsCronFormatException_WhenHashTokenHasInvalidSyntax` y `CronParserJitterFacts.Parse_ThrowsCronFormatException_WhenHashTokenIsInvalidEvenWithoutSeed`.
- Casos: `H-5`, `H,10`, `H5`, `1,H`, `H/` (Minutes); `H#2`, `HW`, `HL` (Days of month); `H#2`, `HL` (Days of week). Cada uno en Standard e IncludeSeconds, con seed 3 (20 casos) y sin seed (20 casos).
- Comando: `dotnet test tests/Cronos.Tests/Cronos.Tests.csproj -f net8.0 --filter "FullyQualifiedName~CronParserJitterFacts" --logger "console;verbosity=normal"`.
- Primera ejecución: **VERDE**, `Pruebas totales: 40 / Correcto: 40` (`04-cycle-1-result.txt`). Es **caracterización**: el parser ya rechaza todas las expresiones con `CronFormatException` y el mensaje nombra el campo. No acepta ninguna expresión prohibida y no aparece ninguna excepción inesperada.
- Sin seed (`04-cycle-1-no-seed-detail.txt`): 18 casos lanzan `MissingSeedException`; 2 casos (`1,H * * * *` y `* 1,H * * * *`) lanzan `CronFormatException: ... Minutes: Value must be a number between 0 and 59`.
- Commit de la prueba: `7e529a5286b9fa5bfd6b01be0209570cc6aaa044`.
- Cambio de producción: **no**.
- Suite completa después del ciclo: 1940/1940 (corrida de `05-coverage-after/test-run.txt`, que incluye los ciclos 1 y 2).

### Ciclo 2 — HU-01 y HU-03: límites del paso en `H/n`
- Pruebas: `Parse_AcceptsHashStep_AtDocumentedUpperBound` (11 casos), `Parse_ThrowsCronFormatException_WhenHashStepExceedsFieldRange` (11 casos) y `Parse_StepUpperBoundDiffersBetweenStarAndHash_InDayOfMonth` (Fact).
- Casos: Seconds `H/59` y `H/60`; Minutes `H/59` y `H/60`; Hours `H/23` y `H/24`; Days of month `H/28` y `H/29`; Months `H/12` y `H/13`; Days of week `H/7` y `H/8`, en ambos formatos (Seconds solo en IncludeSeconds). En los válidos se verifica que dos `Parse` con seed 3 den el mismo resultado y que `GetNextOccurrence(2026-01-01 UTC)` no sea nulo. En los inválidos se verifica que el mensaje contenga el campo y "between 1 and N".
- Comando: `dotnet test ... -f net8.0 --filter "FullyQualifiedName~CronParserJitterFacts.Parse_AcceptsHashStep_AtDocumentedUpperBound|...WhenHashStepExceedsFieldRange|...StepUpperBoundDiffersBetweenStarAndHash_InDayOfMonth" --logger "console;verbosity=normal"`.
- Primera ejecución: **VERDE**, `Pruebas totales: 23 / Correcto: 23` (`04-cycle-2-result.txt`). Es **caracterización**.
- Commit de la prueba: `ad1c0c0d07be1ed6facfbc5ba4e2da03cfad4742`.
- Cambio de producción: **no**. El tope de 28 es deliberado (comentario "Prevent against calculating the 31st of February").
- Suite completa: 1940/1940 pasaron, 0 fallaron (`05-coverage-after/test-run.txt`).

### Ciclo 3A — HU-02: `MissingSeedException` dentro del contrato de `CronFormatException` (rojo → verde real)
- Aprobación: el equipo lo aprobó en el chat el 2026-10-25.
- Prueba: `CronParserJitterFacts.Parse_ThrowsCronFormatExceptionFamily_WhenHashIsPresentAndNoSeedIsProvided` (6 casos: `H` en cada campo en Standard, y `H * * * * *` en IncludeSeconds). Usa `Assert.ThrowsAny<CronFormatException>` y además `Assert.IsType<MissingSeedException>`, porque el tipo concreto que se lanza no debe cambiar.
- Comando: `dotnet test tests/Cronos.Tests/Cronos.Tests.csproj -f net8.0 --filter "FullyQualifiedName~Parse_ThrowsCronFormatExceptionFamily_WhenHashIsPresentAndNoSeedIsProvided" --logger "console;verbosity=normal"`.
- Primera ejecución: **ROJO** (`07-cycle-3a-red.txt`): `Assert.ThrowsAny() Failure: Exception type was not compatible / Expected: typeof(Cronos.CronFormatException) / Actual: typeof(Cronos.MissingSeedException)`, con `Pruebas totales: 6 / Incorrecto: 6`.
- Commit de la prueba: `1279165f880378bc92857497264bea7aebb70f77`.
- **Intento 1 de implementación: FALLIDO, revertido.** El cambio fue `MissingSeedException : CronFormatException` más la eliminación de los dos `catch (MissingSeedException)` de `TryParse` (CS0160). El build falló en los 5 targets con `error CS0108: 'MissingSeedException.BaseMessage' oculta el miembro heredado 'CronFormatException.BaseMessage'`. CS0108 es una advertencia, pero `src/Directory.Build.props` tiene `TreatWarningsAsErrors=true`. Se revirtió con `git checkout -- src/` y se informó al equipo. Las salidas literales están en `07-cycle-3a-attempt1-green.txt`, `07-cycle-3a-attempt1-full-suite.txt` y `07-cycle-3a-attempt1-build-all-targets.txt`, cada una con una línea inicial que explica el fallo.
- **Corrección aprobada (opción B):** renombrar la constante interna `MissingSeedException.BaseMessage` a `MissingSeedMessage`, con el mismo texto. Se descartó usar `new` porque ocultar un miembro heredado es justo lo que un analizador marca después.
- Cambio de producción (sí): `src/Cronos/MissingSeedException.cs` (herencia y rename de la constante; 3 inserciones y 3 borrados) y `src/Cronos/CronExpression.cs` (se eliminan los dos `catch (MissingSeedException)`; 10 borrados).
- Verde después del cambio: build de los 5 targets (net40, net45, netstandard1.0, netstandard2.0, net6.0) con `1 Advertencia(s) / 0 Errores` (System.IO.Hashing; `07-cycle-3a-build-all-targets.txt`). La prueba de 3A.1 dio `Pruebas totales: 6 / Correcto: 6` (`07-cycle-3a-green.txt`).
- Suite completa: **1946/1946** (`07-cycle-3a-full-suite.txt`). Siguen pasando `Parse_ThrowsAnException_WhenHashIsPresentAndNoRandomIsProvided` (usa `Assert.Throws<MissingSeedException>`, tipo exacto) y `TryParse_ReturnsFalse_WhenHashIsPresentAndNoRandomIsProvided` (ahora lo atrapa el `catch (CronFormatException)`).
- Commit de la implementación (incluye las salidas del intento fallido): `b3d4e786ee8cbfee82b110ff2c5a1233ebc1b76e`.
- Lectura TDD: rojo → implementación fallida por CS0108 → corrección → verde. La red de pruebas y el build estricto detectaron el problema antes de integrar.

### Ciclo 3B — HU-03: `h` minúscula aceptada como `H`, igual que `L` y `W` (rojo → verde real)
- Aprobación: el equipo lo aprobó en el chat el 2026-10-25. La prueba preparada en `pending/` se incorporó al archivo de pruebas y la carpeta se borró.
- Prueba: `CronParserJitterFacts.Parse_AcceptsLowercaseHash_LikeUppercase` (8 casos: `h * * * *`, `* * h * *`, `* * * * h`, `h/15 * * * *` y sus variantes IncludeSeconds). Verifica `Assert.Equal(Parse(upper, format, 3), Parse(lower, format, 3))`.
- Comando: `dotnet test ... -f net8.0 --filter "FullyQualifiedName~Parse_AcceptsLowercaseHash_LikeUppercase" --logger "console;verbosity=normal"`.
- Primera ejecución: **ROJO** (`08-cycle-3b-red.txt`), `Pruebas totales: 8 / Incorrecto: 8`, todos con `Cronos.CronFormatException`: "Minutes: Value must be a number between 0 and 59" (×4), "Days of month: Value must be a number between 1 and 31" (×2) y "Days of week: Value must be a number between 0 and 7" (×2).
- Commit de la prueba: `8ba0cb4d6a8dba1f9cedd00ab34683202d802519`.
- Cambio de producción (sí): en `src/Cronos/CronParser.cs`, las 3 ocurrencias de `Accept(ref pointer, 'H')` en `ParseField`, `ParseDayOfMonth` y `ParseDayOfWeek` pasan a `AcceptCharacter(ref pointer, 'H')` (3 inserciones y 3 borrados).
- Verde: `Pruebas totales: 8 / Correcto: 8` (`08-cycle-3b-green.txt`).
- Suite completa: **1954/1954** (`08-cycle-3b-full-suite.txt`). Se comprobó que los 330 casos de `Parse_ThrowsCronFormatException_WhenCronExpressionIsInvalid` dan el mismo resultado antes (07) y después (08): 330/330 correctas. Los únicos casos con `h` minúscula son macros (`@hour`), que se procesan en `ParseMacro`.
- Commit de la implementación: `2f68fa2a63476ac5a29271262f86b2d77869ef43`.
- Documentación: en la sección "Jitter" del README se agregó que `H` no distingue mayúsculas y que `MissingSeedException` deriva de `CronFormatException`. Commit `87723d0f3e4d1374292f4e2946a2ab244d4554e7`.

## Estado después del incremento
- Pruebas: **1877 → 1940 (ciclos 1-2) → 1954 (final)**. Al final pasan 1954 y fallan 0 (`09-coverage-final/test-run.txt`). Las 77 pruebas nuevas están en `CronParserJitterFacts`.
- Cobertura de CronParser (`09-coverage-final/cronparser-delta.md`): líneas **99.4 %** (379/381) y ramas **97.9 %** (284/290) en los tres momentos; delta 0. En el ensamblado, las líneas suben de 92.1 % a 92.4 % porque 3A **eliminó** 8 líneas coberturables de `CronExpression.TryParse`. Uno de los dos `catch` borrados era código muerto. Las ramas del ensamblado quedan en 653/716.
- Advertencias de analizadores en producción: CA 0 / IDE 0 / CS 0 en la línea base (03), después de los ciclos 1-2 (06) y al final (10). **Delta 0.** En la corrida final se recompiló el código modificado (`10-analyzers-final.txt`).
- En `tests` (fuera del alcance del script), el archivo nuevo sigue la convención de nombres con guiones bajos y genera CA1707, igual que `CronExpressionFacts.cs`.

## Hallazgos para el informe
- **`MissingSeedException` no heredaba de `CronFormatException`: CORREGIDO en el ciclo 3A.** Un consumidor que capturara solo `CronFormatException` dejaba escapar el caso `H` sin seed. Ahora la jerarquía es `MissingSeedException : CronFormatException : FormatException`. El tipo lanzado no cambió y el mensaje tampoco. Se mantiene un matiz: con `H` al inicio de un campo, la falta de seed se comprueba (CronParser.cs L348) **antes** de validar la sintaxis que sigue. `H-5 * * * *` sin seed informa la falta de seed y no el rango inválido, pero ahora ambos casos son `CronFormatException`.
- **Impacto en la API pública de 3A:** es un cambio de jerarquía de una clase pública. Es compatible para quien capture `CronFormatException`, `FormatException` o `MissingSeedException`. Si algún consumidor tenía `catch (CronFormatException)` seguido de `catch (MissingSeedException)`, ahora obtendrá CS0160 al recompilar, igual que pasó en el propio `TryParse`. Conviene mencionarlo en las notas de versión.
- **Tope de 28 en `H/n` para el día del mes, contra 1-31 documentado:** sigue como caracterización (`Parse_StepUpperBoundDiffersBetweenStarAndHash_InDayOfMonth`). No se cambió ni se documentó en el README; queda pendiente de decisión.
- **Mayúsculas en `H`: CORREGIDO en el ciclo 3B.** Antes, `h` producía `CronFormatException` mientras que `l`, `w` y `lw` se aceptaban. Ahora `h` es equivalente a `H` en todos los campos y con paso (`h/15`).
- **El intento 1 de 3A falló por CS0108:** las dos excepciones declaraban `internal const string BaseMessage`. Lo detectó el build estricto (`TreatWarningsAsErrors`) en los 5 targets, se revirtió y se corrigió con un rename. Es evidencia de refactorización bajo pruebas.
- **La cobertura estructural no detectó ninguno de los dos defectos corregidos.** `ParseHash` y `ParseHashStep` estaban al 100 % de líneas y ramas, y la cobertura de `CronParser` no se movió en todo el incremento. Para HU-04, la cobertura de líneas y ramas no basta como criterio de adecuación.
- Otros comportamientos observados:
  - Rama no cubierta: `@midnight` **con** seed (CronParser.cs L197).
  - El `catch (MissingSeedException)` del `TryParse` con seed era código muerto (eliminado en 3A).
  - `dotnet build Cronos.sln -f net6.0` falla por NETSDK1005 en `benchmarks/` (falla previa).
  - `tools/` está en `.gitignore`, así que el script se agregó con `git add -f`.
  - ReportGenerator requiere instalarse desde fuera del repo por la política de firmas de `NuGet.config` (ver Entorno).

## Criterios de aceptación: cumplidos y pendientes

| HU | Criterio | Estado | Evidencia |
|---|---|---|---|
| HU-01 | Valores de paso fuera del rango del campo en `H/n` se rechazan con `CronFormatException` y un mensaje con el campo y el rango | Cumplido (caracterización) | `04-cycle-2-result.txt`, commit `ad1c0c0` |
| HU-01 | Los valores en el límite superior (`H/59`, `H/23`, `H/28`, `H/12`, `H/7`) se aceptan de forma determinista | Cumplido (caracterización) | `04-cycle-2-result.txt` |
| HU-01 | El contrato documentado (1-31) coincide con el real para `H/n` en el día del mes | Pendiente (brecha documentada; requiere decisión: corregir el README o el código) | `Parse_StepUpperBoundDiffersBetweenStarAndHash_InDayOfMonth` |
| HU-02 | Las expresiones con `H` mal formada (`H-5`, `H,10`, `H5`, `1,H`, `H/`, `H#2`, `HW`, `HL`) se rechazan con `CronFormatException`, con el campo en el mensaje | Cumplido (caracterización) | `04-cycle-1-result.txt`, commit `7e529a5` |
| HU-02 | Cada expresión inválida provoca el tipo de error del contrato público (`CronFormatException`), incluido `H` sin seed | **Cumplido (ciclo 3A, rojo → verde)** | `07-cycle-3a-red.txt`, `07-cycle-3a-green.txt`, `07-cycle-3a-full-suite.txt`, commits `1279165` y `b3d4e78` |
| HU-03 | Los límites de pasos y su combinación con campos se validan por campo | Cumplido (caracterización) | `04-cycle-2-result.txt` |
| HU-03 | Validación consistente de mayúsculas y minúsculas para `H`, igual que para `L` y `W` | **Cumplido (ciclo 3B, rojo → verde)** | `08-cycle-3b-red.txt`, `08-cycle-3b-green.txt`, `08-cycle-3b-full-suite.txt`, commits `8ba0cb4` y `2f68fa2` |
| HU-04 | Medición reproducible de cobertura de líneas y ramas (coverlet + ReportGenerator, manifiesto local) | Cumplido (ReportGenerator requiere el procedimiento de instalación descrito en Entorno) | `02-coverage-baseline/`, `05-coverage-after/`, `09-coverage-final/`, commit `23b6732` |
| HU-04 | La cobertura guía la ubicación de las pruebas y se compara entre momentos | Cumplido (base, ciclos 1-2 y final) | `02-.../cronparser-uncovered.md`, `05-.../cronparser-delta.md`, `09-.../cronparser-delta.md` |
| Transversal | Análisis estático configurado en el repo y ejecutable con un script, sin advertencias nuevas | Cumplido (delta 0 en los tres momentos) | `.editorconfig`, `tools/static-analysis.ps1`, `03-*`, `06-*`, `10-analyzers-final.txt`, commit `39912c7` |
