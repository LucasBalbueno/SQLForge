// Arquivo responsável por renderizar os resultados da consulta na interface do usuário

// Formatador de valores em Real (R$)
const currency = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
});

// Função auxiliar para exibir uma mensagem quando não há resultados
function showEmpty(message) {
  const hint = document.createElement("p");
  hint.className = "empty-hint";
  hint.textContent = message;

  document.getElementById("table-container").replaceChildren(hint);
}

// Limpa os resultados da consulta e exibe uma mensagem padrão
export function clearResults() {
  document.getElementById("result-summary").textContent = "Nenhuma consulta executada";
  document.getElementById("page-info").textContent = "Página —";

  showEmpty("Execute a consulta para visualizar os produtos");
}

// Renderiza os resultados da consulta na interface do usuário
export function renderResults(data, metadata) {
  // Atualiza o resumo dos resultados
  document.getElementById("result-summary").textContent = `${data.totalCount} registro(s) encontrado(s) · ${data.rows.length} nesta página`;
  // Atualiza a informação da páginação
  document.getElementById("page-info").textContent = data.totalPages ? `Página ${data.page} de ${data.totalPages}` : "Nenhum resultado";

  // Se não houver resultados, exibe uma mensagem apropriada
  if (!data.rows.length) {
    showEmpty("Nenhum produto encontrado nesta página.");
    return;
  }

  // Cria a tabela de resultados com data e metadata
  const table = document.createElement("table");
  const head = table.createTHead().insertRow();

  for (const column of data.columns) {
    const th = document.createElement("th");
    th.scope = "col";
    th.textContent = metadata.find((c) => c.name === column)?.label || column;
    head.append(th);
  }

  const body = table.createTBody();

  for (const record of data.rows) {
    const row = body.insertRow();

    for (const column of data.columns)
      row.insertCell().textContent =
        column === "price"
          ? currency.format(record[column])
          : String(record[column] ?? "");
  }

  document.getElementById("table-container").replaceChildren(table);
}
