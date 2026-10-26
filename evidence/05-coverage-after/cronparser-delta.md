# CronParser: cobertura antes y después del incremento (HU-04, parte 2)

- Antes: `evidence/02-coverage-baseline/raw/2c424c69-ff2b-41d0-99f9-26f30e1972fb/coverage.cobertura.xml` (1877 pruebas)
- Después: `evidence/05-coverage-after/raw/10195dfe-8009-4a6c-bf2f-1ce979a7654d/coverage.cobertura.xml` (1940 pruebas)

| Métrica (Cronos.CronParser) | Antes | Después | Delta |
|---|---|---|---|
| Líneas cubiertas | 379 / 381 (99.4 %) | 379 / 381 (99.4 %) | 0 |
| Ramas cubiertas | 284 / 290 (97.9 %) | 284 / 290 (97.9 %) | 0 |
| Ensamblado Cronos, líneas | 92.1 % | 92.1 % | 0 |
| Ensamblado Cronos, ramas | 91.2 % (653/716) | 91.2 % (653/716) | 0 |

## Líneas de `02-coverage-baseline/cronparser-uncovered.md` que ahora quedan cubiertas

**Ninguna.** Las líneas 464 y 474 (llaves de cierre después de `ThrowFormatException` con `[DoesNotReturn]`)
no se pueden alcanzar. Las ramas parciales de las líneas 38, 197 y 220 pertenecen a `fixed`, `@midnight` y
`@every_*`, que están fuera del alcance de `H`.

## Lo que sí cambió: ejecuciones en ParseHash y ParseHashStep (hits de Cobertura)

| Línea | Código | Hits antes | Hits después |
|---|---|---|---|
| 348 | `if (rng == null) throw new MissingSeedException();` | 86 | 174 |
| 355 | `if (Accept(ref pointer, '/'))` | 82 | 134 |
| 361 | `rng.Next(field.First, maxValueInclusive + 1)` (H sin paso) | 54 | 70 |
| 405 | `ParseNumber(field, ref pointer, 1, high)` en ParseHashStep | 28 | 64 |
| 410 | `rng.Next(0, step)` (después de validar el paso) | 28 | 50 |

Lectura: **antes**, L405 y L410 tenían los mismos hits (28 y 28). Ninguna prueba hacía fallar
`ParseNumber` desde `ParseHashStep`; es decir, el camino de rechazo de `H/n` nunca se ejercitaba. **Después** la
diferencia es 64 − 50 = 14 ejecuciones que terminan en `CronFormatException` dentro de `ParseHashStep`
(11 casos `H/n` fuera de rango, 1 caso `H/29` del Fact de caracterización y 2 casos `H/` sin número).
Las líneas y ramas ya estaban cubiertas, pero ahora el camino de error se recorre de forma intencional y
con una aserción.

## Conclusión

El incremento no mueve el porcentaje de cobertura estructural de `CronParser`, porque ya estaba cerca del
máximo alcanzable. Su aporte se ve en el **dominio de entrada**: 63 casos nuevos con oráculo (gramática del
README) que cubren clases inválidas y valores límite del token `H` sin ninguna aserción previa. Para HU-04
esto muestra que la cobertura de líneas y ramas sirvió para ubicar el código bajo prueba (ParseHash y
ParseHashStep), pero no basta como criterio de adecuación: hace falta complementarla con partición de
equivalencia y valores límite, o con pruebas de mutación.
