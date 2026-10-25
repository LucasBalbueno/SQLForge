// Arquivo responsável por controlar a lógica da consulta.

import { generateQuery, executeQuery } from "./api.js";
import { status } from "./status-view.js";
import { createQueryForm } from "./query-form.js";
import { clearResults, renderResults } from "./results-view.js";

// Coordena o formulário, as requisições e a exibição dos resultados
export function initializeQuery(metadata) {
  // Variáveis de estado interno
  let generatedRequest = null;
  let result = null;
  let busy = false;
  let generationTimer;
  let generationVersion = 0;
  let generationController;

  // Cria o formulário de consulta com os metadados e a função de invalidação
  const form = createQueryForm(metadata, invalidate);

  // Atualiza o estado dos botões de acordo com a situação atual
  function updateButtons() {
    document.getElementById("copy").disabled = !generatedRequest;
    document.getElementById("execute").disabled = !generatedRequest;
    document.getElementById("previous").disabled = !result || result.page <= 1;
    document.getElementById("next").disabled = !result || result.page >= result.totalPages;
    document.getElementById("add-filter").disabled = document.getElementById("filters").children.length >= 20;
    document.getElementById("add-order").disabled = document.getElementById("orders").children.length >= metadata.length;
  }

  // Executa uma ação assíncrona, mostrando o status e bloqueando a interface do usuário
  async function run(action) {
    if (busy) return;

    // Se houver outra transição em andamento, cancela a execução atual e aguarda a próxima
    busy = true;
    document.getElementById("workspace").disabled = true;
    status("Processando…");

    try {
      await action();
    } catch (error) {
      status(error.message, true);
    } finally {
      busy = false;
      document.getElementById("workspace").disabled = false;
      updateButtons();
    }
  }

  // Invalida a consulta atual, gerando uma nova requisição e atualizando o SQL exibido
  function invalidate() {
    // Cancela qualquer geração de consulta em andamento e incrementa a versão para ignorar respostas antigas
    clearTimeout(generationTimer);
    generationController?.abort();
    const version = ++generationVersion;

    // Limpa a requisição e os resultados atuais, atualizando os botões
    generatedRequest = null;
    result = null;
    clearResults();
    updateButtons();

    let body;

    // Tenta ler a requisição do formulário, exibindo uma mensagem de erro se houver algum problema
    try {
      body = form.readRequest();
    } catch (error) {
      document.getElementById("sql").textContent = "Preencha a configuração para visualizar o SQL.";
      status(error.message);
      return;
    }

    // Atualiza o SQL exibido e mostra uma mensagem de status enquanto a nova consulta é gerada
    document.getElementById("sql").textContent = "Atualizando consulta…";
    status("Atualizando SQL automaticamente…");

    // Aguarda a digitação sem bloquear o formulário
    generationTimer = setTimeout(async () => {
      generationController = new AbortController();

      // Gera a consulta no backend, atualizando o SQL exibido e o status da interface do usuário
      try {
        const data = await generateQuery(body, generationController.signal);

        // Ignora respostas de configurações anteriores
        if (version !== generationVersion) return;

        // Atualiza a requisição gerada e o SQL exibido, mostrando uma mensagem de sucesso
        generatedRequest = body;
        document.getElementById("sql").textContent = data.sql;
        status("SQL atualizado. Pronto para copiar ou executar.");
      } catch (error) {
        if (error.name === "AbortError" || version !== generationVersion) return;

        document.getElementById("sql").textContent = "Não foi possível atualizar a consulta.";
        status(error.message, true);
      } finally {
        if (version === generationVersion) updateButtons();
      }
    }, 350);
  }

  // Executa a consulta no backend, atualizando os resultados e o status da interface do usuário
  async function execute(body, message) {
    const data = await executeQuery(body);

    generatedRequest = body;
    result = data;
    renderResults(data, metadata);
    status(message);
  }

  // Configura o botão de execução da consulta
  document.getElementById("execute").addEventListener("click", () =>
    run(() => execute(generatedRequest, "Consulta executada com sucesso.")),
  );

  // Função auxiliar para navegar entre páginas, atualizando a requisição e o SQL exibido
  async function navigate(delta) {
    await run(async () => {
      const body = { ...generatedRequest, page: result.page + delta };
      const sql = await generateQuery(body);
      await execute(body, "Página atualizada.");
      document.getElementById("sql").textContent = sql.sql;
    });
  }

  // Configura os botões de navegação e cópia do SQL
  document.getElementById("previous").addEventListener("click", () => navigate(-1));
  document.getElementById("next").addEventListener("click", () => navigate(1));
  document.getElementById("copy").addEventListener("click", () =>
    run(async () => {
      try {
        await navigator.clipboard.writeText(document.getElementById("sql").textContent);
        status("SQL copiado.");
      } catch {
        throw new Error(
          "Não foi possível copiar automaticamente. Selecione o SQL e copie manualmente.",
        );
      }
    }),
  );

  // Inicializa o formulário e habilita a interface do usuário
  form.initialize();
  document.getElementById("workspace").disabled = false;
  invalidate();
}
