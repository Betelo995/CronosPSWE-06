# CronParser: cobertura en los tres momentos (HU-04)

| Momento | Pruebas | Fuente Cobertura | Líneas CronParser | Ramas CronParser |
|---|---|---|---|---|
| Base (commit 1123418 + coverlet) | 1877 | `02-coverage-baseline/raw/2c424c69-.../coverage.cobertura.xml` | 379/381 (99.4 %) | 284/290 (97.9 %) |
| Después de los ciclos 1-2 (caracterización) | 1940 | `05-coverage-after/raw/10195dfe-.../coverage.cobertura.xml` | 379/381 (99.4 %) | 284/290 (97.9 %) |
| Final (después de 3A y 3B) | 1954 | `09-coverage-final/raw/3bdcb5f9-.../coverage.cobertura.xml` | 379/381 (99.4 %) | 284/290 (97.9 %) |

Las líneas y ramas sin cubrir son las mismas en los tres momentos: L464 y L474 (llaves inalcanzables después de un
`[DoesNotReturn]`) y las ramas parciales de L38 (`fixed`), L197 (`@midnight` con seed) y L220 (`@every_*`).
El cambio de 3B (`Accept` → `AcceptCharacter` en L285/L301/L324) no agrega líneas ni ramas en Cobertura,
porque la llamada sigue siendo una condición de dos salidas.

## Ensamblado Cronos (efecto del ciclo 3A en CronExpression)

| Momento | Líneas cubiertas / coberturables (ensamblado) | % líneas | Ramas | CronExpression (cubiertas/no cubiertas/coberturables) |
|---|---|---|---|---|
| Base | 1142 / 1239 | 92.1 % | 653/716 (91.2 %) | 566 / 80 / 646 (87.6 %) |
| Ciclos 1-2 | 1142 / 1239 | 92.1 % | 653/716 (91.2 %) | 566 / 80 / 646 (87.6 %) |
| Final | 1138 / 1231 | 92.4 % | 653/716 (91.2 %) | 562 / 76 / 638 (88.0 %) |

Al eliminar los dos `catch (MissingSeedException)` de `TryParse` desaparecen 8 líneas coberturables: 4 cubiertas
y 4 no cubiertas. El `catch` del `TryParse(expression, format, ...)` sin seed sí se ejecutaba
(`TryParse_ReturnsFalse_WhenHashIsPresentAndNoRandomIsProvided`). El del `TryParse(..., jitterSeed, ...)`
era **código muerto**: con seed nunca se lanza `MissingSeedException`. El aumento del ensamblado
(92.1 % → 92.4 %) viene de borrar código, no de probar más, y así se reporta.

## Hits en ParseHash y ParseHashStep (Cobertura)

| Línea | Base | Ciclos 1-2 | Final |
|---|---|---|---|
| L348 `if (rng == null) throw new MissingSeedException();` | 86 | 174 | 202 |
| L405 `ParseNumber(...)` en ParseHashStep | 28 | 64 | 68 |
| L410 `rng.Next(0, step)` | 28 | 50 | 54 |

## Conclusión

La cobertura estructural de `CronParser` no se movió en todo el incremento, aunque se agregaron 77 pruebas,
2 de los ciclos corrigieron defectos reales de contrato (3A) y de consistencia (3B), y se cubrieron clases
inválidas y valores límite de `H`. Para HU-04, la medición sirvió para ubicar las pruebas (ParseHash y
ParseHashStep), pero no detecta los defectos que encontraron los ciclos 3A y 3B. Ninguno de los dos es una
línea sin cubrir: uno es un error de tipo en la jerarquía de excepciones y el otro es una comparación que
distingue mayúsculas.
