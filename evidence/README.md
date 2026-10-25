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

### Ciclo 3 — HU-03: mayúsculas y minúsculas en el token `H`
- **NO EJECUTADO: pendiente de aprobación del equipo.** El prompt condiciona este ciclo a una confirmación explícita en el chat, y no la hubo.
- La prueba `Parse_AcceptsLowercaseHash_LikeUppercase` (6 casos: `h` frente a `H` en minutos, día del mes y día de la semana, en ambos formatos) quedó preparada en `pending/cycle-3-lowercase-hash.cs.txt`. Ese archivo **no está commiteado ni se compila**.
- Por lo tanto no hay rojo registrado, ni commit de prueba, ni commit de producción para este ciclo.

## Estado después del incremento
- Pruebas: total **1940** / pasaron **1940** / fallaron **0** (+63 pruebas nuevas, todas de `CronParserJitterFacts`).
- Cobertura de CronParser: líneas **99.4 %** (379/381, delta 0), ramas **97.9 %** (284/290, delta 0). Detalle en `05-coverage-after/cronparser-delta.md`. El cambio observable está en los hits de `ParseHashStep`: L405 y L410 pasaron de 28/28 a 64/50, es decir, 14 ejecuciones nuevas del camino de rechazo de `H/n`.
- Advertencias de analizadores (`06-analyzers-after.txt`): **CA: 0, IDE: 0, CS: 0. Delta: 0.** Era lo esperado, porque no hubo cambios de producción. El conteo del script dice "2 occurrences" sin código de regla, pero es la misma advertencia de System.IO.Hashing de la línea base, que MSBuild imprime en línea y otra vez en el resumen.
- En `tests` (fuera del alcance del script), el archivo nuevo genera 5 apariciones de CA1707 por los guiones bajos en los nombres. Es la misma convención que usa `CronExpressionFacts.cs`.

## Hallazgos para el informe
- **`MissingSeedException` no hereda de `CronFormatException`** (hereda de `System.Exception`). Un consumidor que solo capture `CronFormatException` no la atrapa, y su proceso puede terminar con una excepción no controlada. Además, cuando `H` está al inicio de un campo, la falta de seed se comprueba (CronParser.cs L348) **antes** de validar la sintaxis que sigue: `H-5 * * * *` sin seed informa "requires providing a jitter seed" y no el rango inválido. En cambio `1,H` sin seed lanza `CronFormatException`. El tipo de excepción depende de la posición de `H` (18 de 20 casos frente a 2 de 20).
- **Tope de 28 en `H/n` para el día del mes, contra 1-31 documentado.** `* * */29 * *` se acepta y `* * H/29 * *` lanza "between 1 and 28". La decisión es deliberada, pero ni el README ni la tabla de caracteres especiales la documentan. Ahora queda fijada como caracterización en `Parse_StepUpperBoundDiffersBetweenStarAndHash_InDayOfMonth`.
- **Mayúsculas y minúsculas en `H`:** en el código (L285, L301, L324) `H` se acepta con `Accept`, que distingue mayúsculas, mientras que `L` y `W` usan `AcceptCharacter`, que no las distingue. Estado después del incremento: **sin cambios**. El ciclo 3 sigue pendiente de aprobación y no se ejecutó la prueba que demostraría el rojo.
- **La cobertura estructural no revela la brecha de `H`.** `ParseHash` y `ParseHashStep` ya estaban al 100 % de líneas y ramas antes del incremento, aunque ninguna prueba ejercitaba el rechazo de `H/n` ni la sintaxis inválida alrededor de `H`. Las 63 pruebas nuevas no cambian el porcentaje. Para HU-04, la cobertura de líneas y ramas no basta como criterio de adecuación.
- Otros comportamientos observados:
  - Rama no cubierta en la línea base: `@midnight` **con** seed (CronParser.cs L197, 1/2 ramas). Es una brecha pequeña fuera del alcance de `H`.
  - `dotnet build Cronos.sln -f net6.0` falla por NETSDK1005 en `benchmarks/` (falla previa, no relacionada).
  - `tools/` está en `.gitignore` (`[Tt]ools/`). `tools/static-analysis.ps1` se agregó con `git add -f` sin modificar `.gitignore`.

## Criterios de aceptación: cumplidos y pendientes

| HU | Criterio | Estado | Evidencia |
|---|---|---|---|
| HU-01 | Valores de paso fuera del rango del campo en `H/n` se rechazan con `CronFormatException` y un mensaje con el campo y el rango | Cumplido (caracterización) | `04-cycle-2-result.txt`, commit `ad1c0c0` |
| HU-01 | Los valores en el límite superior (`H/59`, `H/23`, `H/28`, `H/12`, `H/7`) se aceptan de forma determinista | Cumplido (caracterización) | `04-cycle-2-result.txt` |
| HU-01 | El contrato documentado (1-31) coincide con el real para `H/n` en el día del mes | Pendiente (brecha documentada; requiere decisión: corregir el README o el código) | `Parse_StepUpperBoundDiffersBetweenStarAndHash_InDayOfMonth` |
| HU-02 | Las expresiones con `H` mal formada (`H-5`, `H,10`, `H5`, `1,H`, `H/`, `H#2`, `HW`, `HL`) se rechazan con `CronFormatException`, con el campo en el mensaje | Cumplido (caracterización) | `04-cycle-1-result.txt`, commit `7e529a5` |
| HU-02 | Rechazo controlado sin seed: siempre una excepción de formato capturable | Parcial (18/20 lanzan `MissingSeedException`, que no es `CronFormatException`) | `04-cycle-1-no-seed-detail.txt` |
| HU-03 | Los límites de pasos y su combinación con campos se validan por campo | Cumplido (caracterización) | `04-cycle-2-result.txt` |
| HU-03 | Validación consistente de mayúsculas y minúsculas para `H`, igual que para `L` y `W` | Pendiente (ciclo 3 sin aprobación) | `pending/cycle-3-lowercase-hash.cs.txt` (no commiteado) |
| HU-04 | Medición reproducible de cobertura de líneas y ramas (coverlet + ReportGenerator, manifiesto local) | Cumplido (ReportGenerator requiere el procedimiento de instalación descrito en Entorno) | `02-coverage-baseline/`, `05-coverage-after/`, commit `23b6732` |
| HU-04 | La cobertura guía la ubicación de las pruebas y se compara antes y después | Cumplido | `02-coverage-baseline/cronparser-uncovered.md`, `05-coverage-after/cronparser-delta.md` |
| Transversal | Análisis estático configurado en el repo y ejecutable con un script, sin advertencias nuevas | Cumplido (delta 0) | `.editorconfig`, `tools/static-analysis.ps1`, `03-*`, `06-analyzers-after.txt`, commit `39912c7` |
