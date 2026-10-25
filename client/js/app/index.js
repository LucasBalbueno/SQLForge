import { getMetadata } from "./api.js";
import { status } from "./status-view.js";
import { initializeQuery } from "./query-controller.js";

// Tenta carregar os metadados do servidor e inicializar o gerador de consultas
try {
  status("Conectando ao estoque…");

  const metadata = await getMetadata();
  initializeQuery(metadata);
} catch {
  status("Não foi possível carregar os campos. Inicie a API e recarregue a página.", true);
}
