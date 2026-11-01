# PANATTA MEMORIAL

Protótipo da **Fase 1**, autorizado pela Panatta Engenharia. Versão mínima pretendida do CAD: **Civil 3D 2020**. O comando e o pacote Windows ainda precisam de homologação na instalação real; não se declara compatibilidade já testada com todas as versões posteriores.

O protótipo lê uma LWPOLYLINE fechada e calcula coordenadas, azimutes, distâncias, área, perímetro e elementos de arcos circulares. Produz **JSON de auditoria, CSV de segmentos e relatório de conferência em texto**. Não gera memorial definitivo nem DOCX nesta fase. Parcels e confrontantes automáticos são etapas seguintes.

## Instalar e experimentar no Civil 3D

1. Extraia **todo** o pacote Windows em uma pasta gravável, por exemplo `C:/PanattaMemorial`. Mantenha `PANATTAMEMORIAL.lsp` e a subpasta `engine` na mesma pasta. Não mova apenas o `.exe`: suas DLLs e arquivos de runtime são necessários.
2. Use Windows x64 compatível com .NET 8, preferencialmente Windows 10/11. O runtime está incluído no pacote, não sendo necessário instalar Python, Word ou o SDK .NET na estação de uso. Windows antigos, mesmo que executem Civil 3D 2020, não são alvo deste pacote.
3. Na linha de comando do CAD, informe a pasta real:

   ```lisp
   (setq *panatta-home* "C:/PanattaMemorial")
   ```

4. Carregue `PANATTAMEMORIAL.lsp` com `APPLOAD`. Configure a pasta como confiável pela política normal da estação. Não desative `SECURELOAD` ou outras proteções do CAD. O pacote do protótipo não possui certificado de assinatura de código.
5. Execute **PANATTAMEMORIAL**. Escolha A ou B; nesta fase os dois caminhos usam a mesma conferência individual e informam que a composição documental ainda não está disponível.
6. Selecione uma LWPOLYLINE fechada com largura zero, paralela ao plano XY do WCS. Confira a unidade efetiva (metros, milímetros, centímetros, pés internacionais, polegadas ou pés US). `INSUNITS` é exibido, mas a unidade não é presumida.
7. Confira a lista de vértices originais em WCS. Informe o índice inicial e se deseja inverter o sentido. Esses parâmetros afetam somente os dados de saída.
8. Escolha um **nome novo** para o JSON em uma pasta gravável. O programa preserva entradas e relatórios existentes. O núcleo é executado em seguida; confira o arquivo `.report.txt` indicado no CAD e a comparação com os valores nativos.

Nenhum comando de edição, `entmod` ou alteração de variável de sistema é executado. Para homologar, compare também o desenho antes/depois conforme [o roteiro](docs/HOMOLOGACAO.md). Uma execução sem erros do núcleo não significa memorial cadastral validado.

Se a política de segurança bloquear `WScript.Shell` ou a execução de processos pelo CAD, a entrada JSON continuará disponível. Execute o núcleo manualmente no PowerShell:

```powershell
& 'C:/PanattaMemorial/engine/Panatta.Cli.exe' --input 'C:/Trabalho/contorno.json' --output 'C:/Trabalho/conferencia-01'
```

Nesse modo, a mensagem do terminal identifica problemas de entrada/arquivo que podem não aparecer na janela do CAD. Caminhos com acentos devem ser testados na estação; a codificação legada do AutoLISP 2020 depende da configuração regional do Windows. O núcleo usa UTF-8 e o LISP grava JSON ASCII com escapes Unicode.

## O que conferir nas saídas

| Arquivo | Conteúdo |
|---|---|
| `nome.json` | Entrada lida no CAD, coordenadas WCS, bulges, unidade confirmada, área/perímetro nativos, handle e desenho |
| `nome.result.json` | Entrada preservada, hash SHA-256, versão do núcleo, parâmetros de apresentação, resultados sem arredondamento de impressão e diagnósticos |
| `nome.segments.csv` | Uma linha por segmento, incluindo o fechamento; números com ponto decimal para intercâmbio |
| `nome.report.txt` | Conferência em português, valores formatados, azimute em graus/minutos/segundos, dados de curvas e inconsistências |

O arquivo `.result.json` é gravado por último e indica que o conjunto de saídas foi concluído. Arquivos intermediários são preparados antes da publicação e removidos quando uma falha controlada impede a conclusão. Não há sobrescrita. Falhas abruptas do sistema ainda podem exigir escolher um novo prefixo.

Estados e códigos de saída:

