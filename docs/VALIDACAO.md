# Validação da Fase 1

Data: 1 de novembro de 2026. Ambiente: Linux x64, SDK .NET 8.0.427. O SDK foi obtido do endereço oficial Microsoft com TLS e SHA-512 conferido com os metadados oficiais antes da extração.

## Executado

- Compilação Release do núcleo e CLI: zero erros e zero avisos.
- Runner de aceitação: **36 testes passaram, zero falharam, zero ignorados**. Inclui retângulo, semicírculo, círculo, arco maior que 180°, bulge negativo, contorno côncavo, inversão, ponto inicial, coordenadas UTM, escalas/unidades, elevação, pequeno ângulo, comparação CAD simulada e rejeições. Dois testes exercitam adicionalmente 100 retângulos e 60 círculos segmentados determinísticos.
- CLI: **13 cenários passaram**, exercitando arquivos reais, estados/códigos de saída, relatórios JSON/CSV/TXT, SHA-256, precisão de apresentação, entradas inválidas, nomes com acento e preservação de saídas existentes.
- LISP: estrutura de parênteses/strings verificada estaticamente; ausência de operações explícitas `entmod` e `setvar` conferida. Isso não executa AutoLISP nem prova compatibilidade com Autodesk.
- Publicação cruzada win-x64 com runtime incluído: concluída. DLLs Autodesk não são necessárias para esse processo externo.

## Ainda não executado

- AutoLISP no Civil 3D 2020 ou em outra versão.
- Transformação OCS/WCS, seleção, propriedades nativas e inicialização do processo externo dentro do CAD.
- Binário Windows no Windows; script PowerShell de empacotamento e workflow GitHub.
- DWGs reais da Panatta, Parcels, confrontantes, DOCX ou qualquer memorial definitivo.

Os exemplos são sintéticos. Os valores do PDF da Chácara nº 10, que apresentam divergências entre coordenadas e descrição, não foram tratados como resultados geométricos de referência.

## Reproduzir

Na raiz do repositório, com SDK e Python disponíveis: `python3 scripts/verify.py`. No Windows: `python scripts/verify.py`. O runner preserva os códigos de saída e exige que os testes de fato terminem; um processo apenas iniciado não conta como sucesso.
