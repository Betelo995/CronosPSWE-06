# CronParser: cobertura base (HU-04, parte 1)

Fuente: `evidence/02-coverage-baseline/raw/2c424c69-ff2b-41d0-99f9-26f30e1972fb/coverage.cobertura.xml`
(coverlet.collector 10.1.0, net8.0, 1877 pruebas, 25/10/2026). Reporte: `report/Summary.md`.

| Métrica (Cronos.CronParser) | Cubiertas | Totales | % |
|---|---|---|---|
| Líneas | 379 | 381 | 99.4 % |
| Ramas | 284 | 290 | 97.9 % |

## Líneas no cubiertas

| Línea | Código | Observación |
|---|---|---|
| 464 | `}` | Llave de cierre del `if` en `ParseNumber`, después de `ThrowFormatException` (`[DoesNotReturn]`). No se puede alcanzar; es un artefacto de la instrumentación. |
| 474 | `}` | Lo mismo, en `ParseValue`. |

## Ramas parcialmente cubiertas

| Línea | Cobertura | Código | Observación |
|---|---|---|---|
| 38 | 1/2 | `fixed (char* value = expression)` | Rama `expression == null` generada por el compilador. `CronExpression.Parse` valida null antes de llamar, así que es inalcanzable desde la API pública. |
| 197 | 1/2 | `return jitterSeed == null ? CronExpression.Daily : CronExpression.DailyWithJitter(...)` (macro `@midnight`) | Falta la rama `@midnight` **con** seed. Es una brecha real pero pequeña, fuera del alcance de `H`. |
| 220 | 6/10 | `if (AcceptCharacter(ref pointer, 'V') && ... Accept(ref pointer, '_'))` (macro `@every_*`) | Faltan cortocircuitos de macros `@e...` mal escritas en posiciones intermedias. |

## ParseHash (líneas 345-363) y ParseHashStep (líneas 402-412)

Todas las líneas y ramas están cubiertas por la suite existente:

- L348 `if (rng == null) throw new MissingSeedException();`: 2/2 ramas (86 ejecuciones).
- L351 `field == CronField.DaysOfMonth ? LastCommonDayOfMonth : field.Last`: 2/2.
- L355 `if (Accept(ref pointer, '/'))`: 2/2.
- L357 `if (field.CanDefineInterval)`: 2/2.
- L405 `ParseNumber(field, ref pointer, 1, high)`: 28 ejecuciones. Como `ParseNumber` es compartido, la cobertura estructural **no** distingue si el camino de error de `H/n` (paso fuera de rango o ausente) se ejercitó alguna vez.

## Conclusión para HU-04

La cobertura estructural de `CronParser` ya está cerca del 100 %, y `ParseHash`/`ParseHashStep` están al 100 %
de líneas y ramas. **Las métricas de líneas y ramas no revelan la brecha del token `H`.** Las entradas válidas
con `H` (CronExpressionFacts.cs, líneas ~2509-2524) ya recorren esas ramas, pero ninguna prueba verifica:

1. el rechazo de sintaxis mal formada alrededor de `H` (`H-5`, `H,10`, `H#2`, `HW`, `HL`, `H/`, `1,H`, ...);
2. los límites del paso `H/n` por campo (`H/59` y `H/60`, `H/28` y `H/29`, ...), cuyo camino de error en L405 se comparte con `ParseStep`;
3. la sensibilidad a mayúsculas de `H` (L285, L301, L324 usan `Accept`, mientras que `L`/`W` usan `AcceptCharacter`).

Por eso las pruebas nuevas se diseñan por **clases de equivalencia y valores límite** sobre la gramática del
README (el oráculo), no por líneas sin cubrir. Se espera un delta de cobertura estructural cercano a cero en
`CronParser`, y ese resultado es en sí mismo un hallazgo de HU-04.