- `0`, `geometry_checked`: verificações geométricas executadas sem erro crítico. Continua sendo conferência preliminar, sem validação de georreferenciamento ou confrontantes.
- `2`, `invalid`: diagnóstico geométrico ou divergência com CAD; relatório é emitido. Autointerseções e geometrias degeneradas não recebem uma descrição de segmentos aparentemente válida.
- `3`: entrada malformada/fora do domínio JSON, parâmetro inválido ou falha de arquivo; não há conjunto novo de relatórios concluído.

As casas decimais controlam apenas o TXT, nunca os cálculos nem os dados estruturados. Exemplo:

```powershell
& './engine/Panatta.Cli.exe' --input './examples/rectangle.json' --output './resultado-retangulo' --coordinate-decimals 4 --distance-decimals 3 --area-decimals 2 --angle-decimals 2
```

## Desenvolver e testar

Requer SDK **.NET 8.0.427** ou patch compatível conforme `global.json`, e Python 3 para o verificador integrado. Não há dependências NuGet de terceiros no núcleo, CLI ou testes. O pacote Windows utiliza os runtime packs oficiais Microsoft.

```sh
python3 scripts/verify.py
```

No Windows, `python scripts/verify.py`. Se `dotnet` não estiver no PATH, configure `PANATTA_DOTNET` com o caminho do executável. O verificador mantém caches locais em `.tools/`, compila o código real, executa o runner de testes com contagens e falha caso qualquer teste falhe. Não use `dotnet test` como evidência: este protótipo usa runner de aceitação em console, não um adaptador de testes NuGet.

Execução individual:

```sh
dotnet run --project tests/Panatta.Tests -c Release
dotnet run --project src/Panatta.Cli -c Release -- --input examples/rectangle.json --output artifacts/retangulo
```

Para produzir um novo pacote no Windows:

```powershell
powershell -File scripts/build-windows.ps1 -OutputDirectory artifacts/windows-0.1.0
```

O script recusa diretório já existente, roda os testes do núcleo, publica com runtime x64 incluído e reúne LISP, documentação, exemplos e SHA-256. Para outro build, escolha outro diretório. Para atualizar a estação, extraia em uma nova pasta e altere `*panatta-home*`; preserve dados de projetos fora do pacote. A distribuição em `.bundle` e a instalação do adaptador nativo Autodesk serão tratadas com a Fase 2.

## Arquitetura e limites

- `cad/`: interface e extração somente de leitura em AutoLISP/Visual LISP.
- `src/Panatta.Geometry/`: único núcleo C# independente do Autodesk.
- `src/Panatta.Cli/`: execução externa, dados estruturados e relatórios.
- `tests/`: casos analíticos e de rejeição, incluindo curvas, invariantes e coordenadas UTM.
- `examples/`: dados sintéticos, sem identificação cadastral real.
- `docs/`: contrato de dados, decisões e roteiro de homologação.

Na Fase 1 o adaptador de polilinhas é AutoLISP, em vez de uma DLL dependente do SDK Autodesk. O cálculo continua único em C#, em processo externo. Assim, o runtime .NET do Civil 3D 2020 não precisa carregar uma DLL .NET 8. A futura integração de Parcels precisará de um adaptador compilado para o runtime/SDK de cada família de versões e usará este mesmo contrato de dados.

Não suportados nesta fase: Parcels, POLYLINE antiga/3D, splines, elipses, regiões, furos/ilhas, polilinhas com largura, planos inclinados, inferência de sistema geodésico, redução terreno/projeção, adjacência entre imóveis e exportação DOCX. Arcos mantêm centro/raio/ângulo/desenvolvimento/corda/sentido; a orientação exterior pelo meio do arco é representativa e sempre recebe aviso para revisão. Nenhum confrontante é inventado.

Limite: 10.000 vértices. A validação de interseções é analítica, com comparação quadrática; grandes contornos podem demorar. Tolerâncias padrão: `1e-6 m` linear e `1e-4 m²` de área. São limites de cálculo e conferência, não declaração de precisão topográfica; devem ser revistos na homologação. Tolerâncias entre imóveis serão definidas na fase de adjacências.

O .NET 8 é a base deste protótipo; antes de uma versão de produção, o runtime empacotado deverá ser revisto conforme seu ciclo de suporte. O código não usa a instalação .NET interna do Civil 3D.

## Evidência atual

No ambiente Linux: compilação sem erros/avisos, 36 testes geométricos (incluindo 100 retângulos e 60 círculos segmentados), 13 cenários de CLI e verificação estrutural estática do LISP. Pacote win-x64 compilado de forma cruzada. **Execução do AutoLISP, execução do binário Windows e comparação com o Civil 3D ainda não realizadas.** PowerShell de empacotamento e CI também aguardam execução em Windows/GitHub. Consulte [o registro de validação](docs/VALIDACAO.md).
