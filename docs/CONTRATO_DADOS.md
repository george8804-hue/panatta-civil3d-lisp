# Contrato geométrico 1.0

JSON UTF-8, propriedades camelCase, números JSON finitos. Propriedades desconhecidas são recusadas para detectar erros de digitação. Entrada máxima: 10 MB.

Campos de `BoundaryInput`:

| Campo | Regra |
|---|---|
| `schemaVersion` | `1.0` (padrão) |
| `source`, `drawing`, `handle` | Proveniência; texto fornecido pelo adaptador, não comprovação registral |
| `coordinateReference` | Texto opcional; não implica verificação do CRS |
| `units` | Obrigatório e confirmado: `m`, `mm`, `cm`, `ft`, `in`, `us-ft` |
| `closed` | Deve ser `true`; ausência equivale a aberto |
| `vertices` | Lista ordenada de `{e,n,z,bulge}` em WCS; `z` e `bulge` padrão zero |
| `startVertex` | Índice original de início, base 1, padrão 1 |
| `reverse` | Inverter a ordem de caminhamento, padrão false |
| `nativeArea` | Área nativa opcional em unidade do desenho ao quadrado |
| `nativePerimeter` | Perímetro nativo opcional em unidade do desenho |
| `linearToleranceM` | Padrão `1e-6`, finito e positivo |
| `areaToleranceM2` | Padrão `1e-4`, finito e positivo |

O vértice final NÃO repete o inicial. O bulge de cada vértice descreve o segmento até o próximo, incluindo o último até o primeiro. Com a normal do plano normalizada para +Z, bulge positivo descreve arco anti-horário. O adaptador LISP transforma os pontos OCS para WCS e inverte o sinal de bulge para normal -Z. Planos inclinados são recusados antes da exportação.

Todos os resultados geométricos são em metros e metros quadrados; a escala também se aplica às coordenadas e à elevação. Não há reprojeção. A origem é transladada durante o cálculo de área para reduzir cancelamento numérico em coordenadas grandes; os valores de saída retornam à referência original.

Retas: distância plana e azimute `atan2(ΔE, ΔN)` em [0,360). Curvas: ângulo assinado `4 atan(bulge)`, raio `corda*(|b|+1/|b|)/4`, centro analítico, desenvolvimento `R*|ângulo|`. Área total usa cordas mais segmentos circulares assinados. Arcos numericamente extremos (incluindo raio acima de `1e12 m`) são recusados; não são aproximados por retas.

`chordAzimuthDegrees` é azimute da corda, inclusive em curvas; não é azimute da tangente. `centralAngleDegrees` é magnitude positiva; `curveDirection` registra o sentido. A orientação exterior é pela normal ao trecho: para arcos, normal no ponto médio e aviso de revisão. Setores de 45°, deslocados em 22,5°: norte, nordeste, leste, sudeste, sul, sudoeste, oeste, noroeste. A referência é o eixo +N do WCS, sem afirmar norte verdadeiro/magnético.

O resultado registra os índices originais dos vértices inicial/final de cada trecho. Inversão/troca de início não altera a entidade nem sua lista original. Não são eliminados vértices ou arcos para fazer passar uma validação.

Os campos `e` e `n` de cada vértice devem ser fornecidos. Como proteção contra omissões, o desserializador os exige explicitamente. Dados calculados nunca substituem o texto da matrícula; esta fase não importa nem emite cadastro registral.
