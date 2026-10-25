// Arquivo responsável por criar o formulário frontend

// Mapa dos operadores
const operatorLabels = {
  eq: "Igual a",
  ne: "Diferente de",
  gt: "Maior que",
  gte: "Maior ou igual",
  lt: "Menor que",
  lte: "Menor ou igual",
  contains: "Contém",
  startsWith: "Começa com",
  endsWith: "Termina com",
};

export function createQueryForm(metadata, onChange) {
  // Consulta os metadados para criar as opções de coluna para filtros e ordenações
  const columnOptions = metadata.map((field) => [field.name, field.label]);

  // Cria um elemento <select> com as opções dos metadados
  function select(options, label) {
    const element = document.createElement("select");
    element.setAttribute("aria-label", label);

    for (const [value, text] of options) element.add(new Option(text, value));

    return element;
  }

  // Adiciona uma nova linha de filtro ou ordenação
  function addRow(kind) {
    // Cria uma linha html
    const filter = kind === "filters";
    const row = document.createElement("div");
    row.className = filter ? "filter-row" : "order-row";

    // Cria o select de colunas
    const column = select(
      columnOptions,
      filter ? "Coluna do filtro" : "Coluna de ordenação",
    );
    column.className = "column";

    // Organiza a ordemação das colunas
    if (!filter) {
      const used = [...document.getElementById("orders").querySelectorAll(".column")].map(
        (item) => item.value,
      );
      column.value =
        metadata.find((field) => !used.includes(field.name))?.name ||
        metadata[0].name;
    }

    // Cria o select de operadores ou direção na ordenação
    const choice = select(
      filter
        ? []
        : [
            ["ASC", "Crescente"],
            ["DESC", "Decrescente"],
          ],
      filter ? "Operador" : "Direção",
    );
    choice.className = "choice";
    row.append(column, choice);
    if (filter) addFilterInput(row, column, choice);
    column.addEventListener("change", onChange);
    choice.addEventListener("change", onChange);

    // Cria o botão de remover filtro ou ordenação
    const remove = document.createElement("button");
    remove.type = "button";
    remove.className = "remove";
    remove.textContent = "×";
    remove.setAttribute(
      "aria-label",
      filter ? "Remover filtro" : "Remover ordenação",
    );

    remove.addEventListener("click", () => {
      row.remove();
      refresh();
    });

    // Insere a linha no formulário
    row.append(remove);
    document.getElementById(kind).append(row);
    refresh();
  }

  // Cria o campo (<input>) de valor usado por um filtro
  function addFilterInput(row, column, choice) {
    const value = document.createElement("input");
    value.className = "value";
    value.setAttribute("aria-label", "Valor do filtro");
    value.maxLength = 200;

    // Atualiza os operadores e o tipo do campo de valor com base na coluna selecionada
    function setOperators() {
      const field = metadata.find((c) => c.name === column.value);
      choice.replaceChildren(
        ...field.operators.map((op) => new Option(operatorLabels[op], op)),
      );
      value.type = field.type === "text" ? "text" : "number";
      value.step = field.type === "decimal" ? "any" : "1";
      value.placeholder =
        field.type === "text" ? "Digite um texto" : "Informe um número";
      value.value = "";
    }

    // Configura inicialmente os operadores e atualiza quando a coluna é alterada
    setOperators();
    column.addEventListener("change", setOperators);
    value.addEventListener("input", onChange);
    row.append(value);
  }

  // Atualiza a interface quando filtros ou ordenações são adicionados ou removidos
  function refresh() {
    document.getElementById("no-filters").hidden = document.getElementById("filters").children.length > 0;
    document.getElementById("no-orders").hidden = document.getElementById("orders").children.length > 0;
    onChange();
  }

  // Lê os dados do formulário e cria o objeto de requisição com dados validados
  function readRequest() {
    // Leitura e validação do tamanho da página
    const pageSize = Number(document.getElementById("page-size").value);
    if (![5, 10, 20, 30].includes(pageSize))
      throw new Error("Escolha 5, 10, 20 ou 30 itens por página.");

    // Leitura e validação das colunas selecionadas
    const columns = [...document.getElementById("columns").querySelectorAll("input:checked")].map(
      (c) => c.value,
    );
    if (!columns.length) throw new Error("Selecione pelo menos uma coluna.");

    // Leitura e validação dos filtros selecionados
    const filters = [...document.getElementById("filters").children].map((row) => {
      const input = row.querySelector(".value");
      if (!input.value.trim() || !input.checkValidity())
        throw new Error(
          "Preencha todos os filtros com valores válidos para gerar o SQL.",
        );

      return {
        column: row.querySelector(".column").value,
        operator: row.querySelector(".choice").value,
        value: input.value,
      };
    });

    // Leitura e validação das ordenações selecionadas
    const orderBy = [...document.getElementById("orders").children].map((row) => ({
      column: row.querySelector(".column").value,
      direction: row.querySelector(".choice").value,
    }));
    if (new Set(orderBy.map((order) => order.column)).size !== orderBy.length)
      throw new Error("Escolha colunas diferentes para cada ordenação.");

    // Retorna uma requisição pronta para ser usada pelo controller do backend
    return { columns, filters, orderBy, page: 1, pageSize };
  }

  // Configura o formulário inicial com metadados
  function initialize() {
    // Cria os checkboxes de colunas
    for (const field of metadata) {
      const label = document.createElement("label");
      const input = document.createElement("input");
      input.type = "checkbox";
      input.value = field.name;
      input.checked = true;
      input.addEventListener("change", onChange);
      label.append(input, document.createTextNode(field.label));
      document.getElementById("columns").append(label);
    }

    // Configuração do botão de selecionar todas as colunas
    document.getElementById("select-all").addEventListener("click", () => {
      document.getElementById("columns")
        .querySelectorAll("input")
        .forEach((c) => {
          c.checked = true;
        });
      onChange();
    });

    // Alteração do tamanho da página
    document.getElementById("page-size").addEventListener("input", onChange);
    // Botões de adicionar filtros e ordenações
    document.getElementById("add-filter").addEventListener("click", () => addRow("filters"));
    document.getElementById("add-order").addEventListener("click", () => addRow("orders"));
  }

  return { initialize, readRequest };
}
