# Homologação obrigatória — Civil 3D 2020 e versões posteriores

Executar numa cópia de um DWG de teste. Registrar versão completa/build do Civil 3D, idioma, Windows, unidade efetiva, UCS, configuração regional e SHA-256 do pacote utilizado. Repetir nas versões que a Panatta decidir suportar; não extrapolar um resultado para todas as versões.

## Casos e resultados independentes

1. Retângulo 10 × 20 m: área 200 m²; perímetro 60 m; quatro trechos; fechamento preservado.
2. Semicírculo de raio 10 m fechado por diâmetro: área 50π m²; perímetro 20 + 10π m. Conferir corda 20 m, desenvolvimento 10π m e ângulo central 180°.
3. Arco de 270° de raio 10 m fechado por corda: área `50*(3π/2 + 1) m²`; perímetro `15π + √200 m`.
4. Executar novamente cada figura invertendo o sentido e variando o vértice inicial. Conferir área/perímetro invariantes, sentido de curva invertido, todas as divisas presentes e orientação exterior fisicamente igual.
5. Usar UCS rotacionado e transladado. Coordenadas de saída devem continuar em WCS. Repetir com coordenadas UTM, sem atribuir CRS automaticamente.
6. Normal OCS -Z, elevação constante diferente de zero, milímetros e pés: verificar transformação, sinal do bulge, escala e elevação. A orientação plana independe do sinal da normal original.
7. Polilinha aberta, bow-tie, vértice repetido, largura não zero, plano inclinado e entidade não suportada: rejeição clara, sem reparo automático.
8. Confirmar comparação de área/perímetro com `LIST`/Propriedades do CAD. Metas iniciais dos casos sintéticos: `1e-6 m` e `1e-4 m²`. Investigar diferenças, sem aumentar tolerância para encobri-las.
9. Cancelar seleção, unidade e arquivo. Testar pasta sem permissão, nome existente, caminho com espaços/acentos, arquivo de saída aberto e política bloqueando WScript.Shell. Nenhum arquivo do usuário deve ser sobrescrito.
10. Comparar geometria, bulges, layer e handle antes/depois. Confirmar que seleção/cálculo não executou edição. O próprio CAD pode alterar seu estado de sessão por razões independentes; verificar entidades, não apenas a indicação de modificação na janela.

## Registro por execução

| Item | Resultado a preencher |
|---|---|
| Versão/build Civil 3D e Windows | Pendente |
| Identidade e hash do pacote | Pendente |
| DWG e geometria de referência | Pendente |
| Área/perímetro no CAD e no relatório | Pendente |
| Coordenadas/curvas e fechamento | Pendente |
| UCS/OCS/unidades e sentido | Pendente |
| Integridade das entidades após leitura | Pendente |
| Cancelamentos e arquivos | Pendente |
| Responsável pela conferência e data | Pendente |

Até preencher esse registro com execução real, o pacote é um protótipo compilado para teste, sem aceite de integração Autodesk.
