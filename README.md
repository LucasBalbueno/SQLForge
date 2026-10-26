# SQLForge

Consulte uma tabela de produtos de forma visual, gerando uma consulta equivalente em SQL (PostgreSQL). Escolha as informações que deseja ver, aplique filtros e organize os resultados.

**Acesse a aplicação SQLForge:** https://sqlforge.onrender.com/

**OBSERVAÇÃO:** Caso apareça uma tela do Render, espere alguns segundos até reativar a hospedagem e a página será automaticamente recarregada na aplicação.

O uso é feito diretamente pelo navegador, não é necessária instalação ou cadastro de usuários.

## O que você pode fazer

- Consultar os produtos disponíveis no armazenamento da loja.
- Escolher quais informações aparecem nos resultados.
- Combinar filtros de texto e números.
- Ordenar os produtos por um ou mais campos.
- Visualizar e copiar o SQL gerado pelas suas escolhas.
- Executar a consulta e navegar pelos resultados em páginas.

**OBSERVAÇÃO:** A aplicação não permite cadastrar, editar ou excluir produtos.

## Sua primeira consulta

1. Acesse https://sqlforge.onrender.com/ e aguarde o carregamento dos campos.
2. Na seção 1, chamada **Colunas**, escolha as informações que deseja visualizar. Todas começam selecionadas. É obrigatório escolher pelo menos uma.
3. Se quiser refinar a busca, clique em **+ Adicionar filtro** (Na seção 2), escolha o campo, a condição e preencha o valor.
4. Para organizar a lista, clique em **+ Adicionar ordem** (Na seção 3) e escolha o campo e a direção.
5. Em **Consulta SQL** (Na seção 4), selecione quantos itens deseja ver por página e aguarde a atualização automática do SQL.
6. Copie a consulta personalizada clicando em "Copiar SQL".
6. Clique em **Executar consulta** (Na seção 4) para visualizar os produtos em **Resultados** (Na seção 5).

**OBSERVAÇÃO:** Para consultar todos os produtos, basta manter as colunas selecionadas e executar a consulta sem adicionar filtros.

## 01 - Colunas (Quais informações você pode ver)

Essa são as informações dos produtos que você pode selecionar para visualizar.

- **Id:** Identificador do produto.
- **Nome:** Nome do produto.
- **Categoria:** Categoria à qual o produto pertence.
- **Preço:** Preço do produto, exibido em reais (R$) nos resultados.
- **Estoque:** Quantidade de unidades em estoque.

**OBSERVAÇÃO:** O botão **Selecionar todas** marca novamente todos as colunas disponíveis.

## 02 - Como usar os filtros

Use **+ Adicionar filtro** para filtrar um campo por um valor específico. Por exemplo, filtrar produtos pelo nome "Monitor".

Cada filtro tem três partes: **campo**, **condição** e **valor**. As condições disponíveis dependem se campo escolhido é do tipo texto ou número:

| Campos | Condições disponíveis |
| --- | --- |
| Nome e Categoria (Tipo texto) | Igual a, Diferente de, Contém, Começa com e Termina com. |
| ID, Preço e Estoque (Tipo número) | Igual a, Diferente de, Maior que, Maior ou igual, Menor que e Menor ou igual. |

Importante:
- Todos os filtros são combinados por **E (AND)**, ou seja, um produto precisa atender a todas as condições para aparecer.
- A busca por texto diferencia maiúsculas e minúsculas. Por exemplo, `Monitor` e `monitor` podem retornar resultados diferentes.
- Preencha valores numéricos sem símbolos de moeda. Para preços com centavos, use ponto como separador decimal, por exemplo, `1500.50`.
- ID e Estoque recebem números inteiros.
- É possível adicionar até **20 filtros**, com valores de até **200 caracteres**.
- Clique em **X** na linha de um filtro para removê-lo.

## 03 - Como ordenar os resultados

Use **+ Adicionar ordem** para escolher um campo e a direção **Crescente** ou **Decrescente**. Por exemplo, ordenar por Preço em ordem crescente mostra os produtos mais baratos primeiro.

- Quando houver mais de uma ordenação, a prioridade segue de cima para baixo, ou seja, a segunda resolve os empates da primeira, e assim por diante.
- Cada campo pode ser usado apenas uma vez, com até cinco ordenações.
- Clique em **X** na linha de uma ordenação para removê-la.

**OBSERVAÇÃO:** Sem uma ordenação personalizada, os produtos são apresentados por ID crescente. O ID também é usado para desempatar resultados quando não faz parte das ordenações escolhidas.

## 04 - Visualizar, copiar e executar o SQL

A seção **Consulta SQL**, acompanha automaticamente as alterações do formulário quando todos os campos estão válidos.

- **Copiar SQL** copia a consulta exibida para a área de transferência pronta para uso em PostgreSQL.
- **Executar consulta** busca os produtos e preenche a tabela de resultados (Na seção 5).

Você ainda pode escolher o número de resultados por página (5, 10, 20 ou 30). O padrão é 10. A seção **Resultados** informa o total de registros encontrados, quantos estão na página atual e a página em que você está.

Use **← Anterior** e **Próxima →** para navegar. Esses botões ficam disponíveis quando há uma página correspondente. O SQL exibido também acompanha a página consultada.

Ao alterar colunas, filtros, ordenações ou itens por página, os resultados anteriores são limpos. Aguarde a atualização do SQL e execute novamente.

## Exemplo de uso: Buscar por notebooks disponíveis por até R$ 4.000

Primeiramente vamos visualizar um exemplo de consulta gerada pela aplicação

1. Em **Colunas**, selecione **Nome**, **Preço** e **Estoque**.
2. Adicione os seguintes filtros:

   | Campo | Condição | Valor |
   | --- | --- | --- |
   | Categoria | Igual a | `Notebooks` |
   | Preço | Menor ou igual | `4000` |
   | Estoque | Maior que | `0` |

3. Adicione uma ordenação por **Preço**, na direção **Crescente**.
4. Aguarde o SQL ser atualizado e clique em **Executar consulta**.

O resultado mostrará os notebooks que atendem ao limite de preço e possuem unidades em estoque, dos mais baratos para os mais caros.

## Dúvidas e soluções

| Situação | Problema | O que fazer |
| --- | --- | --- |
| Os botões **Copiar SQL** e **Executar consulta** estão desativados. | A configuração pode estar incompleta ou inválida, ou a geração do SQL ainda não terminou. | Confira a mensagem acima da consulta. Selecione pelo menos uma coluna, preencha todos os filtros com valores válidos e use campos diferentes nas ordenações. Depois, aguarde a atualização do SQL. |
| Nenhum produto foi encontrado. | Nenhum registro atende a todos os filtros informados. Diferenças entre maiúsculas e minúsculas também podem afetar a busca. | Revise os valores e a escrita dos textos. Remova ou ajuste filtros para ampliar a busca e clique em **Executar consulta** novamente. |
| Os resultados desapareceram após uma alteração. | Ao mudar a configuração, a aplicação limpa os resultados anteriores para evitar a exibição de dados de outra consulta. Esse comportamento é esperado. | Aguarde a atualização do SQL e clique em **Executar consulta** para visualizar os resultados da nova configuração. |
| Não foi possível copiar o SQL automaticamente. | O navegador pode ter bloqueado o acesso à área de transferência ou ocorrido uma falha na cópia. | Selecione o texto na seção **Consulta SQL** e copie com **Ctrl+C** (Windows/Linux) ou **⌘+C** (macOS). No celular, mantenha o texto pressionado e use a opção de copiar. |
| A página não carrega ou a consulta apresenta erro. | Pode haver uma falha de conexão ou uma indisponibilidade temporária do serviço. | Confira sua conexão e a mensagem de erro, se houver. Aguarde um pouco e tente novamente. Se o problema persistir, recarregue a página e refaça a configuração da consulta. |
